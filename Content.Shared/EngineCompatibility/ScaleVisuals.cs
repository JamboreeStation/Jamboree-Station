// SPDX-FileCopyrightText: 2019 Space Station 14 Contributors
// SPDX-License-Identifier: MIT
// Adapted from RobustToolbox 56eda3ea92913e60c27aa029ba7877c8559dfe9d.

using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects;

[Serializable, NetSerializable]
public enum ScaleVisuals : byte
{
    // Blep
    Scale,
}
