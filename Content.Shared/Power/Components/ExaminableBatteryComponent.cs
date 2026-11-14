// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
/// Allows the charge of a battery to be seen by examination.
/// Requires <see cref="BatteryComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExaminableBatteryComponent : Component;
