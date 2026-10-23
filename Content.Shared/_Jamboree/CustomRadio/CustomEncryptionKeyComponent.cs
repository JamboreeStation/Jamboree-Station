// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Jamboree.CustomRadio;

/// <summary>
/// This is an encryption key that players can set up with their own custom radio channels.
/// </summary>
[RegisterComponent]
public sealed partial class CustomEncryptionKeyComponent : Component
{
    /// <summary>
    /// The custom channel that this key was set up with, otherwise it is null while blank.
    /// </summary>
    [DataField]
    public string? ChannelId;

    [DataField]
    public ProtoId<ToolQualityPrototype> QualityNeeded = SharedToolSystem.PulseQuality;
}

[Serializable, NetSerializable]
public enum CustomEncryptionKeyUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CustomEncryptionKeySetupMessage(string name, string key, Color color) : BoundUserInterfaceMessage
{
    public string Name { get; } = name;
    public string Key { get; } = key;
    public Color Color { get; } = color;
}

[Serializable, NetSerializable]
public enum CustomEncryptionKeyVisuals : byte
{
    Color,
}

public enum CustomEncryptionKeyLayers : byte
{
    Base,
}