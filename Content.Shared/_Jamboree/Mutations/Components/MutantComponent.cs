// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.GameStates;

namespace Content.Shared._Jamboree.Mutations;

[RegisterComponent, NetworkedComponent]
public sealed partial class MutantComponent : Component
{
    /// <summary>
    ///     The list of all mutations currently on this mutant, by Prototype.
    /// </summary>
    [DataField(serverOnly: true)]
    public HashSet<MutationPrototype> ActiveMutations = new();

    /// <summary>
    ///     The list of all provided Mutation Abilities by Entity UID
    /// </summary>
    [DataField]
    public Dictionary<string, List<EntityUid>> Actions = new();

    /// Used for tracking what ability a Mutant is actively casting
    [DataField]
    public DoAfterId? DoAfter;

    /// Popup to play if a Psion attempts to start casting a power while already casting one
    [DataField]
    public string AlreadyCasting = "already-casting";
}