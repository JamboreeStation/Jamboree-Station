// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Forensics;
using Content.Shared._Funkystation.Fluids;
using Content.Shared._Funkystation.Stains.Components;
using Content.Shared._Funkystation.Stains.Systems;
using Content.Shared._Funkystation.WashingMachine;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Destructible;
using Content.Shared.Forensics.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Power;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Funkystation;

[TestFixture]
public sealed class StainsTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: StainsTestUniform
  components:
  - type: Item
    size: Tiny
  - type: Clothing
    slots: [innerclothing]
  - type: Appearance
  - type: Stainable
    stainChance: 1
    spillTransferAmount: 0.1
    maxStainVolume: 1
  - type: SolutionContainerManager
    solutions:
      stain:
        maxVol: 1

- type: entity
  id: StainsTestWearer
  components:
  - type: Inventory
  - type: ContainerContainer

- type: entity
  id: StainsTestBlocker
  components:
  - type: Item
    size: Tiny
  - type: Clothing
    slots: [outerclothing]
  - type: StainBlocker
    slots: [innerclothing]

- type: entity
  id: StainsTestWasher
  components:
  - type: WashingMachine
    washTime: 60
    cooldown: 0
    bluntDamagePerSecond: 0
    selfDamagePerSecond: 0
    waterSprayChance: 0
    thumpSoundChance: 0
  - type: EntityStorage
    open: false
    airtight: false
  - type: Forensics
  - type: Appearance

- type: entity
  id: StainsTestHolder
  components:
  - type: Hands
  - type: Sprite
  - type: ContainerContainer
