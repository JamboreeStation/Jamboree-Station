// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Add to grids that you do not want to FTL, but still might want to pilot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PreventFTLComponent : Component;
