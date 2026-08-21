// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Tech.Clothing;

[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingEquipmentRequirementsComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;
}