";

    [Test]
    public async Task ProductionPrototypesSpawnWithTheirDependencies()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var spawned = new Dictionary<string, EntityUid>();

        await server.WaitAssertion(() =>
        {
            foreach (var prototype in new[]
            {
                "WashingMachine",
                "WashingMachineFlatpack",
                "WashingMachineCircuitboard",
                "CrateServiceWashingMachineSet",
                "ClothingUniformJumpsuitColorGrey",
            })
            {
                spawned.Add(prototype, server.EntMan.SpawnEntity(prototype, map.GridCoords));
            }
        });

        await server.WaitRunTicks(2);

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            foreach (var (prototype, entity) in spawned)
            {
                Assert.That(entMan.Deleted(entity), Is.False, prototype);
                Assert.That(entMan.IsQueuedForDeletion(entity), Is.False, prototype);
                Assert.That(entMan.GetComponent<MetaDataComponent>(entity).EntityPrototype!.ID, Is.EqualTo(prototype));
            }

            var machine = spawned["WashingMachine"];
            Assert.That(entMan.HasComponent<WashingMachineComponent>(machine), Is.True);
            Assert.That(entMan.HasComponent<EntityStorageComponent>(machine), Is.True);
            Assert.That(entMan.HasComponent<ForensicsComponent>(machine), Is.True);
            var uniform = spawned["ClothingUniformJumpsuitColorGrey"];
            Assert.That(entMan.HasComponent<StainableComponent>(uniform), Is.True);
            Assert.That(server.System<SharedSolutionContainerSystem>()
                .TryGetSolution(uniform, "stain", out _, out var stain), Is.True);
            Assert.That(stain.MaxVolume, Is.EqualTo(FixedPoint2.New(1)));
        });

        await pair.CleanReturnAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PhysicalSourceSpillRetainsBloodDnaAndFiltersWater(bool mixedWithWater)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var solutions = server.System<SharedSolutionContainerSystem>();
            var source = entMan.SpawnEntity(null, map.GridCoords);
            var uniform = entMan.SpawnEntity("StainsTestUniform", map.GridCoords);
            Assert.That(entMan.HasComponent<SolutionComponent>(source), Is.False);
            Assert.That(solutions.TryGetSolution(uniform, "stain", out _, out var stain), Is.True);

            var freshness = TimeSpan.FromSeconds(123);
            var spill = Blood("spill-donor", FixedPoint2.New(1), freshness);
            if (mixedWithWater)
                spill.AddReagent("Water", FixedPoint2.New(1));

            var originalVolume = spill.Volume;
            entMan.EventBus.RaiseLocalEvent(uniform, new SpilledOnEvent(source, spill));

            var expectedBlood = FixedPoint2.New(mixedWithWater ? 0.05f : 0.1f);
            Assert.Multiple(() =>
            {
                Assert.That(spill.Volume, Is.EqualTo(originalVolume - FixedPoint2.New(0.1f)));
                Assert.That(stain.Volume, Is.EqualTo(expectedBlood));
                Assert.That(stain.GetTotalPrototypeQuantity("Blood"), Is.EqualTo(expectedBlood));
                Assert.That(stain.GetTotalPrototypeQuantity("Water"), Is.EqualTo(FixedPoint2.Zero));
                Assert.That(server.System<ForensicsSystem>().GetSolutionsDNA(stain),
                    Is.EquivalentTo(new[] { ("spill-donor", freshness) }));
                Assert.That(server.System<ForensicsSystem>().GetSolutionsDNA(spill),
                    Is.EquivalentTo(new[] { ("spill-donor", freshness) }));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task WaterSpillDoesNotStain()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var source = server.EntMan.SpawnEntity(null, map.GridCoords);
            var uniform = server.EntMan.SpawnEntity("StainsTestUniform", map.GridCoords);
            var spill = new Solution("Water", FixedPoint2.New(1));
            Assert.That(server.System<SharedSolutionContainerSystem>()
                .TryGetSolution(uniform, "stain", out _, out var stain), Is.True);

            server.EntMan.EventBus.RaiseLocalEvent(uniform, new SpilledOnEvent(source, spill));

            Assert.Multiple(() =>
            {
                Assert.That(spill.Volume, Is.EqualTo(FixedPoint2.New(0.9f)));
                Assert.That(stain.Volume, Is.EqualTo(FixedPoint2.Zero));
                Assert.That(stain.Contents, Is.Empty);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task FullStainReplacesOldSolutionWithoutExceedingCapacity()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var solutions = server.System<SharedSolutionContainerSystem>();
            var source = server.EntMan.SpawnEntity(null, map.GridCoords);
            var uniform = server.EntMan.SpawnEntity("StainsTestUniform", map.GridCoords);
            Assert.That(solutions.TryGetSolution(uniform, "stain", out var solution, out var stain), Is.True);
            Assert.That(solutions.TryAddSolution(solution.Value, new Solution("Water", FixedPoint2.New(1))), Is.True);

            var spill = Blood("new-donor", FixedPoint2.New(1));
            server.EntMan.EventBus.RaiseLocalEvent(uniform, new SpilledOnEvent(source, spill));

            Assert.Multiple(() =>
            {
                Assert.That(stain.MaxVolume, Is.EqualTo(FixedPoint2.New(1)));
                Assert.That(stain.Volume, Is.EqualTo(stain.MaxVolume));
                Assert.That(stain.GetTotalPrototypeQuantity("Water"), Is.EqualTo(FixedPoint2.New(0.9f)));
                Assert.That(stain.GetTotalPrototypeQuantity("Blood"), Is.EqualTo(FixedPoint2.New(0.1f)));
                Assert.That(spill.Volume, Is.EqualTo(FixedPoint2.New(0.9f)));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task EquippedBlockerProtectsUniformUntilUnequipped()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var inventory = server.System<InventorySystem>();
            var wearer = entMan.SpawnEntity("StainsTestWearer", map.GridCoords);
            var uniform = entMan.SpawnEntity("StainsTestUniform", map.GridCoords);
            var blocker = entMan.SpawnEntity("StainsTestBlocker", map.GridCoords);
            var source = entMan.SpawnEntity(null, map.GridCoords);
            Assert.That(inventory.TryEquip(wearer, uniform, "jumpsuit"), Is.True);
            Assert.That(inventory.TryEquip(wearer, blocker, "outerClothing"), Is.True);
            Assert.That(server.System<SharedSolutionContainerSystem>()
                .TryGetSolution(uniform, "stain", out _, out var stain), Is.True);

            var spill = Blood("wearer-donor", FixedPoint2.New(1));
            entMan.EventBus.RaiseLocalEvent(wearer, new SpilledOnEvent(source, spill));
            Assert.Multiple(() =>
            {
                Assert.That(stain.Volume, Is.EqualTo(FixedPoint2.Zero));
                Assert.That(spill.Volume, Is.EqualTo(FixedPoint2.New(1)));
            });

            Assert.That(inventory.TryUnequip(wearer, "outerClothing"), Is.True);
            entMan.EventBus.RaiseLocalEvent(wearer, new SpilledOnEvent(source, spill));
            Assert.Multiple(() =>
            {
                Assert.That(stain.GetTotalPrototypeQuantity("Blood"), Is.EqualTo(FixedPoint2.New(0.1f)));
                Assert.That(spill.Volume, Is.EqualTo(FixedPoint2.New(0.9f)));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SolutionChangesUpdateStainOwnerAppearance()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var uniform = server.EntMan.SpawnEntity("StainsTestUniform", map.GridCoords);
            var solutions = server.System<SharedSolutionContainerSystem>();
            var appearance = server.System<SharedAppearanceSystem>();
            Assert.That(solutions.TryGetSolution(uniform, "stain", out var solution, out _), Is.True);
            Assert.That(solutions.TryAddSolution(solution.Value, Blood("visual-donor", FixedPoint2.New(1))), Is.True);
            Assert.That(appearance.TryGetData(uniform, StainVisuals.Toggle, out bool before), Is.True);

            solutions.RemoveAllSolution(solution.Value);

            Assert.That(appearance.TryGetData(uniform, StainVisuals.Toggle, out bool after), Is.True);
            Assert.That(after, Is.Not.EqualTo(before));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ReplicatedProductionUniformStainsRefreshBothHandsWithoutMutatingTemplates()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var client = pair.Client;
        var map = await pair.CreateTestMap();
        var locations = new[] { HandLocation.Left, HandLocation.Right };
        var uniforms = new Dictionary<HandLocation, EntityUid>();
        var templateColors = new Dictionary<HandLocation, Color?[]>();
        var expectedColors = new Dictionary<HandLocation, Color>();
        var stainKeys = new HashSet<string>();
        EntityUid holder = default;
        EntityUid? previousActor = null;

        await server.WaitAssertion(() =>
        {
            holder = server.EntMan.SpawnEntity("StainsTestHolder", map.GridCoords);
            var hands = server.System<SharedHandsSystem>();
            foreach (var location in locations)
            {
                var hand = location.ToString().ToLowerInvariant();
                hands.AddHand(holder, hand, location);
                var uniform = server.EntMan.SpawnEntity("ClothingUniformJumpsuitColorGrey", map.GridCoords);
                uniforms.Add(location, uniform);
                Assert.That(hands.TryPickup(holder, uniform, hand, checkActionBlocker: false, animate: false), Is.True);
            }

            previousActor = pair.Player!.AttachedEntity;
            server.PlayerMan.SetAttachedEntity(pair.Player, holder);
        });

        await pair.RunTicksSync(10);

        await client.WaitAssertion(() =>
        {
            var entMan = client.EntMan;
            var clientHolder = pair.ToClientUid(holder);
            foreach (var location in locations)
            {
                var uniform = pair.ToClientUid(uniforms[location]);
                var stainable = entMan.GetComponent<StainableComponent>(uniform);
                var templates = stainable.ItemVisuals[location.ToString().ToLowerInvariant()];
                Assert.That(templates, Is.Not.Empty);
                templateColors.Add(location, templates.Select(layer => layer.Color).ToArray());
                var visuals = new GetInhandVisualsEvent(clientHolder, location);
                entMan.EventBus.RaiseLocalEvent(uniform, visuals);
                Assert.That(visuals.Layers.Any(layer => layer.Item1.StartsWith("stain-")), Is.False);
            }

            Assert.That(entMan.GetComponent<HandsComponent>(clientHolder).RevealedLayers
                .Values.SelectMany(keys => keys).Any(key => key.StartsWith("stain-")), Is.False);
        });

        await server.WaitAssertion(() =>
        {
            var solutions = server.System<SharedSolutionContainerSystem>();
            foreach (var location in locations)
            {
                Assert.That(solutions.TryGetSolution(uniforms[location], "stain", out var solution, out var stain), Is.True);
                var reagent = location == HandLocation.Left ? "Blood" : "Copper";
                Assert.That(solutions.TryAddSolution(solution.Value, new Solution(reagent, FixedPoint2.New(1))), Is.True);
                expectedColors.Add(location, stain.GetColor(server.ResolveDependency<IPrototypeManager>()));
            }

            Assert.That(expectedColors[HandLocation.Left], Is.Not.EqualTo(expectedColors[HandLocation.Right]));
        });

        await pair.RunTicksSync(10);

        await client.WaitAssertion(() =>
        {
            var entMan = client.EntMan;
            var clientHolder = pair.ToClientUid(holder);
            var sprites = client.System<SpriteSystem>();
            var hands = entMan.GetComponent<HandsComponent>(clientHolder);
            foreach (var location in locations)
            {
                var uniform = pair.ToClientUid(uniforms[location]);
                Assert.That(client.System<SharedSolutionContainerSystem>()
                    .TryGetSolution(uniform, "stain", out _, out var stain), Is.True);
                Assert.That(stain.Volume, Is.EqualTo(FixedPoint2.New(1)));
                var templates = entMan.GetComponent<StainableComponent>(uniform)
                    .ItemVisuals[location.ToString().ToLowerInvariant()];
                var visuals = new GetInhandVisualsEvent(clientHolder, location);
                entMan.EventBus.RaiseLocalEvent(uniform, visuals);
                var layers = visuals.Layers.Where(layer => layer.Item1.StartsWith("stain-")).ToArray();
                Assert.That(layers.Length, Is.EqualTo(templates.Count));
                for (var i = 0; i < layers.Length; i++)
                {
                    var (key, layer) = layers[i];
                    Assert.That(stainKeys.Add(key), Is.True, "Each hand must use distinct stain layer keys.");
                    Assert.That(layer, Is.Not.SameAs(templates[i]));
                    Assert.That(layer.Color, Is.EqualTo(expectedColors[location]));
                    Assert.That(layer.RsiPath, Is.EqualTo(templates[i].RsiPath));
                    Assert.That(layer.State, Is.EqualTo(templates[i].State));
                    // Querying visuals must not be what refreshes the actual holder sprite.
                    Assert.That(hands.RevealedLayers[location], Does.Contain(key));
                    Assert.That(sprites.LayerMapTryGet(clientHolder, key, out var index, false), Is.True);
                    Assert.That(sprites.TryGetLayer(clientHolder, index, out var rendered, false), Is.True);
                    Assert.That(rendered.Color, Is.EqualTo(expectedColors[location]));
                }

                Assert.That(templates.Select(layer => layer.Color), Is.EqualTo(templateColors[location]));
            }
        });

        await server.WaitAssertion(() =>
        {
            var solutions = server.System<SharedSolutionContainerSystem>();
            foreach (var uniform in uniforms.Values)
            {
                Assert.That(solutions.TryGetSolution(uniform, "stain", out var solution), Is.True);
                solutions.RemoveAllSolution(solution.Value);
            }
        });

        await pair.RunTicksSync(10);

        await client.WaitAssertion(() =>
        {
            var clientHolder = pair.ToClientUid(holder);
            var hands = client.EntMan.GetComponent<HandsComponent>(clientHolder);
            var sprites = client.System<SpriteSystem>();
            foreach (var location in locations)
            {
                var uniform = pair.ToClientUid(uniforms[location]);
                var visuals = new GetInhandVisualsEvent(clientHolder, location);
                client.EntMan.EventBus.RaiseLocalEvent(uniform, visuals);
                Assert.That(visuals.Layers.Any(layer => layer.Item1.StartsWith("stain-")), Is.False);
                Assert.That(hands.RevealedLayers[location].Any(key => key.StartsWith("stain-")), Is.False);
                var templates = client.EntMan.GetComponent<StainableComponent>(uniform)
                    .ItemVisuals[location.ToString().ToLowerInvariant()];
                Assert.That(templates.Select(layer => layer.Color), Is.EqualTo(templateColors[location]));
            }

            foreach (var key in stainKeys)
                Assert.That(sprites.LayerMapTryGet(clientHolder, key, out _, false), Is.False);
        });

        await server.WaitPost(() => server.PlayerMan.SetAttachedEntity(pair.Player!, previousActor));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CompletedWashRemovesStainAndPreservesDnaFreshness()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var solutions = server.System<SharedSolutionContainerSystem>();
            var storage = server.System<SharedEntityStorageSystem>();
            // Without a power receiver, this isolated machine is always powered.
            var machine = entMan.SpawnEntity("StainsTestWasher", map.GridCoords);
            var uniform = entMan.SpawnEntity("StainsTestUniform", map.GridCoords);
            var user = entMan.SpawnEntity(null, map.GridCoords);
            var washer = entMan.GetComponent<WashingMachineComponent>(machine);
            var evidence = entMan.GetComponent<ForensicsComponent>(machine);
            Assert.That(solutions.TryGetSolution(uniform, "stain", out var solution, out var stain), Is.True);
            Assert.That(solutions.TryAddSolution(solution.Value, Blood("wash-donor", FixedPoint2.New(1))), Is.True);

            // Seed the stored evidence directly to isolate washing from reagent cloning.
            var freshness = TimeSpan.FromSeconds(123);
            stain.Contents.Single().Reagent.Data!.OfType<DnaData>().Single().Freshness = freshness;
            Assert.That(server.System<ForensicsSystem>().GetSolutionsDNA(stain),
                Is.EquivalentTo(new[] { ("wash-donor", freshness) }));
            evidence.DNAs.Add(("previous-donor", TimeSpan.FromSeconds(1)));
            Assert.That(storage.Insert(uniform, machine), Is.True);

            var activate = new ActivateInWorldEvent(user, machine, complex: true);
            entMan.EventBus.RaiseLocalEvent(machine, activate);
            Assert.Multiple(() =>
            {
                Assert.That(activate.Handled, Is.True);
                Assert.That(washer.State, Is.EqualTo(WashingMachineState.Washing));
                Assert.That(washer.WashFinishTime, Is.Not.Null);
                Assert.That(storage.IsOpen(machine), Is.False);
                Assert.That(stain.Volume, Is.EqualTo(FixedPoint2.New(1)));
                Assert.That(evidence.DNAs, Does.Not.Contain(("wash-donor", freshness)));
            });

            // Advance the deadline, not wall time, and run the real completion path.
            washer.WashFinishTime = server.ResolveDependency<IGameTiming>().CurTime;
            server.System<SharedWashingMachineSystem>().Update(0);

            Assert.Multiple(() =>
            {
                Assert.That(stain.Volume, Is.EqualTo(FixedPoint2.Zero));
                Assert.That(stain.Contents, Is.Empty);
                Assert.That(evidence.DNAs, Is.EquivalentTo(new[]
                {
                    ("previous-donor", TimeSpan.FromSeconds(1)),
                    ("wash-donor", freshness),
                }));
                Assert.That(washer.State, Is.EqualTo(WashingMachineState.Idle));
                Assert.That(washer.WashFinishTime, Is.Null);
                Assert.That(washer.AudioStream, Is.Null);
                Assert.That(storage.IsOpen(machine), Is.True);
                Assert.That(entMan.GetComponent<EntityStorageComponent>(machine).Contents.Contains(uniform), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PausingWasherPreservesRemainingWashAndCooldown(bool duringCooldown)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        EntityUid machine = default;
        EntityUid uniform = default;
        TimeSpan pausedAt = default;
        TimeSpan? finishBefore = null;
        TimeSpan nextAllowedBefore = default;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var solutions = server.System<SharedSolutionContainerSystem>();
            var storage = server.System<SharedEntityStorageSystem>();
            machine = entMan.SpawnEntity("StainsTestWasher", map.GridCoords);
            uniform = entMan.SpawnEntity("StainsTestUniform", map.GridCoords);
            var user = entMan.SpawnEntity(null, map.GridCoords);
            var washer = entMan.GetComponent<WashingMachineComponent>(machine);
            washer.WashTime = TimeSpan.FromSeconds(0.25);
            washer.Cooldown = TimeSpan.FromSeconds(0.25);
            Assert.That(solutions.TryGetSolution(uniform, "stain", out var solution), Is.True);
            Assert.That(solutions.TryAddSolution(solution.Value, Blood("paused-donor", FixedPoint2.New(1))), Is.True);
            Assert.That(storage.Insert(uniform, machine), Is.True);
            entMan.EventBus.RaiseLocalEvent(machine, new ActivateInWorldEvent(user, machine, complex: true));
            Assert.That(washer.State, Is.EqualTo(WashingMachineState.Washing));

            if (duringCooldown)
            {
                washer.WashFinishTime = server.ResolveDependency<IGameTiming>().CurTime;
                server.System<SharedWashingMachineSystem>().Update(0);
                Assert.That(washer.State, Is.EqualTo(WashingMachineState.Idle));
            }

            pausedAt = server.ResolveDependency<IGameTiming>().CurTime;
            finishBefore = washer.WashFinishTime;
            nextAllowedBefore = washer.NextWashAllowed;
            server.System<MetaDataSystem>().SetEntityPaused(machine, true);
        });

        await server.WaitRunTicks(120);

        await server.WaitAssertion(() =>
        {
            var washer = server.EntMan.GetComponent<WashingMachineComponent>(machine);
            var metadata = server.System<MetaDataSystem>();
            var now = server.ResolveDependency<IGameTiming>().CurTime;
            var pausedFor = metadata.GetPauseTime(machine);
            Assert.That(pausedFor, Is.EqualTo(now - pausedAt));
            Assert.That(pausedFor, Is.GreaterThan(TimeSpan.FromSeconds(0.25)));
            Assert.That(metadata.EntityPaused(machine), Is.True);
            Assert.That(washer.State, Is.EqualTo(duringCooldown ? WashingMachineState.Idle : WashingMachineState.Washing));
            Assert.That(washer.WashFinishTime, Is.EqualTo(finishBefore));
            Assert.That(washer.NextWashAllowed, Is.EqualTo(nextAllowedBefore));

            metadata.SetEntityPaused(machine, false);

            Assert.That(metadata.EntityPaused(machine), Is.False);
            Assert.That(washer.WashFinishTime, Is.EqualTo(finishBefore + pausedFor));
            Assert.That(washer.NextWashAllowed, Is.EqualTo(nextAllowedBefore + pausedFor));
            if (duringCooldown)
                Assert.That(washer.NextWashAllowed - now, Is.EqualTo(nextAllowedBefore - pausedAt));
            else
                Assert.That(washer.WashFinishTime - now, Is.EqualTo(finishBefore - pausedAt));

            server.System<SharedWashingMachineSystem>().Update(0);
            Assert.That(washer.State, Is.EqualTo(duringCooldown ? WashingMachineState.Idle : WashingMachineState.Washing));
            Assert.That(server.System<SharedSolutionContainerSystem>()
                .TryGetSolution(uniform, "stain", out _, out var stain), Is.True);
            Assert.That(stain.Volume, Is.EqualTo(duringCooldown ? FixedPoint2.Zero : FixedPoint2.New(1)));
        });

        await server.WaitRunTicks(120);

        await server.WaitAssertion(() =>
        {
            var washer = server.EntMan.GetComponent<WashingMachineComponent>(machine);
            Assert.That(washer.State, Is.EqualTo(WashingMachineState.Idle));
            Assert.That(washer.WashFinishTime, Is.Null);
            Assert.That(server.System<SharedSolutionContainerSystem>()
                .TryGetSolution(uniform, "stain", out _, out var stain), Is.True);
            Assert.That(stain.Volume, Is.EqualTo(FixedPoint2.Zero));
        });

        await pair.CleanReturnAsync();
    }

    [TestCase("power")]
    [TestCase("break")]
    [TestCase("shutdown")]
    public async Task InterruptedWashReleasesStorageWithoutCleaning(string interruption)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var solutions = server.System<SharedSolutionContainerSystem>();
            var storage = server.System<SharedEntityStorageSystem>();
            var machine = entMan.SpawnEntity("StainsTestWasher", map.GridCoords);
            var uniform = entMan.SpawnEntity("StainsTestUniform", map.GridCoords);
            var user = entMan.SpawnEntity(null, map.GridCoords);
            var washer = entMan.GetComponent<WashingMachineComponent>(machine);
            Assert.That(solutions.TryGetSolution(uniform, "stain", out var solution, out var stain), Is.True);
            Assert.That(solutions.TryAddSolution(solution.Value, Blood("unwashed-donor", FixedPoint2.New(1))), Is.True);
            Assert.That(storage.Insert(uniform, machine), Is.True);
            entMan.EventBus.RaiseLocalEvent(machine, new ActivateInWorldEvent(user, machine, complex: true));
            Assert.That(washer.State, Is.EqualTo(WashingMachineState.Washing));
            washer.AccumulatedSelfDamage = 5;

            switch (interruption)
            {
                case "power":
                    var power = new PowerChangedEvent(false, 0);
                    entMan.EventBus.RaiseLocalEvent(machine, ref power);
                    break;
                case "break":
                    entMan.EventBus.RaiseLocalEvent(machine, new BreakageEventArgs());
                    break;
                case "shutdown":
                    entMan.RemoveComponent<WashingMachineComponent>(machine);
                    break;
            }

            Assert.Multiple(() =>
            {
                Assert.That(washer.State, Is.EqualTo(interruption == "break"
                    ? WashingMachineState.Broken : WashingMachineState.Idle));
                Assert.That(washer.WashFinishTime, Is.Null);
                Assert.That(washer.AudioStream, Is.Null);
                Assert.That(washer.AccumulatedSelfDamage, Is.Zero);
                Assert.That(storage.IsOpen(machine), Is.True);
                Assert.That(entMan.GetComponent<EntityStorageComponent>(machine).Contents.Contains(uniform), Is.False);
                Assert.That(stain.GetTotalPrototypeQuantity("Blood"), Is.EqualTo(FixedPoint2.New(1)));
                Assert.That(server.System<ForensicsSystem>().GetSolutionsDNA(stain),
                    Is.EquivalentTo(new[] { ("unwashed-donor", TimeSpan.Zero) }));
                Assert.That(entMan.GetComponent<ForensicsComponent>(machine).DNAs, Is.Empty);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DeathsquadAndCentcomClothingCanBeStained()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var solutions = server.System<SharedSolutionContainerSystem>();
            var source = server.EntMan.SpawnEntity(null, map.GridCoords);
            string[] prototypes =
            [
                "ClothingUniformJumpsuitDeathSquad",
                "ClothingOuterHardsuitDeathsquad",
                "ClothingHeadHelmetHardsuitDeathsquad",
                "ClothingHandsGlovesDeathsquad",
                "ClothingHandsGlovesDeathsquadUnremovable",
                "ClothingShoesBootsMagDeathsquad",
                "ClothingMaskGasDeathSquad",
                "ClothingUniformJumpsuitCentcomAgent",
                "ClothingUniformJumpsuitCentcomOfficial",
                "ClothingUniformJumpsuitCentcomOfficer",
                "ClothingUniformJumpsuitCentcomFormal",
                "ClothingUniformJumpskirtCentcomFormalDress",
                "ClothingHeadHatCentcom",
                "ClothingHeadHatCentcomcap",
                "ClothingHeadHatHoodWinterCentcom",
                "ClothingHandsGlovesHeavyGoldenTrim",
                "ClothingHandsGlovesGoldenTrim",
                "ClothingOuterArmorCentcommCarapace",
                "ClothingOuterWinterCentcom",
                "ClothingOuterWinterCCWarden",
                "ClothingOuterArmoredJacketCentralCommand",
                "ClothingMaskGasCentcom",
            ];

            foreach (var prototype in prototypes)
            {
                var clothing = server.EntMan.SpawnEntity(prototype, map.GridCoords);
                Assert.That(server.EntMan.TryGetComponent<StainableComponent>(clothing, out var stainable), Is.True, prototype);
                Assert.That(solutions.TryGetSolution(clothing, stainable.SolutionName, out _, out var stain), Is.True,
                    $"{prototype} must retain its stain solution through inheritance.");

                stainable.StainChance = 1;
                server.EntMan.EventBus.RaiseLocalEvent(clothing,
                    new SpilledOnEvent(source, Blood("command-donor", FixedPoint2.New(1))));

                Assert.That(stain.GetTotalPrototypeQuantity("Blood"), Is.EqualTo(FixedPoint2.New(0.1f)), prototype);
                Assert.That(stainable.ClothingVisuals, Is.Not.Empty, prototype);
                Assert.That(stainable.IconVisuals, Is.Not.Empty, prototype);
            }
        });

        await pair.CleanReturnAsync();
    }

    private static Solution Blood(string dna, FixedPoint2 volume, TimeSpan freshness = default)
    {
        return new Solution("Blood", volume, new List<ReagentData> { new DnaData { DNA = dna, Freshness = freshness } });
    }
}
