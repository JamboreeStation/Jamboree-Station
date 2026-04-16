// SPDX-FileCopyrightText: 2025 Baine Junk <wym0n@proton.me>
// SPDX-FileCopyrightText: 2025 JamboreeBot <JamboreeBot@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Player;
using Content.Server.DoAfter;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Examine;
using static Content.Shared.Examine.ExamineSystemShared;
using Robust.Shared.Timing;
using Robust.Server.Audio;
using Content.Server.Atmos.Rotting;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared._Jamboree.Mutations;
using Content.Shared._Jamboree.Actions.Events;

namespace Content.Server._Jamboree.Abilities.Mutations;

public sealed class RevififyMutationSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly PotentialMutantSystem _mutant = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MutantComponent, MutantHealOtherMutationActionEvent>(OnPowerUsed);
        SubscribeLocalEvent<MutantComponent, MutantHealOtherDoAfterEvent>(OnDoAfter);
    }


    private void OnPowerUsed(EntityUid uid, MutantComponent component, MutantHealOtherMutationActionEvent args)
    {
        if (!_mutant.OnAttemptMutantAbilityUse(args.Performer, args.Target, args.AbilityName, true))
            return;

        if (!args.Immediate)
            AttemptDoAfter(uid, component, args);
        else ActivatePower(uid, component, args);

        if (args.PopupText is not null)
            _popupSystem.PopupEntity(Loc.GetString(args.PopupText, ("entity", uid)), uid,
                Filter.Pvs(uid).RemoveWhereAttachedEntity(entity => !_examine.InRangeUnOccluded(uid, entity, ExamineRange, null)),
                true,
                args.PopupType);

        if (args.PlaySound)
            _audioSystem.PlayPvs(args.SoundUse, uid, args.AudioParams);

        _mutant.LogAbilityUsed(uid, args.AbilityName);
        args.Handled = true;
    }

    private void AttemptDoAfter(EntityUid uid, MutantComponent component, MutantHealOtherMutationActionEvent args)
    {
        var ev = new MutantHealOtherDoAfterEvent(_gameTiming.CurTime);
        if (args.HealingAmount is not null)
            ev.HealingAmount = args.HealingAmount;
        if (args.RotReduction is not null)
            ev.RotReduction = args.RotReduction.Value;
        ev.DoRevive = args.DoRevive;
        var doAfterArgs = new DoAfterArgs(EntityManager, uid, args.UseDelay, ev, uid, target: args.Target)
        {
            BreakOnMove = args.BreakOnMove
        };

        if (!_doAfterSystem.TryStartDoAfter(doAfterArgs, out var doAfterId))
            return;

        component.DoAfter = doAfterId;
    }

    private void OnDoAfter(EntityUid uid, MutantComponent component, MutantHealOtherDoAfterEvent args)
    {
        // It's entirely possible for the caster to stop being Psionic(due to mindbreaking) mid cast
        if (component is null)
            return;
        component.DoAfter = null;

        // The target can also cease existing mid-cast
        // Or the DoAfter is cancelled(such as if the caster moves).
        if (args.Target is null
            || args.Cancelled)
            return;

        if (args.RotReduction is not null)
            _rotting.ReduceAccumulator(args.Target.Value, TimeSpan.FromSeconds(args.RotReduction.Value /* * args.ModifiedAmplification */)); // Jamboree - No mood system

        if (!TryComp<DamageableComponent>(args.Target.Value, out var damageableComponent))
            return;

        if (args.HealingAmount is not null)
            _damageable.TryChangeDamage(args.Target.Value, args.HealingAmount /* * args.ModifiedAmplification */, true, false, damageableComponent, uid); // Jamboree - No mood system

        if (!args.DoRevive
            || _rotting.IsRotten(args.Target.Value)
            || !TryComp<MobStateComponent>(args.Target.Value, out var mob)
            || !_mobState.IsDead(args.Target.Value, mob)
            || !_mobThreshold.TryGetThresholdForState(args.Target.Value, MobState.Dead, out var threshold)
            || damageableComponent.TotalDamage > threshold)
            return;

        _mobState.ChangeMobState(args.Target.Value, MobState.Critical, mob, uid);
    }

    // This would be the same thing as OnDoAfter, except that here the target isn't nullable, so I have to reuse code with different arguments.
    private void ActivatePower(EntityUid uid, MutantComponent component, MutantHealOtherMutationActionEvent args)
    {
        if (component is null)
            return;

        if (args.RotReduction is not null)
            _rotting.ReduceAccumulator(args.Target, TimeSpan.FromSeconds(args.RotReduction.Value));

        if (!TryComp<DamageableComponent>(args.Target, out var damageableComponent))
            return;

        if (args.HealingAmount is not null)
            _damageable.TryChangeDamage(args.Target, args.HealingAmount, true, false, damageableComponent, uid);

        if (!args.DoRevive
            || _rotting.IsRotten(args.Target)
            || !TryComp<MobStateComponent>(args.Target, out var mob)
            || !_mobState.IsDead(args.Target, mob)
            || !_mobThreshold.TryGetThresholdForState(args.Target, MobState.Dead, out var threshold)
            || damageableComponent.TotalDamage > threshold)
            return;

        _mobState.ChangeMobState(args.Target, MobState.Critical, mob, uid);
    }
}
