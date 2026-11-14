// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;

namespace Content.Shared.Radio.Components;

[RegisterComponent]
public sealed partial class RelayComponent : Component
{
    [DataField("isActive")]
    public bool IsActive = true;

    public bool BoostsLongRange => IsActive;
}
