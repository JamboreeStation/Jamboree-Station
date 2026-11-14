// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Thaven;

[Virtual, DataDefinition]
[Serializable, NetSerializable]
public partial class ThavenMood
{
    [DataField(readOnly: true)]
    public ProtoId<ThavenMoodPrototype> ProtoId = string.Empty;

    /// <summary>
    /// A locale string of the mood name.
    /// </summary>
    [DataField(required: true)]
    public string MoodName = string.Empty;

    /// <summary>
    /// A locale string of the mood description. Gets passed to
    /// <see cref="Loc.GetString"/> with <see cref="MoodVars"/>.
    /// </summary>
    [DataField(required: true)]
    public string MoodDesc = string.Empty;

    [DataField(serverOnly: true)]
    public HashSet<ProtoId<ThavenMoodPrototype>> Conflicts = new();

    /// <summary>
    /// Additional localized words for the <see cref="MoodDesc"/>, for things like random
    /// verbs and nouns.
    /// </summary>
    [DataField]
    public Dictionary<string, string> MoodVars = new();

    public (string, object)[] GetLocArgs()
    {
        return MoodVars.Select(v => (v.Key, (object)v.Value)).ToArray();
    }

    public string GetLocName()
    {
        return Loc.GetString(MoodName, GetLocArgs());
    }

    public string GetLocDesc()
    {
        return Loc.GetString(MoodDesc, GetLocArgs());
    }
}

[Prototype("thavenMood")]
[Serializable, NetSerializable]
public sealed partial class ThavenMoodPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string MoodName = string.Empty;

    [DataField(required: true)]
    public string MoodDesc = string.Empty;

    /// <summary>
    /// A list of mood IDs that this mood will conflict with.
    /// </summary>
    [DataField("conflicts")]
    public HashSet<ProtoId<ThavenMoodPrototype>> Conflicts = new();

    /// <summary>
    /// Extra mood variables that will be randomly chosen and provided
    /// to the <see cref="Loc.GetString"/> call on <see cref="ThavenMood.MoodDesc"/>.
    /// </summary>
    [DataField("moodVars")]
    public Dictionary<string, ProtoId<DatasetPrototype>> MoodVarDatasets = new();

    /// <summary>
    /// If false, prevents the same variable from being rolled twice when rolling
    /// mood variables for this mood. Does not prevent the same mood variable
    /// from being present in other moods.
    /// </summary>
    [DataField]
    public bool AllowDuplicateMoodVars;
}

