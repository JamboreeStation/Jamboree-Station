// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

/// <summary>
/// Stun as a status effect.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class StunnedStatusEffectComponent : Component;
