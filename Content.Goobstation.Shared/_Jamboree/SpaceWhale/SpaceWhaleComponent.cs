// SPDX-FileCopyrightText: 2026 Goob Station Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;

namespace Content.Goobstation.Shared._Jamboree.SpaceWhale;

/// <summary>
/// Marks an entity as a space whale that should be despawned when it gets too close to a station.
/// </summary>
[RegisterComponent]
public sealed partial class SpaceWhaleComponent : Component
{
    /// <summary>
    /// How close (in tiles) a station grid can be before this whale is despawned.
    /// Defaults to -1, which falls back to the server CVar misc.space_whale_spawn_distance * 0.5.
    /// </summary>
    [DataField("despawnDistance")]
    public float DespawnDistance = -1f;
}
