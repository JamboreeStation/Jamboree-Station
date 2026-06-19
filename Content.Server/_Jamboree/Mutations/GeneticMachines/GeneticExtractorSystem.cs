using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Power.EntitySystems;
using Content.Shared._Jamboree.Mutations;
using Content.Shared._Jamboree.Mutations.GeneticMachines;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Jamboree.Mutations.GeneticMachines;

public sealed class GeneticExtractorSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly GeneticMachineSystem _machine = default!;
    [Dependency] private readonly PotentialMutantSystem _potentialMutant = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeneticExtractorComponent, GeneticExtractorSetModeMessage>(OnSetMode);
        SubscribeLocalEvent<GeneticExtractorComponent, GeneticExtractorSetIsolatedMessage>(OnSetIsolated);
        SubscribeLocalEvent<GeneticExtractorComponent, GeneticExtractorSetGenepackNameMessage>(OnSetGenepackName);
        SubscribeLocalEvent<GeneticExtractorComponent, GeneticExtractorActivateMessage>(OnActivate);
        SubscribeLocalEvent<GeneticExtractorComponent, AfterActivatableUIOpenEvent>((u, c, _) => UpdateUi(u, c));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GeneticExtractorComponent, GeneticMachineComponent>();
        while (query.MoveNext(out var uid, out var extractor, out var machine))
        {
            if (extractor.FinishesAt is not { } finishesAt)
                continue;
            if (_timing.CurTime < finishesAt)
                continue;
            CompleteRun(uid, extractor, machine);
        }
    }

    private void OnSetMode(EntityUid uid, GeneticExtractorComponent component, GeneticExtractorSetModeMessage args)
    {
        component.Mode = args.Mode;
        UpdateUi(uid, component);
    }

    private void OnSetIsolated(EntityUid uid, GeneticExtractorComponent component, GeneticExtractorSetIsolatedMessage args)
    {
        component.IsolatedMutation = args.MutationId;
        UpdateUi(uid, component);
    }

    private void OnSetGenepackName(EntityUid uid, GeneticExtractorComponent component, GeneticExtractorSetGenepackNameMessage args)
    {
        component.GenepackName = args.Name;
        UpdateUi(uid, component);
    }

    private void OnActivate(EntityUid uid, GeneticExtractorComponent component, GeneticExtractorActivateMessage args)
    {
        if (!_power.IsPowered(uid))
            return;
        if (component.FinishesAt != null)
            return;
        if (!TryComp<GeneticMachineComponent>(uid, out var machine))
            return;

        _machine.RecheckRange(uid, machine);
        if (_machine.GetScannedBody(uid, machine) is not { } target)
            return;

        component.CurrentTarget = target;
        var duration = component.Mode == GeneticExtractorMode.Cleanse ? component.CleanseTime : component.IsolateTime;
        component.StartedAt = _timing.CurTime;
        component.FinishesAt = _timing.CurTime + TimeSpan.FromSeconds(duration);
        UpdateUi(uid, component);
    }

    private void CompleteRun(EntityUid uid, GeneticExtractorComponent component, GeneticMachineComponent machine)
    {
        var target = component.CurrentTarget;
        component.CurrentTarget = null;
        component.StartedAt = null;
        component.FinishesAt = null;

        var resolved = _machine.GetScannedBody(uid, machine);
        if (target is { } original && resolved == original && TryComp<MutantComponent>(original, out var mutant))
        {
            switch (component.Mode)
            {
                case GeneticExtractorMode.Cleanse:
                    _potentialMutant.RemoveAllMutations((original, mutant));
                    // TODO: apply nausea status effect for ~2 minutes per design doc.
                    break;
                case GeneticExtractorMode.Isolate:
                    var mutations = mutant.ActiveMutations
                        .Select(m => m.ID)
                        .Where(id => component.IsolatedMutation is null || id != component.IsolatedMutation.Value.Id)
                        .ToList();
                    SpawnGenepack(uid, mutations);
                    // Strip every mutation from the (about-to-be-deceased) subject so the body doesn't keep them if revived. The extracted genes only
                    //   live on inside the freshly-spawned genepack.
                    _potentialMutant.RemoveAllMutations((original, mutant));
                    // Kill the target by overdamaging the chest
                    if (TryComp<DamageableComponent>(original, out var damageable)
                        && _proto.TryIndex<DamageGroupPrototype>("Brute", out var group))
                    {
                        var damage = new DamageSpecifier(group, FixedPoint2.New(500));
                        _damageable.TryChangeDamage(original, damage, true);
                    }
                    break;
            }
        }
        UpdateUi(uid, component);
    }

    private void SpawnGenepack(EntityUid console, List<string> mutationIds)
    {
        if (!TryComp<GeneticExtractorComponent>(console, out var component))
            return;
        var coords = Transform(console).Coordinates;
        var gp = Spawn(component.GenepackPrototype, coords);
        var gpComp = EnsureComp<GenepackComponent>(gp);
        gpComp.Mutations = mutationIds.Select(id => new ProtoId<MutationPrototype>(id)).ToList();
        // Apply the user-supplied tag via NameModifierSystem so the genepack ends
        // up displayed as "genepack (teddy bear)" everywhere — including the
        // Recombiner library, which renders MetaData.EntityName.
        if (!string.IsNullOrWhiteSpace(component.GenepackName))
            _label.Label(gp, component.GenepackName);
    }

    private void UpdateUi(EntityUid uid, GeneticExtractorComponent component)
    {
        if (!_ui.HasUi(uid, GeneticExtractorUiKey.Key))
            return;
        if (!TryComp<GeneticMachineComponent>(uid, out var machine))
            return;
        _machine.RecheckRange(uid, machine);

        var status = GetStatus(uid, machine, out var occupantName);
        var mutations = new List<string>();
        if (_machine.GetScannedBody(uid, machine) is { } body && TryComp<MutantComponent>(body, out var mutant))
            mutations = mutant.ActiveMutations.Select(m => m.ID).ToList();

        _ui.SetUiState(uid, GeneticExtractorUiKey.Key, new GeneticExtractorBoundUserInterfaceState(
            status, occupantName, component.FinishesAt != null,
            component.Mode, mutations, component.IsolatedMutation?.Id,
            component.GenepackName,
            component.StartedAt, component.FinishesAt));
    }

    private GeneticMachineScannerStatus GetStatus(EntityUid uid, GeneticMachineComponent machine, out string occupantName)
    {
        occupantName = string.Empty;
        if (machine.Scanner is null)
            return GeneticMachineScannerStatus.NoScannerLinked;
        if (!machine.ScannerInRange)
            return GeneticMachineScannerStatus.ScannerOutOfRange;
        var body = _machine.GetScannedBody(uid, machine);
        if (body is null)
            return GeneticMachineScannerStatus.ScannerEmpty;
        occupantName = MetaData(body.Value).EntityName;
        return GeneticMachineScannerStatus.ScannerOccupied;
    }
}
