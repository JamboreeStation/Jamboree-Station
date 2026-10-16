// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Jamboree.CustomRadio;

/// <summary>
/// Every custom radio channel of the round (id ot YAML), sent to clients so the chat box can show their custom names.
/// </summary>
[Serializable, NetSerializable]
public sealed class CustomRadioChannelsSyncEvent(Dictionary<string, string> channels) :
EntityEventArgs
{
    public Dictionary<string, string> Channels { get; } = channels;
}