// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Jamboree.Telecomms;

/// <summary>
/// A traffic control console: lists the telecom servers on its map and uploads NTSL scripts to them.
/// </summary>
[RegisterComponent]
public sealed partial class NtslConsoleComponent : Component
{
    [ViewVariables]
    public EntityUid? SelectedServer;

    [ViewVariables]
    public string? Output;
}

[Serializable, NetSerializable]
public enum NtslConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class NtslConsoleServerEntry(NetEntity server, string label)
{
    public NetEntity Server { get; } = server;
    public string Label { get; } = label;
}

[Serializable, NetSerializable]
public sealed class NtslConsoleBoundUserInterfaceState(List<NtslConsoleServerEntry> servers, NetEntity? selected, bool enabled, string code, List<string> log, string? output, bool hasPrevious) : BoundUserInterfaceState
{
    public List<NtslConsoleServerEntry> Servers { get; } = servers;
    public NetEntity? Selected { get; } = selected;
    public bool Enabled { get; } = enabled;
    public string Code { get; } = code;
    public List<string> Log { get; } = log;
    public string? Output { get; } = output;
    public bool HasPrevious { get; } = hasPrevious;
}

[Serializable, NetSerializable]
public sealed class NtslConsoleSelectServerMessage(NetEntity server) : BoundUserInterfaceMessage
{
    public NetEntity Server { get; } = server;
}

[Serializable, NetSerializable]
public sealed class NtslConsoleUploadMessage(string code) : BoundUserInterfaceMessage
{
    public string Code { get; } = code;
}

[Serializable, NetSerializable]
public sealed class NtslConsoleClearMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class NtslConsoleRestoreMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class NtslConsoleSetEnabledMessage(bool enabled) : BoundUserInterfaceMessage
{
    public bool Enabled { get; } = enabled;
}

[Serializable, NetSerializable]
public sealed class NtslConsoleRefreshMessage : BoundUserInterfaceMessage;
