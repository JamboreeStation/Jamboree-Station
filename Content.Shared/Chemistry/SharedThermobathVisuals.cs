// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry;

[Serializable, NetSerializable]
public enum ThermobathVisuals : byte
{
    Powered,
    HasBeaker,
    ActiveMode
}

public enum ThermobathVisualLayers : byte
{
    PowerOn,
    PowerOff,
    Heating,
    Cooling,
    Open,
    Beaker,
    LidIdle,
    LidCooling,
    LidHeating
}
