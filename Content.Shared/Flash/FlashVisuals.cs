// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Flash;

[Serializable, NetSerializable]
public enum FlashVisuals : byte
{
    Burnt,
    Flashing,
}

[Serializable, NetSerializable]
public enum FlashVisualLayers : byte
{
    BaseLayer,
    LightLayer,
}
