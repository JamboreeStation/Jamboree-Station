// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Damage.Systems;
using Content.Shared._EinsteinEngines.Silicon.Components;
using Content.Shared._Imp.Drone;
using Content.Shared._Jamboree.CommandGear.CaptainFlag;
using Content.Shared._Shitmed.Damage;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC;
using Content.Shared.NukeOps;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Jamboree.CommandGear.CaptainFlag;

public sealed class BuffNearbyActionSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ExamineSystemShared _occlusion = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<CaptainRallyActionEvent>(OnCaptainRally);
        SubscribeLocalEvent<CaptainRallySpeedModifiedComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnCaptainRally(CaptainRallyActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var performer = args.Performer;
        var action = args.Action;

        if (!TryComp<CaptainRallyActionComponent>(action, out var comp))
            return;

        var targets = _entityLookup.GetEntitiesInRange(performer, comp.Range)
            .Where(e =>
                HasComp<MobStateComponent>(e) // blacklisted components
                && !_mobState.IsDead(e)
                && !HasComp<SiliconComponent>(e)
                && !HasComp<NukeOperativeComponent>(e)
                && !HasComp<ActiveNPCComponent>(e)
                && !HasComp<DroneComponent>(e)
                && !HasComp<SiliconLawBoundComponent>(e))
            .Where(e => _occlusion.InRangeUnOccluded(performer, e, comp.Range));


        foreach (var target in targets)
        {
            _damageable.TryChangeDamage(target,
                comp.Healing,
                targetPart: TargetBodyPart.All,
                ignoreBlockers: true,
                splitDamage: SplitDamageBehavior.SplitEnsureAll);
            Spawn(comp.RalliedEffect, Transform(target).Coordinates); // spawn effect on the target

            EnsureComp<CaptainRallySpeedModifiedComponent>(target);
            _movement.RefreshMovementSpeedModifiers(target);

            Timer.Spawn(TimeSpan.FromSeconds(5),
                    () =>
                    {
                        if (Deleted(target))
                            return;

                        RemComp<CaptainRallySpeedModifiedComponent>(target);
                        _movement.RefreshMovementSpeedModifiers(target);
                    })
                ;
        }
        Spawn(comp.RallyEffect, Transform(performer).Coordinates); // spawn effect on the user
        _audio.PlayPvs(comp.RallySoundPath, performer, new AudioParams(-2f, 1f, SharedAudioSystem.DefaultSoundRange, 1f, false, 0f));
    }

    private void OnRefresh(Entity<CaptainRallySpeedModifiedComponent> ent, ref RefreshMovementSpeedModifiersEvent ev)
    {
        ev.ModifySpeed(ent.Comp.WalkSpeedMultiplier, ent.Comp.SprintSpeedMultiplier);
    }
}
