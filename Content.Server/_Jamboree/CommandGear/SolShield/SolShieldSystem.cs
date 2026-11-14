// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Boomerang;
using Content.Shared._Jamboree.CommandGear.SolShield;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Content.Shared.Throwing;

namespace Content.Server._Jamboree.CommandGear.SolShield;

public sealed class SolShieldSystem : EntitySystem
{
    [Dependency] private readonly BoomerangSystem _boomerang = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<SolShieldComponent, ThrowEvent>(OnThrown);
        SubscribeLocalEvent<SolShieldComponent, LandEvent>(OnLanded);
    }

    private void OnThrown(EntityUid uid, SolShieldComponent component, ref ThrowEvent args)
    {
        if (args.User == null)
            return;

        var user = args.User.Value;

        if (TryComp<BoomerangComponent>(uid, out var existing) &&
            existing.Thrower != null)
            return;

        if (!_inventory.TryGetSlotEntity(user, "gloves", out var gloves) ||
            gloves is not {} gloveUid ||
            !_tag.HasTag(gloveUid, "SolGloves"))
        {
            RemCompDeferred<BoomerangComponent>(uid);
            return;
        }

        var boomerang = EnsureComp<BoomerangComponent>(uid);
        _boomerang.SetThrower((uid, boomerang), user);
    }

    private void OnLanded(EntityUid uid, SolShieldComponent component, LandEvent args)
    {
        if (!TryComp<BoomerangComponent>(uid, out var boomerang))
            return;

        if (boomerang.Thrower == null)
        {
            RemCompDeferred<BoomerangComponent>(uid);
            return;
        }

        var thrower = boomerang.Thrower.Value;

        if (!_inventory.TryGetSlotEntity(thrower, "gloves", out var gloves) ||
            !_tag.HasTag(gloves.Value, "SolGloves"))
        {
            RemCompDeferred<BoomerangComponent>(uid);
        }
    }
}
