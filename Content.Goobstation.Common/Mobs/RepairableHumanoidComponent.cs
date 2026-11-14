// SPDX-FileCopyrightText: 2026 QuantumPotatoGlitch <219327875+QuantumPotatoGlitch@users.noreply.github.com>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;

/// <summary>
/// Used for species that can be repaired with welders
/// but have Shitmed bodies, like IPCs.
/// All the actual component does is get detected by <see cref="Content.Shared.Repairable.RepairableSystem"/>.
/// </summary>

namespace Content.Goobstation.Common.Mobs;

[RegisterComponent]
public sealed partial class RepairableHumanoidComponent : Component
{
}
