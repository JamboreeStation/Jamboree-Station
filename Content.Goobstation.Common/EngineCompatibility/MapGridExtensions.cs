// SPDX-License-Identifier: MIT
// Retains the old grid convenience methods while forwarding to engine 293 systems.
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Maths;

using Robust.Shared.Map.Components;

namespace Robust.Shared.GameObjects;

public static class JamboreeMapGridExtensions
{
    public static TileRef GetTileRef(this MapGridComponent grid, EntityCoordinates coords)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GetTileRef(grid.Owner, grid, coords);
    }

    public static TileRef GetTileRef(this MapGridComponent grid, Vector2i tileCoordinates)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GetTileRef(grid.Owner, grid, tileCoordinates);
    }

    public static IEnumerable<TileRef> GetAllTiles(this MapGridComponent grid, bool ignoreEmpty = true)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GetAllTiles(grid.Owner, grid, ignoreEmpty);
    }

    public static GridTileEnumerator GetAllTilesEnumerator(this MapGridComponent grid, bool ignoreEmpty = true)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GetAllTilesEnumerator(grid.Owner, grid, ignoreEmpty);
    }

    public static void SetTile(this MapGridComponent grid, EntityCoordinates coords, Tile tile)
    {
            IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().SetTile(grid.Owner, grid, coords, tile);
    }

    public static void SetTile(this MapGridComponent grid, Vector2i gridIndices, Tile tile)
    {
            IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().SetTile(grid.Owner, grid, gridIndices, tile);
    }

    public static void SetTiles(this MapGridComponent grid, List<(Vector2i GridIndices, Tile Tile)> tiles)
    {
            IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().SetTiles(grid.Owner, grid, tiles);
    }

    public static IEnumerable<TileRef> GetTilesIntersecting(this MapGridComponent grid, Box2 worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GetTilesIntersecting(grid.Owner, grid, worldArea, ignoreEmpty, predicate);
    }

    public static IEnumerable<TileRef> GetLocalTilesIntersecting(this MapGridComponent grid, Box2 localArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GetLocalTilesIntersecting(grid.Owner, grid, localArea, ignoreEmpty, predicate);
    }

    public static IEnumerable<TileRef> GetTilesIntersecting(this MapGridComponent grid, Circle worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GetTilesIntersecting(grid.Owner, grid, worldArea, ignoreEmpty, predicate);
    }

    public static IEnumerable<EntityUid> GetAnchoredEntities(this MapGridComponent grid, MapCoordinates coords)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GetAnchoredEntities(grid.Owner, grid, coords);
    }

    public static IEnumerable<EntityUid> GetAnchoredEntities(this MapGridComponent grid, Vector2i pos)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GetAnchoredEntities(grid.Owner, grid, pos);
    }

    public static AnchoredEntitiesEnumerator GetAnchoredEntitiesEnumerator(this MapGridComponent grid, Vector2i pos)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GetAnchoredEntitiesEnumerator(grid.Owner, grid, pos);
    }

    public static Vector2i TileIndicesFor(this MapGridComponent grid, EntityCoordinates coords)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().TileIndicesFor(grid.Owner, grid, coords);
    }

    public static Vector2i TileIndicesFor(this MapGridComponent grid, MapCoordinates worldPos)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().TileIndicesFor(grid.Owner, grid, worldPos);
    }

    public static IEnumerable<EntityUid> GetCellsInSquareArea(this MapGridComponent grid, EntityCoordinates coords, int n)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GetCellsInSquareArea(grid.Owner, grid, coords, n);
    }

    public static Vector2 WorldToLocal(this MapGridComponent grid, Vector2 posWorld)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().WorldToLocal(grid.Owner, grid, posWorld);
    }

    public static EntityCoordinates MapToGrid(this MapGridComponent grid, MapCoordinates posWorld)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().MapToGrid(grid.Owner, posWorld);
    }

    public static Vector2 LocalToWorld(this MapGridComponent grid, Vector2 posLocal)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().LocalToWorld(grid.Owner, grid, posLocal);
    }

    public static Vector2i WorldToTile(this MapGridComponent grid, Vector2 posWorld)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().WorldToTile(grid.Owner, grid, posWorld);
    }

    public static Vector2i LocalToTile(this MapGridComponent grid, EntityCoordinates coordinates)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().LocalToTile(grid.Owner, grid, coordinates);
    }

    public static Vector2i CoordinatesToTile(this MapGridComponent grid, EntityCoordinates coords)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().CoordinatesToTile(grid.Owner, grid, coords);
    }

    public static EntityCoordinates GridTileToLocal(this MapGridComponent grid, Vector2i gridTile)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GridTileToLocal(grid.Owner, grid, gridTile);
    }

    public static Vector2 GridTileToWorldPos(this MapGridComponent grid, Vector2i gridTile)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().GridTileToWorldPos(grid.Owner, grid, gridTile);
    }

    public static bool TryGetTileRef(this MapGridComponent grid, Vector2i indices, out TileRef tile)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().TryGetTileRef(grid.Owner, grid, indices, out tile);
    }

    public static bool TryGetTileRef(this MapGridComponent grid, EntityCoordinates coords, out TileRef tile)
    {
        return IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().TryGetTileRef(grid.Owner, grid, coords, out tile);
    }

    public static IEnumerable<MapGridComponent> FindGridsIntersecting(this SharedMapSystem maps, MapId mapId, Box2 bounds)
    {
        var grids = new List<Entity<MapGridComponent>>();
        maps.FindGridsIntersecting(mapId, bounds, ref grids);
        return grids.Select(grid => grid.Comp);
    }

    public static IEnumerable<MapGridComponent> FindGridsIntersecting(this SharedMapSystem maps, MapId mapId, Box2Rotated bounds)
    {
        var grids = new List<Entity<MapGridComponent>>();
        maps.FindGridsIntersecting(mapId, bounds, ref grids);
        return grids.Select(grid => grid.Comp);
    }
}
