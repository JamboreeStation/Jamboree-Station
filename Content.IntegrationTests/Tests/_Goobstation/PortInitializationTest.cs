// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.DropPod;
using Content.Goobstation.Shared.ModSuits.Components;
using Content.Goobstation.Shared.ModSuits.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Alert;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Goobstation;

[TestFixture]
public sealed class PortInitializationTest
{
    [TestCase("ClothingModsuitStandardPowerCell")]
    [TestCase("ClothingModsuitFirepower")]
    public async Task ModSuitInitializesPartsActionsAndPower(string prototype)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var suits = entMan.System<ModSuitSystem>();
        var slots = entMan.System<ItemSlotsSystem>();
        var inventory = entMan.System<InventorySystem>();
        var actions = entMan.System<SharedActionsSystem>();
        var alerts = entMan.System<AlertsSystem>();
        Entity<ModSuitComponent> suitEntity = default;
        EntityUid wearer = default;

        await server.WaitAssertion(() =>
        {
            var uid = entMan.SpawnEntity(prototype, map.GridCoords);
            var suit = entMan.GetComponent<ModSuitComponent>(uid);
            suitEntity = (uid, suit);

            Assert.That(suit.PartsContainer, Is.Not.Null);
            Assert.That(suit.ModulesContainer, Is.Not.Null);
            Assert.That(suit.Parts, Has.Count.EqualTo(4));
            Assert.That(suit.PartEntities.Keys, Is.EquivalentTo(suit.Parts.Keys));
            Assert.That(suit.PartsContainer.ContainedEntities, Is.EquivalentTo(suit.PartEntities.Values));

            foreach (var (slot, part) in suit.PartEntities)
            {
                Assert.That(entMan.GetComponent<MetaDataComponent>(part).EntityPrototype.ID,
                    Is.EqualTo(suit.Parts[slot].Id));
                var partComp = entMan.GetComponent<ModPartComponent>(part);
                Assert.That(partComp.Suit, Is.EqualTo(uid));
                Assert.That(partComp.Slot, Is.EqualTo(slot));
                Assert.That(partComp.StoredContainer, Is.Not.Null);
            }

            foreach (var (action, actionPrototype) in new[]
                     {
                         (suit.ActivateActionEntity, suit.ActivateAction),
                         (suit.DeployActionEntity, suit.DeployAction),
                     })
            {
                Assert.That(entMan.HasComponent<ActionComponent>(action), Is.True, actionPrototype.Id);
                Assert.That(entMan.GetComponent<MetaDataComponent>(action.Value).EntityPrototype.ID,
                    Is.EqualTo(actionPrototype.Id));
            }

            var core = suits.GetCore((uid, suit));
            Assert.That(core, Is.Not.Null);
            Assert.That(entMan.HasComponent<ModCoreComponent>(core), Is.True);
            Assert.That(entMan.GetComponent<MetaDataComponent>(core.Value).EntityPrototype.ID,
                Is.EqualTo("ModsuitCoreStandardHighCapacity"));

            var cellSlot = entMan.GetComponent<PowerCellSlotComponent>(core.Value);
            var cell = slots.GetItemOrNull(core.Value, cellSlot.CellSlotId);
            Assert.That(cell, Is.Not.Null);
            Assert.That(entMan.HasComponent<PowerCellComponent>(cell), Is.True);
            Assert.That(entMan.GetComponent<MetaDataComponent>(cell.Value).EntityPrototype.ID,
                Is.EqualTo("PowerCellHigh"));
            var battery = suits.GetBattery((uid, suit));
            Assert.That(battery, Is.Not.Null);
            Assert.That(battery.Value.Owner, Is.EqualTo(cell.Value));
            Assert.That(suits.HasCharge((uid, suit), 1f), Is.True);

            wearer = entMan.SpawnEntity("MobHuman", map.GridCoords);
            Assert.That(inventory.TryEquip(wearer, uid, "back"), Is.True);
            Assert.That(suit.Wearer, Is.EqualTo(wearer));
            var granted = entMan.GetComponent<ActionsComponent>(wearer).Actions;
            foreach (var action in new[] { suit.ActivateActionEntity, suit.DeployActionEntity })
            {
                Assert.That(granted, Does.Contain(action.Value));
                Assert.That(entMan.GetComponent<ActionComponent>(action.Value).AttachedEntity, Is.EqualTo(wearer));
            }

            actions.PerformAction(wearer, actions.GetActions(wearer).Single(action => action.Owner == suit.DeployActionEntity));
            Assert.That(suit.DeployQueue, Has.Count.EqualTo(4));
        });

