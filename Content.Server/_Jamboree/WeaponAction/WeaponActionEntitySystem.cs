// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Knockdown;
using Content.Shared._Jamboree.WeaponAction;
using Content.Shared._Shitmed.Targeting;
using Content.Shared._White.Standing;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Projectiles;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Server._Jamboree.WeaponAction;

public sealed class WeaponActionAttackSystem : EntitySystem

{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<WeaponActionEntityComponent, StartCollideEvent>(OnHit);
    }

    private void OnHit(Entity<WeaponActionEntityComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<MobStateComponent>(args.OtherEntity))
            return;

        _damage.TryChangeDamage(args.OtherEntity, ent.Comp.Damage, targetPart: TargetBodyPart.Chest);

        if (!ent.Comp.Knockdown)
        {
            return;
        }
        _stun.TryKnockdown(args.OtherEntity, TimeSpan.FromSeconds(2), true);
        _audio.PlayPvs(ent.Comp.HitSound, ent, new AudioParams(-2f, 1f, SharedAudioSystem.DefaultSoundRange, 1f, false, 0f));
    }
}
