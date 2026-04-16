using System.Linq;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._Jamboree.Mutations;

public sealed partial class PotentialMutantSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PotentialMutantComponent, DamageChangedEvent>(OnDamageChanged);
    }

    public MutantComponent BecomeMutant(Entity<PotentialMutantComponent> ent)
    {
        if (TryComp<MutantComponent>(ent, out MutantComponent? mutant)) return mutant;

        // Gaining mutant effects
        mutant = AddComp<MutantComponent>(ent);
        // Change blood?

        return mutant;
    }

    public void TryGainRandomMutation(Entity<PotentialMutantComponent> ent)
    {
        if (!_protoMan.TryIndex(ent.Comp.MutationPool, out var pool))
            return; // No mutation pool :(

        // Roll a random mutation
        var randomMutation = pool.Pick();
        if (!_protoMan.TryIndex<MutationPrototype>(randomMutation, out var mutation))
            return; // Failed to roll a power.

        // If not already a mutant, make a mutant.
        var mutant = BecomeMutant(ent);
        AddMutation(new(ent.Owner, mutant), mutation);
    }

    public void AddMutation(Entity<MutantComponent> ent, MutationPrototype mutation)
    {
        var mutant = ent.Comp;
        if (!_protoMan.HasIndex<MutationPrototype>(mutation.ID) || mutant.ActiveMutations.Contains(mutation))
            return; // Sanity check
        _popup.PopupEntity(Loc.GetString("mutant-gain-popup"), ent.Owner, ent.Owner, PopupType.MediumCaution);
        mutant.ActiveMutations.Add(mutation);
        foreach (var function in mutation.InitializeFunctions)
            function.OnMutate(ent.Owner,
                _componentFactory,
                EntityManager,
                _serialization,
                _playerManager,
                Loc,
                mutant,
                mutation
            );
    }

    public void RemoveMutation(Entity<MutantComponent> ent, MutationPrototype mutation)
    {
        var mutant = ent.Comp;
        if (!_protoMan.HasIndex<MutationPrototype>(mutation.ID) || !mutant.ActiveMutations.Contains(mutation))
            return; // Sanity check
        foreach (var function in mutation.RemovalFunctions)
            function.OnMutate(ent.Owner,
                _componentFactory,
                EntityManager,
                _serialization,
                _playerManager,
                Loc,
                mutant,
                mutation
            );
        mutant.ActiveMutations.Remove(mutation);
        if(!mutant.ActiveMutations.Any())
        {
            // Demutate me.
            RemComp<MutantComponent>(ent.Owner);
        }
    }

    public void RemoveAllMutations(Entity<MutantComponent> ent)
    {
        var currentMutations = _serialization.CreateCopy(ent.Comp.ActiveMutations, notNullableOverride: true);
        foreach (var proto in currentMutations)
        {
            RemoveMutation(ent, proto);
        }
    }

    public bool OnAttemptMutantAbilityUse(EntityUid uid, string power, bool checkInsulation = true)
    {
        if (!TryComp<MutantComponent>(uid, out var component)
            || checkInsulation
            && TryComp(uid, out PsionicInsulationComponent? insul) && !insul.Passthrough)
            return false;

        var tev = new OnAttemptPowerUseEvent(uid, power);
        RaiseLocalEvent(uid, tev);

        if (tev.Cancelled)
            return false;

        if (component.DoAfter is not null)
        {
            _popup.PopupEntity(Loc.GetString(component.AlreadyCasting), uid, uid, PopupType.LargeCaution);
            return false;
        }

        return true;
    }

    public bool OnAttemptMutantAbilityUse(EntityUid uid, EntityUid target, string power, bool checkInsulation = true)
    {
        if (!TryComp<MutantComponent>(uid, out var component)
            || checkInsulation
            && (TryComp(uid, out PsionicInsulationComponent? insul) && !insul.Passthrough || HasComp<PsionicInsulationComponent>(target)))
            return false;

        var tev = new OnAttemptPowerUseEvent(uid, power);
        RaiseLocalEvent(uid, tev);

        if (tev.Cancelled)
            return false;

        if (component.DoAfter is not null)
        {
            _popup.PopupEntity(Loc.GetString(component.AlreadyCasting), uid, uid, PopupType.LargeCaution);
            return false;
        }

        return true;
    }

    public void LogAbilityUsed(EntityUid uid, string ability)
    {
        _adminLogger.Add(Database.LogType.Psionics, Database.LogImpact.Medium, $"{ToPrettyString(uid):player} used {ability}");
    }

    public void OnDamageChanged(Entity<PotentialMutantComponent> entity, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is not { } damageDelta)
            return;
        // if we have radiation damage over a threshold, gain a mutation
        foreach (var kv in entity.Comp.MutateDamageThreshold.DamageDict) {
            if (!damageDelta.DamageDict.TryGetValue(kv.Key, out var damageValue))
                return; // Target did not take this kind of damage, or cannot take it.
            if (damageValue < kv.Value)
                return; // Target did not meet threshold
        }
        // Passed all thresholds
        TryGainRandomMutation(entity);
    }
}