// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Jamboree.Telecomms;

/// <summary>
/// A station event that puts an NTSL script on the station's telecom servers.
/// </summary>
[RegisterComponent, Access(typeof(NtslScriptRule))]
public sealed partial class NtslScriptRuleComponent : Component
{
    /// <summary>
    /// The event installs one of these, picked at random.
    /// </summary>
    [DataField(required: true)]
    public List<string> Scripts = new();

    /// <summary>
    /// Put in front of the picked script, like the banner and helper functions of whoever wrote it.
    /// </summary>
    [DataField]
    public string Header = string.Empty;

    /// <summary>
    /// Install it on every telecom server of the station instead of a random one.
    /// </summary>
    [DataField]
    public bool AllServers;

    /// <summary>
    /// When the event ends, servers still running its script get their old script back.
    /// </summary>
    [DataField]
    public bool Restore;

    /// <summary>
    /// Announced once if the crew removes or changes the script before the event ends.
    /// </summary>
    [DataField]
    public LocId? EarlyRemovalAnnouncement;

    [ViewVariables]
    public string Script = string.Empty;

    [ViewVariables]
    public Dictionary<EntityUid, NtslScriptBackup> Replaced = new();

    [ViewVariables]
    public bool EarlyRemovalNoted;

    [ViewVariables]
    public TimeSpan NextRemovalCheck;
}

public readonly record struct NtslScriptBackup(string Code, bool Enabled);
