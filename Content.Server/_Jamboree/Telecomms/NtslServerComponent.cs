// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Jamboree.Ntsl;

namespace Content.Server._Jamboree.Telecomms;

/// <summary>
/// An NTSL script uploaded to a telecom server! Its process_signal function runs on every radio message the server operates.
/// </summary>
[RegisterComponent]
public sealed partial class NtslServerComponent : Component
{
    [DataField]
    public string Code = string.Empty;

    [DataField]
    public bool Enabled = true;

    [DataField]
    public string? LastEditor;

    /// <summary>
    /// The script this server ran before the last upload, so a console can bring it back.
    /// </summary>
    [DataField]
    public string PreviousCode = string.Empty;

    [ViewVariables]
    public NtslInterpreter? Interpreter;

    [ViewVariables]
    public readonly Dictionary<string, object?> Memory = new();

    [ViewVariables]
    public readonly List<string> Log = new();

    [ViewVariables]
    public TimeSpan NextUpload;
}
