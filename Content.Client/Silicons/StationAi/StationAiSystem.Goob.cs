// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map.Components;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    public bool IsVisibleToLocalAi(EntityUid uid)
    {
        if (_overlay == null)
            return true;

        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        return _overlay.IsTileVisible(gridUid, Maps.LocalToTile(gridUid, grid, xform.Coordinates));
    }
}
