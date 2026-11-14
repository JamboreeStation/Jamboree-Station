// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Tech.Tinkering;

/// <summary>
/// Uses tools, builds, crafts and deconstructs faster.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TinkererComponent : Component
{
    /// <summary>
    /// How long that work takes compared to everyone else!
    /// </summary>
    [DataField]
    public float DelayMultiplier = 0.7f;
}
