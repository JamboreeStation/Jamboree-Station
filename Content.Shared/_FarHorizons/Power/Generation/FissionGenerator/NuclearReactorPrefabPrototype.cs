// SPDX-FileCopyrightText: 2025 jhrushbe (Far-Horizons-SS14)
// SPDX-FileCopyrightText: The Goonstation Project (nuclear reactor code derivation, per the Far-Horizons-SS14 README)
//
// SPDX-License-Identifier: CC-BY-NC-SA-3.0

using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Power.Generation.FissionGenerator;

[Prototype]
public sealed partial class NuclearReactorPrefabPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("parts")]
    public Dictionary<Vector2i, EntProtoId> ReactorComponents { get; private set; } = [];
}