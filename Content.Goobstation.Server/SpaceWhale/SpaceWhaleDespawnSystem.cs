// SPDX-FileCopyrightText: 2026 Jamboree Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Shared._Jamboree.SpaceWhale;
using Content.Server.Station.Components;
using Content.Shared.Maps;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
// These are all the APIs I need to make this

namespace Content.Goobstation.Server.SpaceWhale;

public sealed partial class SpaceWhaleDespawnSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedPointLightSystem _light = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(10); // This is a timer. Every 10 seconds; it kills leviathans near stations.
    private TimeSpan _nextCheck = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpaceWhaleComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<SpaceWhaleComponent> ent, ref ComponentStartup args)
    {
        _nextCheck = _timing.CurTime + CheckInterval; // When the leviathan spawns, proximity wont happen until 10 seconds after the leviathan spawns.
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextCheck)
            return; // if the current time is under 10 seconds after existence, then do nothing.

        _nextCheck = _timing.CurTime + CheckInterval; // if after 10 seconds, check the proximity to stations.
        CheckWhaleProximity();
    }

    private void CheckWhaleProximity()
    {
        if (!_cfg.GetCVar(GoobCVars.SpaceWhaleSpawn))
            return;

        var stationQuery = EntityQueryEnumerator<BecomesStationComponent, MapGridComponent>();
        var stations = new List<(EntityUid Uid, MapGridComponent Grid, TransformComponent Xform)>();

        while (stationQuery.MoveNext(out var uid, out _, out var grid))
        {
            var xform = Transform(uid);
            stations.Add((uid, grid, xform));
        }

        if (stations.Count == 0)
            return;

        var whaleQuery = EntityQueryEnumerator<SpaceWhaleComponent, TransformComponent>();
        while (whaleQuery.MoveNext(out var whale, out var comp, out var whaleXform))
        {
            foreach (var (_, grid, stationXform) in stations)
            {
                if (stationXform.MapUid != whaleXform.MapUid) // Well, id rather calculate it in the same map. Would be weird if it suddenly vanished elsewhere.
                    continue;

                var whalePos = _transform.GetWorldPosition(whaleXform); // get the world position of the leviathan.
                var stationPos = _transform.GetWorldPosition(stationXform); // ditto
                var distance = (whalePos - stationPos).Length(); // simple math formula to calculate distance from leviathan to station..

                if (grid.LocalAABB.Size.Length() > 0) // AABB means axis-aligned bounding box. This should be the stations.
                {
                    var gridRadius = grid.LocalAABB.Size.Length() / 2f;
                    distance = Math.Max(0, distance - gridRadius);
                }

                var despawnDistance = comp.DespawnDistance > 0
                    ? comp.DespawnDistance
                    : _cfg.GetCVar(GoobCVars.SpaceWhaleSpawnDistance) * 0.5f;

                if (distance <= despawnDistance)
                {
                    var coords = whaleXform.Coordinates;
                    _audio.PlayPvs(
                        new SoundPathSpecifier("/Audio/_Goobstation/Effects/Smites/Thunderstrike/thunderstrike.ogg"),
                        coords);
                    CreateLighting(coords);
                    Spawn("Ash", coords);
                    QueueDel(whale); // Whale goes die if its in the despawn radius. After 10 seconds of course.
                    break;
                }
            }
        }
    }

    private void CreateLighting(Robust.Shared.Map.EntityCoordinates coordinates, int energy = 125, int radius = 15)
    {
        var ent = Spawn(null, coordinates);
        var comp = _light.EnsureLight(ent);
        _light.SetColor(ent, Color.White, comp);
        _light.SetEnergy(ent, energy, comp);
        _light.SetRadius(ent, radius, comp);
        EnsureComp<TimedDespawnComponent>(ent).Lifetime = 0.125f;
    }
}
