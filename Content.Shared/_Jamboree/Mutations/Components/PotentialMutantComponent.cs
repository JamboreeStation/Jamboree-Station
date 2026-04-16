// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Random;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Jamboree.Mutations;

[RegisterComponent, NetworkedComponent]
public sealed partial class PotentialMutantComponent : Component
{
    /// <summary>
    ///     The list of mutations that this potential mutant can roll into
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype> MutationPool = "RandomMutationPool";

    /// <summary>
    ///     The damage threshold from a single effect in which to gain a mutation.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier MutateDamageThreshold = default!;
}