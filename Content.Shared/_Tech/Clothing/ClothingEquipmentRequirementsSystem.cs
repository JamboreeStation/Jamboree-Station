// SPDX-FileCopyrightText: 2026 Tornado Tech <54727692+TornadoTechnology@users.noreply.github.com>
// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
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

        args.Reason = entity.Comp.Reason;
        args.Cancel();
    }
}
