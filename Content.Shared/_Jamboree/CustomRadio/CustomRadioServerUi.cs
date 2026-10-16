// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Jamboree.CustomRadio;

[Serializable, NetSerializable]
public enum CustomRadioServerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CustomRadioChannelEntry(string id, string name, string key, Color color, int headsets)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string Key { get; } = key;
    public Color Color { get; } = color;
    public int Headsets { get; } = headsets;
}

[Serializable, NetSerializable]
public sealed class CustomRadioServerBoundUserInterfaceState(List<CustomRadioChannelEntry> channels) : BoundUserInterfaceState
{
    public List<CustomRadioChannelEntry> Channels { get; } = channels;
}

[Serializable, NetSerializable]
public sealed class CustomRadioServerEditMessage(string channelId, string name, string key, Color color) : BoundUserInterfaceMessage
{
    public string ChannelId { get; } = channelId;
    public string Name { get; } = name;
    public string Key { get; } = key;
    public Color Color { get; } = color;
}