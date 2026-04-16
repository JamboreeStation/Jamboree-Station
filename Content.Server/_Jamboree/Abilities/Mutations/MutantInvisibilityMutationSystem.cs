using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Stunnable;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared._EinsteinEngines.Psionics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Throwing;
using Robust.Shared.Timing;
using Content.Shared._Jamboree.Mutations;
using Content.Shared._Jamboree.Actions.Events;

namespace Content.Server._Jamboree.Abilities.Mutations;

public sealed class MutantInvisibilityMutationSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly PotentialMutantSystem _mutant = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MutantComponent, MutantInvisibilityMutationActionEvent>(OnPowerUsed);
        SubscribeLocalEvent<RemoveMutantInvisibilityOffMutationActionEvent>(OnPowerOff);
        SubscribeLocalEvent<MutantInvisibilityUsedComponent, ComponentInit>(OnStart);
        SubscribeLocalEvent<MutantInvisibilityUsedComponent, ComponentShutdown>(OnEnd);
        SubscribeLocalEvent<MutantInvisibilityUsedComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MutantInvisibilityUsedComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<MutantInvisibilityUsedComponent, ShotAttemptedEvent>(OnShootAttempt);
        SubscribeLocalEvent<MutantInvisibilityUsedComponent, ThrowAttemptEvent>(OnThrowAttempt);
    }

    // This entire system is disgusting and doesn't comply with newer psi power standards.
    // But all I'm here for is to fix a bug, so bite me - TCJ.
    private void OnPowerUsed(EntityUid uid, MutantComponent component, MutantInvisibilityMutationActionEvent args)
    {
        if (!_mutant.OnAttemptMutantAbilityUse(args.Performer, "mutant invisibility", true)
            || HasComp<MutantInvisibilityUsedComponent>(uid))
            return;

        ToggleInvisibility(args.Performer);
        if (_actions.GetAction(args.Action.AsNullable()) is { Comp.UseDelay: not null } action)
            _actions.SetCooldown(args.Action.AsNullable(), action.Comp.UseDelay.Value);

        Timer.Spawn(TimeSpan.FromSeconds(args.PowerTimer), () => RemComp<MutantInvisibilityUsedComponent>(uid));
        _mutant.LogAbilityUsed(uid, "psionic invisibility");
        args.Handled = true;
    }

    private void OnPowerOff(RemoveMutantInvisibilityOffMutationActionEvent args)
    {
        if (!HasComp<MutantInvisibilityUsedComponent>(args.Performer))
            return;

        ToggleInvisibility(args.Performer);
        args.Handled = true;
    }

    private void OnStart(EntityUid uid, MutantInvisibilityUsedComponent component, ComponentInit args)
    {
        EnsureComp<PsionicallyInvisibleComponent>(uid);
        var stealth = EnsureComp<StealthComponent>(uid);
        _stealth.SetVisibility(uid, 0.66f, stealth);

        if (_net.IsServer)
            _audio.PlayPvs(component.StartSound, uid);

    }

    private void OnEnd(EntityUid uid, MutantInvisibilityUsedComponent component, ComponentShutdown args)
    {
        if (Terminating(uid))
            return;

        RemComp<PsionicallyInvisibleComponent>(uid);
        RemComp<StealthComponent>(uid);

        if (_net.IsServer)
            _audio.PlayPvs(component.EndSound, uid);

        DirtyEntity(uid);
    }

    private void OnAttackAttempt(EntityUid uid, MutantInvisibilityUsedComponent component, AttackAttemptEvent args) =>
        RemComp<MutantInvisibilityUsedComponent>(uid);

    private void OnShootAttempt(EntityUid uid, MutantInvisibilityUsedComponent component, ShotAttemptedEvent args) =>
        RemComp<MutantInvisibilityUsedComponent>(uid);

    private void OnThrowAttempt(EntityUid uid, MutantInvisibilityUsedComponent component, ThrowAttemptEvent args) =>
        RemComp<MutantInvisibilityUsedComponent>(uid);

    private void OnDamageChanged(EntityUid uid, MutantInvisibilityUsedComponent component, DamageChangedEvent args)
    {
        if (!TryComp<MutantComponent>(uid, out var mutant)
            || !args.DamageIncreased || args.DamageDelta is not null && args.DamageDelta.GetTotal() < component.DamageToStun)
            return;

        ToggleInvisibility(uid);
        _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(component.StunTime), false);
    }

    public void ToggleInvisibility(EntityUid uid)
    {
        if (!HasComp<MutantInvisibilityUsedComponent>(uid))
        {
            EnsureComp<MutantInvisibilityUsedComponent>(uid);
        }
        else
        {
            RemComp<MutantInvisibilityUsedComponent>(uid);
        }
    }
}
