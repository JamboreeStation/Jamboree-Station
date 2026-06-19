using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared._Jamboree.Mutations;
using Content.Shared._Jamboree.Mutations.GeneticMachines;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Jamboree.Mutations.GeneticMachines;

public sealed class RecombinerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RecombinerComponent, RecombinerTogglePackMessage>(OnTogglePack);
        SubscribeLocalEvent<RecombinerComponent, RecombinerSetGenotypeNameMessage>(OnSetGenotype);
        SubscribeLocalEvent<RecombinerComponent, RecombinerStartMessage>(OnStart);
        SubscribeLocalEvent<RecombinerComponent, RecombinerEjectMessage>(OnEject);
        SubscribeLocalEvent<RecombinerComponent, AfterActivatableUIOpenEvent>((u, c, _) => UpdateUi(u, c));
        SubscribeLocalEvent<RecombinerComponent, EntInsertedIntoContainerMessage>((u, c, _) => UpdateUi(u, c));
        SubscribeLocalEvent<RecombinerComponent, EntRemovedFromContainerMessage>((u, c, _) => UpdateUi(u, c));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RecombinerComponent>();
        while (query.MoveNext(out var uid, out var recombiner))
        {
            if (recombiner.FinishesAt is not { } finishesAt)
                continue;
            if (_timing.CurTime < finishesAt)
                continue;
            CompleteRun(uid, recombiner);
        }
    }

    private void OnTogglePack(EntityUid uid, RecombinerComponent component, RecombinerTogglePackMessage args)
    {
        if (component.FinishesAt != null)
            return;
        if (!component.SelectedPacks.Remove(args.Genepack))
            component.SelectedPacks.Add(args.Genepack);
        UpdateUi(uid, component);
    }

    private void OnSetGenotype(EntityUid uid, RecombinerComponent component, RecombinerSetGenotypeNameMessage args)
    {
        component.GenotypeName = args.Name;
        UpdateUi(uid, component);
    }

    private void OnEject(EntityUid uid, RecombinerComponent component, RecombinerEjectMessage args)
    {
        var ent = GetEntity(args.Genepack);
        // Pop the genepack out of whichever slot it's in.
        foreach (var slotId in component.SlotIds)
        {
            if (!_itemSlots.TryGetSlot(uid, slotId, out var slot)) continue;
            if (slot.Item == ent)
            {
                _itemSlots.TryEjectToHands(uid, slot, null);
                break;
            }
        }
        component.SelectedPacks.Remove(args.Genepack);
        UpdateUi(uid, component);
    }

    private void OnStart(EntityUid uid, RecombinerComponent component, RecombinerStartMessage args)
    {
        if (!_power.IsPowered(uid))
            return;
        if (component.FinishesAt != null)
            return;

        var stats = ComputeStats(component);
        if (stats.UnionMutationIds.Count == 0
            || stats.Complexity > component.ComplexityCap
            || stats.Positive != stats.Negative)
            return;

        component.StartedAt = _timing.CurTime;
        component.FinishesAt = _timing.CurTime + TimeSpan.FromSeconds(component.ProcessingTime);
        UpdateUi(uid, component);
    }

    private void CompleteRun(EntityUid uid, RecombinerComponent component)
    {
        var stats = ComputeStats(component);
        component.StartedAt = null;
        component.FinishesAt = null;

        // Validate again (something may have changed mid-process).
        if (stats.UnionMutationIds.Count == 0)
        {
            UpdateUi(uid, component);
            return;
        }

        var coords = Transform(uid).Coordinates;
        var injector = Spawn(component.InjectorPrototype, coords);
        var injComp = EnsureComp<GenepackInjectorComponent>(injector);
        injComp.Mutations = stats.UnionMutationIds
            .Select(id => new ProtoId<MutationPrototype>(id))
            .ToList();
        if (!string.IsNullOrWhiteSpace(component.GenotypeName))
        {
            // Label flows through NameModifierSystem so the entity's display name
            // ends up as e.g. "genepack injector (teddy bear)".
            _label.Label(injector, component.GenotypeName);
        }

        // The recombination consumes every selected pack outright.
        foreach (var packNet in component.SelectedPacks.ToList())
        {
            var ent = GetEntity(packNet);
            if (Deleted(ent))
                continue;
            // Eject from the slot first so the container doesn't keep a reference.
            foreach (var slotId in component.SlotIds)
            {
                if (!_itemSlots.TryGetSlot(uid, slotId, out var slot)) continue;
                if (slot.Item == ent)
                {
                    _itemSlots.TryEject(uid, slot, null, out _);
                    break;
                }
            }
            QueueDel(ent);
        }
        component.SelectedPacks.Clear();
        UpdateUi(uid, component);
    }

    /// <summary>
    ///     Computes the deduplicated union of every selected pack's mutations
    ///     plus the aggregate Complexity / MetabolicEfficiency / +/- counts.
    ///     Mutations that appear in multiple selected packs are counted once.
    /// </summary>
    public RecombinerStats ComputeStats(RecombinerComponent component)
    {
        var union = new HashSet<string>();
        foreach (var packNet in component.SelectedPacks)
        {
            var ent = GetEntity(packNet);
            if (!TryComp<GenepackComponent>(ent, out var pack))
                continue;
            foreach (var m in pack.Mutations)
                union.Add(m.Id);
        }

        var complexity = 0;
        var efficiency = 0;
        var positive = 0;
        var negative = 0;
        foreach (var id in union)
        {
            if (!_proto.TryIndex<MutationPrototype>(id, out var proto))
                continue;
            complexity += proto.Complexity;
            efficiency += proto.MetabolicEfficiency;
            if (proto.Beneficial) positive++;
            else negative++;
        }
        return new RecombinerStats(union.ToList(), complexity, efficiency, positive, negative);
    }

    public readonly record struct RecombinerStats(
        List<string> UnionMutationIds,
        int Complexity,
        int MetabolicEfficiency,
        int Positive,
        int Negative)
    {
        public bool Balanced => Positive <= Negative;
    }

    private void UpdateUi(EntityUid uid, RecombinerComponent component)
    {
        if (!_ui.HasUi(uid, RecombinerUiKey.Key))
            return;

        var loaded = new List<RecombinerLoadedGenepack>();
        foreach (var slotId in component.SlotIds)
        {
            if (!_itemSlots.TryGetSlot(uid, slotId, out var slot)) continue;
            if (slot.Item is not { } itemUid) continue;
            if (!TryComp<GenepackComponent>(itemUid, out var pack)) continue;
            loaded.Add(new RecombinerLoadedGenepack
            {
                Entity = GetNetEntity(itemUid),
                // MetaData.EntityName already includes the LabelSystem-applied tag
                // via NameModifierSystem, so labelled packs show as e.g.
                // "genepack (teddy bear)" here without extra plumbing.
                Label = MetaData(itemUid).EntityName,
                MutationIds = pack.Mutations.Select(m => m.Id).ToList(),
            });
        }

        // Drop selections that point at packs that have been ejected.
        var loadedSet = loaded.Select(p => p.Entity).ToHashSet();
        component.SelectedPacks.RemoveAll(g => !loadedSet.Contains(g));

        var stats = ComputeStats(component);
        var balanced = stats.Balanced && stats.UnionMutationIds.Count > 0
                       && stats.Complexity <= component.ComplexityCap;

        _ui.SetUiState(uid, RecombinerUiKey.Key, new RecombinerBoundUserInterfaceState(
            loaded,
            new List<NetEntity>(component.SelectedPacks),
            stats.UnionMutationIds,
            stats.Complexity, component.ComplexityCap,
            stats.MetabolicEfficiency, stats.Positive, stats.Negative,
            balanced,
            component.FinishesAt != null, component.GenotypeName,
            component.StartedAt, component.FinishesAt));
    }
}
