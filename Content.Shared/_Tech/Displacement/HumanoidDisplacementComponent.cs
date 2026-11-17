// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DisplacementMap;

namespace Content.Shared._Tech.Displacement;

/// <summary>
/// Bends the whole body sprite with a displacement map!
/// </summary>
[RegisterComponent]
public sealed partial class HumanoidDisplacementComponent : Component
{
    [DataField(required: true)]
    public DisplacementData Displacement = default!;

    [ViewVariables]
    public List<string> AppliedLayers = new();
}