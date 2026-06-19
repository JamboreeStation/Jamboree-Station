// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization; // Jamboree

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

    /// <summary>
    ///     Currently active suppression sources keyed by a stable id (e.g.
    ///     "InhibitorCollar", "PsionicInsulation"). A mutation is suppressed if
    ///     any source's filter matches it. While suppressed, the mutation's
    ///     RemovalFunctions have been run and InitializeFunctions are NOT applied
    ///     when the mutation is gained.
    /// </summary>
    [DataField(serverOnly: true)]
    public Dictionary<string, MutationSuppressionFilter> SuppressionSources = new();
}

[Serializable, NetSerializable]
public enum MutationSuppressionFilter : byte
{
    /// <summary>Suppresses every active mutation.</summary>
    All,
    /// <summary>Suppresses only mutations marked <c>IsMental: true</c>.</summary>
    Mental,
}