// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Spawners.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Server.Spawners.EntitySystems;

public sealed partial class SpawnOnDespawnSystem : EntitySystem // Starlight edit
{
    [Dependency] private TransformSystem _xform = default!; // Starlight
    private readonly Queue<(EntProtoId Prototype, EntityCoordinates Coordinates, ComponentRegistry? overrides)> _queuedSpawns = new(); // Starlight

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnOnDespawnComponent, TimedDespawnEvent>(OnDespawn);
    }

    // Starlight Start
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Spawn queued entities after all deletions are processed
        while (_queuedSpawns.Count > 0)
        {
            var (prototype, coordinates, overrides) = _queuedSpawns.Dequeue();
            var uid = EntityManager.SpawnAtPosition(prototype, coordinates, overrides);
        }
    }
    // Starlight End

    private void OnDespawn(EntityUid uid, SpawnOnDespawnComponent comp, ref TimedDespawnEvent args)
    {
        if (!TryComp(uid, out TransformComponent? xform))
            return;

        // Lavaland Change start
        if (comp.Prototype != null)
            _queuedSpawns.Enqueue((comp.Prototype.Value, xform.Coordinates, comp.Overrides));
        // Lavaland Change end

        // Lavaland Change start
        // make it spawn more (without intrusion)
        foreach (var prot in comp.Prototypes)
            _queuedSpawns.Enqueue((prot, xform.Coordinates, comp.Overrides));
        // Lavaland Change end
    }

    public void SetPrototype(Entity<SpawnOnDespawnComponent> entity, EntProtoId prototype)
    {
        entity.Comp.Prototype = prototype;
    }

    #region Starlight

    public void SetOverrides(Entity<SpawnOnDespawnComponent> entity, ComponentRegistry? overrides) =>
        entity.Comp.Overrides = overrides;

    #endregion
}
