using Content.Shared._Shitmed.Weapons.Ranged.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Random;

namespace Content.Shared._Tech.Weapon;

public sealed class DropDualWieldWeaponSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = null!;
    [Dependency] private readonly SharedHandsSystem _hands = null!;
    [Dependency] private readonly ThrowingSystem _throwing = null!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropDualWieldWeaponComponent, GunShotBodyEvent>(OnShot);
    }

    private void OnShot(Entity<DropDualWieldWeaponComponent> entity, ref GunShotBodyEvent args)
    {
        if (!HasComp<GunRequiresWieldComponent>(args.GunUid))
            return;

        _hands.TryDrop(entity.Owner, Transform(entity).Coordinates);

        var direction = _random.NextAngle().ToVec();
        _throwing.TryThrow(args.GunUid, direction);
    }
}
