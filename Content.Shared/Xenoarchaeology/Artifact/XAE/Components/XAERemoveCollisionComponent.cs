// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
///     Removes the masks/layers of hard fixtures from the artifact when added, allowing it to pass through walls
///     and such.
/// </summary>
[RegisterComponent, Access(typeof(XAERemoveCollisionSystem)), NetworkedComponent]
public sealed partial class XAERemoveCollisionComponent : Component;