        await PoolManager.WaitUntil(server, () => suitEntity.Comp.DeployQueue.Count == 0, tickStep: 10);
        await server.WaitAssertion(() =>
        {
            Assert.That(suits.AllPartsDeployed(suitEntity), Is.True);
            actions.PerformAction(wearer, actions.GetActions(wearer).Single(action => action.Owner == suitEntity.Comp.ActivateActionEntity));
            Assert.That(suitEntity.Comp.State, Is.EqualTo(ModSuitState.Activating));
        });

        await PoolManager.WaitUntil(server, () => suitEntity.Comp.State == ModSuitState.Active, tickStep: 10);
        await server.WaitAssertion(() =>
        {
            Assert.That(suitEntity.Comp.Powered, Is.True);
            Assert.That(entMan.GetComponent<ActionComponent>(suitEntity.Comp.ActivateActionEntity.Value).Toggled, Is.True);
            Assert.That(alerts.IsShowingAlert(wearer, suitEntity.Comp.PowerAlert), Is.True);
            var powerAlert = alerts.GetActiveAlerts(wearer).Values.Single(alert => alert.Type == suitEntity.Comp.PowerAlert);
            Assert.That(powerAlert.Severity, Is.GreaterThan(0));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DropPodMapInitSetsLandingDeadline()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var timing = server.ResolveDependency<IGameTiming>();

        await server.WaitAssertion(() =>
        {
            // Check in the spawn callback, before Update can land a pod with an unset deadline.
            var now = timing.CurTime;
            var uid = entMan.SpawnEntity("DropPod", map.GridCoords);
            var pod = entMan.GetComponent<DropPodComponent>(uid);
            Assert.That(pod.LandDelay, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(pod.LandTime, Is.EqualTo(now + pod.LandDelay));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DropPodLauncherUseTransfersContentsAndDeletesLauncher()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var containers = entMan.System<SharedContainerSystem>();
        EntityUid launcher = default;
        EntityUid item = default;
        EntityUid pod = default;

        await server.WaitAssertion(() =>
        {
            var user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            launcher = entMan.SpawnEntity("DropPodLauncher", map.GridCoords);
            item = entMan.SpawnEntity("Screwdriver", map.GridCoords);
            Assert.That(containers.Insert(item, containers.GetContainer(launcher, "storagebase")), Is.True);

            var use = new UseInHandEvent(user);
            entMan.EventBus.RaiseLocalEvent(launcher, use);
            Assert.That(use.Handled, Is.True);
            pod = entMan.GetComponent<TransformComponent>(item).ParentUid;
            Assert.That(entMan.HasComponent<DropPodComponent>(pod), Is.True);
            Assert.That(containers.GetContainer(pod, DropPodComponent.ContainerId).ContainedEntities,
                Is.EquivalentTo(new[] { item }));
        });

        await PoolManager.WaitUntil(server, () => !entMan.EntityExists(launcher), maxTicks: 10);
        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.EntityExists(launcher), Is.False);
            Assert.That(entMan.EntityExists(pod), Is.True);
            Assert.That(entMan.EntityExists(item), Is.True);
            Assert.That(containers.GetContainer(pod, DropPodComponent.ContainerId).Contains(item), Is.True);
        });

        await pair.CleanReturnAsync();
    }
}
