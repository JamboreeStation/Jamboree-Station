// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory.Events;
using Content.Shared.Whitelist;

namespace Content.Shared._Tech.Clothing;

public sealed class ClothingEquipmentRequirementsSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = null!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ClothingEquipmentRequirementsComponent, BeingEquippedAttemptEvent>(OnAttempt);
    }

    private void OnAttempt(Entity<ClothingEquipmentRequirementsComponent> entity, ref BeingEquippedAttemptEvent args)
    {
        if (_whitelist.IsWhitelistPassOrNull(entity.Comp.Whitelist, args.EquipTarget))
            return;

        args.Cancel();
    }
}
