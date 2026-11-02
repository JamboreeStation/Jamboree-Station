// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Atmos.Piping.Components;
using Content.Shared._FarHorizons.Power.Generation.FissionGenerator;
using Content.Shared.Eye;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using TurbineSystem = Content.Client._FarHorizons.Power.Generation.FissionGenerator.TurbineSystem;
using VisibilitySystem = Robust.Server.GameObjects.VisibilitySystem;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class TurbineVisualsTest
{
    [TestCase("Turbine")]
    [TestCase("TurbineSmall")]
    public async Task TurbineAnimationStates(string prototype)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        var appearance = client.System<AppearanceSystem>();
        var sprites = client.System<SpriteSystem>();

        await client.WaitAssertion(() =>
        {
            var uid = client.EntMan.Spawn(prototype);
            var turbine = client.EntMan.GetComponent<TurbineComponent>(uid);
            var sprite = client.EntMan.GetComponent<SpriteComponent>(uid);
            var animations = client.System<AnimationPlayerSystem>();
            Assert.That(sprites.LayerMapTryGet((uid, sprite), TurbineVisualLayers.TurbineSpeed, out var rotorId, false));
            Assert.That(sprites.LayerMapTryGet((uid, sprite), TurbineVisualLayers.TurbineRuined, out var ruinedId, false));
            Assert.That(sprites.TryGetLayer((uid, sprite), rotorId, out var rotor, false));
            Assert.That(sprites.TryGetLayer((uid, sprite), ruinedId, out var ruined, false));

            for (var i = 0; i < 12; i++)
                Assert.That(rotor.ActualRsi.TryGetState($"turbinerun_{i:00}", out _));

            // Exercise startup, stopping, destruction with residual RPM, and repair.
            (float RPM, bool Ruined)[] states =
            [
                (0, false),
                (0.5f, false),
                (1, false),
                (1.01f, false),
                (600, false),
                (0, false),
                (600, false),
                (600, true),
                (0, false),
                (600, false),
                (float.NaN, false),
                (float.PositiveInfinity, false),
                (-100, false),
            ];

            foreach (var state in states)
            {
                turbine.RPM = state.RPM;
                turbine.Ruined = state.Ruined;
                appearance.SetData(uid, TurbineVisuals.TurbineRuined, state.Ruined);
                appearance.OnChangeData(uid, sprite);

                var running = !state.Ruined && float.IsFinite(state.RPM) && state.RPM > 1;
                Assert.Multiple(() =>
                {
                    Assert.That(animations.HasRunningAnimation(uid, "turbine-speed"), Is.EqualTo(running));
                    Assert.That(rotor.State.ToString(), Is.EqualTo(running ? "turbinerun_00" : "turbine"));
                    Assert.That(rotor.ActualState, Is.Not.Null);
                    Assert.That(rotor.Visible, Is.EqualTo(!state.Ruined));
                    Assert.That(ruined.Visible, Is.EqualTo(state.Ruined));
                });
            }

            client.EntMan.DeleteEntity(uid);
        });

        await pair.CleanReturnAsync();
    }

    [TestCase("Turbine")]
    [TestCase("TurbineSmall")]
    public async Task TurbineAnimationFollowsRpmAndLoops(string prototype)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;

        await client.WaitAssertion(() =>
        {
            var uid = client.EntMan.Spawn(prototype);
            var turbine = client.EntMan.GetComponent<TurbineComponent>(uid);
            var sprite = client.EntMan.GetComponent<SpriteComponent>(uid);
            var sprites = client.System<SpriteSystem>();
            var animations = client.System<AnimationPlayerSystem>();
            Assert.That(sprites.LayerMapTryGet((uid, sprite), TurbineVisualLayers.TurbineSpeed, out var layerId, false));
            Assert.That(sprites.TryGetLayer((uid, sprite), layerId, out var rotor, false));

            void SetRpm(float rpm)
            {
                turbine.RPM = rpm;
                var ev = new AfterAutoHandleStateEvent();
                client.System<TurbineSystem>().RaiseComponentEvent(uid, turbine, ref ev);
            }

            SetRpm(turbine.BestRPM);
            var player = client.EntMan.GetComponent<AnimationPlayerComponent>(uid);
            animations.FrameUpdate(0.03f);
            Assert.That(rotor.State.ToString(), Is.EqualTo("turbinerun_00"));
            animations.FrameUpdate(0.02f);
            Assert.That(rotor.State.ToString(), Is.EqualTo("turbinerun_01"));

            // Twice the RPM advances frames twice as fast.
            SetRpm(turbine.BestRPM * 2);
            Assert.That(rotor.State.ToString(), Is.EqualTo("turbinerun_00"));
            animations.FrameUpdate(0.03f);
            Assert.That(rotor.State.ToString(), Is.EqualTo("turbinerun_01"));

            SetRpm(turbine.BestRPM * 2.05f);
            Assert.That(turbine.AnimRPM, Is.EqualTo(turbine.BestRPM * 2), "Small RPM changes should not restart the cycle.");
            Assert.That(rotor.State.ToString(), Is.EqualTo("turbinerun_01"));

            animations.FrameUpdate(0.5f);
            Assert.That(player.PlayingAnimationCount, Is.EqualTo(1), "Natural completion should loop exactly once.");
            Assert.That(rotor.State.ToString(), Is.EqualTo("turbinerun_00"));

            animations.Stop(uid, player, "turbine-speed");
            Assert.That(player.PlayingAnimationCount, Is.Zero, "Explicit stops must not synchronously restart playback.");
            client.EntMan.EventBus.RaiseLocalEvent(uid, new AnimationCompletedEvent(uid, player, "unrelated"));
            Assert.That(player.PlayingAnimationCount, Is.Zero);

            client.System<TurbineSystem>().FrameUpdate(1f);
            Assert.That(player.PlayingAnimationCount, Is.EqualTo(1), "The periodic update should recover missing playback.");

            SetRpm(0);
            Assert.That(player.PlayingAnimationCount, Is.Zero);
            Assert.That(rotor.State.ToString(), Is.EqualTo("turbine"));
            SetRpm(2);
            Assert.That(player.PlayingAnimationCount, Is.EqualTo(1), "Low RPM must restart immediately after stopping.");
            SetRpm(50);
            Assert.That(turbine.AnimRPM, Is.EqualTo(50), "Large relative changes at low RPM must not wait minutes for a cycle to finish.");

            client.EntMan.DeleteEntity(uid);
        });

        await pair.CleanReturnAsync();
    }

    [TestCase("Turbine")]
    [TestCase("TurbineSmall")]
    public async Task TurbineDamageSpawnsClientEffects(string prototype)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;

        await client.WaitAssertion(() =>
        {
            var map = client.System<SharedMapSystem>().CreateMap();
            Assert.That(client.ResolveDependency<IResourceCache>()
                .TryGetResource<AudioResource>("/Audio/_FarHorizons/Ambience/Objects/bad_bearing.ogg", out _), Is.True);
            var uid = client.EntMan.SpawnAttachedTo(prototype, new EntityCoordinates(map, 0, 0));
            var appearance = client.System<AppearanceSystem>();
            var visuals = client.System<TurbineSystem>();

            MetaDataComponent[] Effects() => client.EntMan.EntityQuery<MetaDataComponent>()
                .Where(meta => meta.EntityPrototype?.ID is "GasTurbineSparkEffect" or "GasTurbineSmokeEffect")
                .ToArray();

            visuals.FrameUpdate(1f);
            Assert.That(Effects(), Is.Empty);

            appearance.SetData(uid, TurbineVisuals.DamageSpark, true);
            appearance.SetData(uid, TurbineVisuals.DamageSmoke, true);
            visuals.FrameUpdate(1f);
            var effects = Effects();
            Assert.That(effects.Select(meta => meta.EntityPrototype!.ID),
                Is.EquivalentTo(new[] { "GasTurbineSparkEffect", "GasTurbineSmokeEffect" }));
            foreach (var effect in effects)
            {
                var sprite = client.EntMan.GetComponent<SpriteComponent>(effect.Owner);
                Assert.That(sprite.BaseRSI, Is.Not.Null);
                Assert.That(client.EntMan.GetComponent<TransformComponent>(effect.Owner).ParentUid, Is.EqualTo(map));
            }

            appearance.SetData(uid, TurbineVisuals.DamageSpark, false);
            appearance.SetData(uid, TurbineVisuals.DamageSmoke, false);
            visuals.FrameUpdate(1f);
            Assert.That(Effects(), Has.Length.EqualTo(2), "Repair must stop further effect spawns.");

            client.EntMan.DeleteEntity(map);
        });

        await pair.CleanReturnAsync();
    }

    [TestCase("Turbine")]
    [TestCase("TurbineSmall")]
    public async Task TurbineVisualsReplicateAcrossPvsChanges(string prototype)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Dirty = true });
        var (server, client) = pair;
        var map = await pair.CreateTestMap();
        EntityUid uid = default;
        var previousActor = pair.Player!.AttachedEntity;

        await server.WaitPost(() =>
        {
            var viewer = server.EntMan.SpawnAtPosition("MobHuman", map.GridCoords);
            server.PlayerMan.SetAttachedEntity(pair.Player, viewer);
            server.CfgMan.SetCVar(CVars.NetPVS, true);
            uid = server.EntMan.SpawnAtPosition(prototype, map.GridCoords);
            // Isolate presentation from the atmosphere simulation changing RPM.
            server.EntMan.RemoveComponent<AtmosDeviceComponent>(uid);
            var turbine = server.EntMan.GetComponent<TurbineComponent>(uid);
            turbine.RPM = 600;
            server.EntMan.Dirty(uid, turbine);
        });
        await pair.RunTicksSync(10);

        void AssertVisuals(float rpm, bool ruined)
        {
            var clientUid = pair.ToClientUid(uid);
            var turbine = client.EntMan.GetComponent<TurbineComponent>(clientUid);
            var sprites = client.System<SpriteSystem>();
            Assert.That((client.MetaData(clientUid).Flags & MetaDataFlags.Detached) == 0, Is.True);
            Assert.That(turbine.RPM, Is.EqualTo(rpm));
            Assert.That(turbine.Ruined, Is.EqualTo(ruined));
            Assert.That(client.System<AnimationPlayerSystem>().HasRunningAnimation(clientUid, "turbine-speed"),
                Is.EqualTo(!ruined && rpm > 1));
            Assert.That(sprites.LayerMapTryGet(clientUid, TurbineVisualLayers.TurbineSpeed, out var layerId, false));
            Assert.That(sprites.TryGetLayer(clientUid, layerId, out var rotor, false));
            Assert.That(rotor.Visible, Is.EqualTo(!ruined));
        }

        await client.WaitAssertion(() => AssertVisuals(600, false));
        await server.WaitPost(() =>
        {
            var turbine = server.EntMan.GetComponent<TurbineComponent>(uid);
            turbine.RPM = 0;
            server.EntMan.Dirty(uid, turbine);
        });
        await pair.RunTicksSync(10);
        await client.WaitAssertion(() => AssertVisuals(0, false));

        await server.WaitPost(() =>
        {
            var turbine = server.EntMan.GetComponent<TurbineComponent>(uid);
            turbine.RPM = 600;
            server.EntMan.Dirty(uid, turbine);
        });
        await pair.RunTicksSync(10);
        await client.WaitAssertion(() => AssertVisuals(600, false));

        await server.WaitPost(() => server.System<VisibilitySystem>().AddLayer(uid, (int)VisibilityFlags.Ghost));
        await pair.RunTicksSync(10);
        await client.WaitAssertion(() =>
            Assert.That((client.MetaData(pair.ToClientUid(uid)).Flags & MetaDataFlags.Detached) != 0, Is.True));

        await server.WaitPost(() =>
        {
            var turbine = server.EntMan.GetComponent<TurbineComponent>(uid);
            turbine.RPM = 600;
            turbine.Ruined = true;
            server.EntMan.Dirty(uid, turbine);
            server.System<SharedAppearanceSystem>().SetData(uid, TurbineVisuals.TurbineRuined, true);
            server.System<VisibilitySystem>().RemoveLayer(uid, (int)VisibilityFlags.Ghost);
        });
        await pair.RunTicksSync(10);
        await client.WaitAssertion(() => AssertVisuals(600, true));

        await server.WaitPost(() =>
        {
            var turbine = server.EntMan.GetComponent<TurbineComponent>(uid);
            turbine.RPM = 2;
            turbine.Ruined = false;
            server.EntMan.Dirty(uid, turbine);
            server.System<SharedAppearanceSystem>().SetData(uid, TurbineVisuals.TurbineRuined, false);
        });
        await pair.RunTicksSync(10);
        await client.WaitAssertion(() => AssertVisuals(2, false));

        await server.WaitPost(() =>
        {
            server.PlayerMan.SetAttachedEntity(pair.Player, previousActor);
            server.CfgMan.SetCVar(CVars.NetPVS, false);
            server.EntMan.DeleteEntity(map.MapUid);
        });
        await pair.RunTicksSync(5);
        await pair.CleanReturnAsync();
    }
}
