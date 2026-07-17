// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Jamboree.Mutations.GeneticMachines;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._Jamboree.Mutations;

[ByRefEvent]
public readonly record struct MutationAddedEvent(EntityUid Entity, MutationPrototype Mutation);

[ByRefEvent]
public readonly record struct MutationRemovedEvent(EntityUid Entity, MutationPrototype Mutation);

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

        // Suppression: collar (suppresses everything) is owned here. PsionicInsulation
        // is owned by PsionicInvisibilitySystem which already subscribes to its
        // lifecycle; that system hooks into the helpers below directly.
        SubscribeLocalEvent<MutantComponent, ComponentStartup>(OnMutantStartup);
        SubscribeLocalEvent<MutantSuppressedComponent, ComponentStartup>(OnCollarStartup);
        SubscribeLocalEvent<MutantSuppressedComponent, ComponentShutdown>(OnCollarShutdown);
    }

    public const string CollarSourceId = "InhibitorCollar";
    public const string InsulationSourceId = "PsionicInsulation";

    private void OnMutantStartup(EntityUid uid, MutantComponent comp, ComponentStartup args)
    {
        // If the entity already has suppressing components when it becomes a mutant,
        // pick up those suppressions now so freshly-gained genes don't activate.
        if (HasComp<MutantSuppressedComponent>(uid))
            AddSuppressionSource((uid, comp), CollarSourceId, MutationSuppressionFilter.All);
        if (TryComp(uid, out PsionicInsulationComponent? insul) && !insul.Passthrough)
            AddSuppressionSource((uid, comp), InsulationSourceId, MutationSuppressionFilter.Mental);
    }

    private void OnCollarStartup(EntityUid uid, MutantSuppressedComponent comp, ComponentStartup args)
    {
        if (TryComp<MutantComponent>(uid, out var mutant))
            AddSuppressionSource((uid, mutant), CollarSourceId, MutationSuppressionFilter.All);
    }

    private void OnCollarShutdown(EntityUid uid, MutantSuppressedComponent comp, ComponentShutdown args)
    {
        if (TryComp<MutantComponent>(uid, out var mutant))
            RemoveSuppressionSource((uid, mutant), CollarSourceId);
    }

    /// <summary>
    ///     Returns true when this mutation's effects should be inert because at least
    ///     one currently-active suppression source matches it.
    /// </summary>
    public bool IsSuppressed(MutantComponent comp, MutationPrototype mutation)
    {
        foreach (var filter in comp.SuppressionSources.Values)
        {
            if (Matches(filter, mutation))
                return true;
        }
        return false;
    }

    private static bool Matches(MutationSuppressionFilter filter, MutationPrototype mutation) => filter switch
    {
        MutationSuppressionFilter.All => true,
        MutationSuppressionFilter.Mental => mutation.IsMental,
        _ => false,
    };

    /// <summary>
    ///     Begins suppressing mutations matching <paramref name="filter"/>. For each
    ///     active mutation that transitions from running → suppressed, its
    ///     RemovalFunctions are invoked. Adding the same source twice is a no-op.
    /// </summary>
    public void AddSuppressionSource(Entity<MutantComponent> ent, string sourceId, MutationSuppressionFilter filter)
    {
        var comp = ent.Comp;
        if (comp.SuppressionSources.ContainsKey(sourceId))
            return;

        // Snapshot what's currently running (not yet suppressed by any existing source).
        var wasRunning = new List<MutationPrototype>();
        foreach (var m in comp.ActiveMutations)
        {
            if (!IsSuppressed(comp, m))
                wasRunning.Add(m);
        }

        comp.SuppressionSources[sourceId] = filter;

        foreach (var m in wasRunning)
        {
            if (!Matches(filter, m))
                continue;
            foreach (var fn in m.RemovalFunctions)
                fn.OnMutate(ent.Owner, _componentFactory, EntityManager, _serialization, _playerManager, Loc, comp, m);
        }
    }

    /// <summary>
    ///     Removes the suppression source. Any mutation that was being suppressed by
    ///     it and is no longer suppressed by anything else has its InitializeFunctions
    ///     invoked again.
    /// </summary>
    public void RemoveSuppressionSource(Entity<MutantComponent> ent, string sourceId)
    {
        var comp = ent.Comp;
        if (!comp.SuppressionSources.Remove(sourceId, out var filter))
            return;

        foreach (var m in comp.ActiveMutations)
        {
            // Only re-activate mutations this source was actually covering and that
            // no remaining source is still covering.
            if (!Matches(filter, m))
                continue;
            if (IsSuppressed(comp, m))
                continue;
            foreach (var fn in m.InitializeFunctions)
                fn.OnMutate(ent.Owner, _componentFactory, EntityManager, _serialization, _playerManager, Loc, comp, m);
        }
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
        if (!_protoMan.HasIndex<MutationPrototype>(mutation.ID) || mutant.ActiveMutations.Any(mut => mut.ID == mutation.ID))
            return; // Sanity check
        _popup.PopupEntity(Loc.GetString("mutant-gain-popup"), ent.Owner, ent.Owner, PopupType.MediumCaution);
        mutant.ActiveMutations.Add(mutation);
        // If a suppression source currently covers this mutation, leave its effects
        // un-applied; they'll be applied later by RemoveSuppressionSource.
        if (!IsSuppressed(mutant, mutation))
        {
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
        // Raised last, once the mutation is fully applied. Handlers are allowed to strip or
        // transform the entity outright (GreaterMutantSystem does), so anything we ran after the
        // raise would be writing effects onto an entity that has already been cleaned up.
        var addedEvent = new MutationAddedEvent(ent.Owner, mutation);
        RaiseLocalEvent(ent.Owner, ref addedEvent);
    }

    public void RemoveMutation(Entity<MutantComponent> ent, MutationPrototype mutation)
    {
        var mutant = ent.Comp;
        if (!_protoMan.HasIndex<MutationPrototype>(mutation.ID) || !mutant.ActiveMutations.Any(mut => mut.ID == mutation.ID))
            return; // Sanity check
        // Only invoke RemovalFunctions if the mutation's effects are currently live.
        // If it's suppressed, the effects were already removed when suppression started.
        if (!IsSuppressed(mutant, mutation))
        {
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
        }
        mutant.ActiveMutations.RemoveWhere(mut => mut.ID == mutation.ID);
        var removedEvent = new MutationRemovedEvent(ent.Owner, mutation);
        RaiseLocalEvent(ent.Owner, ref removedEvent);
        if (!mutant.ActiveMutations.Any())
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
        foreach (var kv in entity.Comp.MutateDamageThreshold.DamageDict)
        {
            if (!damageDelta.DamageDict.TryGetValue(kv.Key, out var damageValue))
                return; // Target did not take this kind of damage, or cannot take it.
            if (damageValue < kv.Value)
                return; // Target did not meet threshold
        }
        // Passed all thresholds
        TryGainRandomMutation(entity);
    }
}