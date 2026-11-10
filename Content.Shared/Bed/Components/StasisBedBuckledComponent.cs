// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Components;

/// <summary>
/// Tracking component added to entities buckled to stasis beds.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BedSystem))]
public sealed partial class StasisBedBuckledComponent : Component;
