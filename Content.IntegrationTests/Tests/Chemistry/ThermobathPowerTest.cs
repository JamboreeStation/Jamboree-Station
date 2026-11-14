// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Numerics;
using Content.Client.Chemistry.UI.Thermobath;
using Content.Goobstation.Maths.FixedPoint;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power.Components;
using Content.Shared.Temperature.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.IntegrationTests.Tests.Chemistry;

public sealed class ThermobathPowerTest : InteractionTest
{
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task ClientPowerSwitchHeatsOnFloorAndTable(bool onTable, bool clickButton)
    {
        EntityUid apc = default;
        EntityCoordinates destination = default;
        await Server.WaitPost(() =>
        {
            for (var x = 0; x < 2; x++)
            for (var y = 0; y < 2; y++)
            {
                var coords = new EntityCoordinates(MapData.Grid, new Vector2(x + 0.5f, y + 0.5f));
                MapSystem.SetTile(MapData.Grid, MapData.Grid.Comp, coords, new Tile(TileMan[Plating].TileId));
                SEntMan.SpawnEntity("CableApcExtension", coords);
            }

            apc = SEntMan.SpawnEntity("APCBasic", new EntityCoordinates(MapData.Grid, new Vector2(0.5f, 1.5f)));
            destination = new EntityCoordinates(MapData.Grid, new Vector2(1.5f, 1.5f));
            if (onTable)
            {
                var table = SEntMan.SpawnEntity("Table", destination);
                Assert.That(SEntMan.GetComponent<TransformComponent>(table).Anchored, Is.True);
            }
        });

        await SpawnTarget("MachineThermobath");
        AssertAnchored();
        await InteractUsing("Wrench");
        AssertAnchored(false);

        // Move an unanchored machine onto the destination, then use the normal wrench path.
        await Server.WaitAssertion(() =>
        {
            Transform.SetCoordinates(STarget!.Value, destination);
            TargetCoords = SEntMan.GetNetCoordinates(destination);
            var body = Comp<PhysicsComponent>();
            Assert.That(SEntMan.System<AnchorableSystem>().TileFree(destination, body), Is.True,
                "A thermobath must be anchorable on both bare plating and an occupied table tile.");
        });
        await RunTicks(5);
        await Interact();
        AssertAnchored();
        AssertLocation(Target, TargetCoords);
        await DeleteHeldEntity();

        var beaker = await PlaceInHands("Beaker");
        var solutions = SEntMan.System<SharedSolutionContainerSystem>();
        Solution solution = default!;
        var capacity = 0f;
        var initialTemperature = 0f;
        await Server.WaitAssertion(() =>
        {
            Assert.That(solutions.TryGetSolution(ToServer(beaker), "beaker", out var solutionEnt, out solution), Is.True);
            Assert.That(solutions.TryAddReagent(solutionEnt.Value, "Water", FixedPoint2.New(10)), Is.True);
            initialTemperature = Comp<ThermoregulatorComponent>().Temperature;
            solutions.SetTemperature(solutionEnt.Value, initialTemperature);
            capacity = solution.GetHeatCapacity(ProtoMan);
        });
        await Interact();
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<ItemSlotsSystem>().GetItemOrNull(STarget!.Value, ThermobathComponent.BeakerSlotId),
                Is.EqualTo(ToServer(beaker)));
        });

        await Interact();
        await RunTicks(15);
        Assert.That(IsUiOpen(ThermobathUiKey.Key), Is.True);
        await SendBui(ThermobathUiKey.Key, new ThermobathSetpointChangedMessage(500f));
        await SendBui(ThermobathUiKey.Key, new ThermobathModeChangedMessage(ThermobathMode.Heating));
        await Server.WaitAssertion(() =>
        {
            Assert.That(Comp<ThermobathComponent>().Setpoint, Is.EqualTo(500f));
            Assert.That(Comp<ThermobathComponent>().Mode, Is.EqualTo(ThermobathMode.Heating));
            Assert.That(Comp<ApcPowerReceiverComponent>().PowerDisabled, Is.True);
            Assert.That(Comp<ThermoregulatorComponent>().Temperature, Is.EqualTo(initialTemperature));
        });

        // Compare the actual predicted button path with an explicit client BUI request.
        var powerRequests = new List<bool>();
        await Client.WaitAssertion(() =>
        {
            var window = GetWindow<ThermobathMenu>();
            var receiver = CEntMan.GetComponent<Content.Client.Power.Components.ApcPowerReceiverComponent>(CTarget!.Value);
            var button = GetControlFromField<Button>("PowerButton", window);
            Assert.Multiple(() =>
            {
                Assert.That(receiver.NeedsPower, Is.True);
                Assert.That(receiver.PowerDisabled, Is.True);
                Assert.That(button.Pressed, Is.False, "The client must start with the same disabled switch as the server.");
            });
            window.OnPowerChanged += enabled => powerRequests.Add(enabled);
        });
        await SetPower(true);
        await RunSeconds(3f);
        if (clickButton)
        {
            await Client.WaitAssertion(() => Assert.That(powerRequests, Is.EqualTo(new[] { true }),
                "The real button must emit exactly one enable request."));
        }
        await Server.WaitAssertion(() =>
        {
            var receiver = Comp<ApcPowerReceiverComponent>();
            Assert.Multiple(() =>
            {
                Assert.That(receiver.NeedsPower, Is.True);
                Assert.That(receiver.PowerDisabled, Is.False, "The actual client power button must enable the server receiver.");
                Assert.That(receiver.Provider, Is.Not.Null);
                Assert.That(receiver.Powered, Is.True);
                Assert.That(receiver.PowerReceived, Is.EqualTo(1200f).Within(0.1f));
                Assert.That(SEntMan.GetComponent<PowerNetworkBatteryComponent>(apc).CurrentSupply,
                    Is.EqualTo(1200f).Within(0.1f));
                Assert.That(Comp<ThermoregulatorComponent>().ActiveMode, Is.EqualTo(ThermoregulatorActiveMode.Heating));
                Assert.That(Comp<ThermoregulatorComponent>().Temperature, Is.GreaterThan(initialTemperature));
                Assert.That(solution.Temperature, Is.GreaterThan(initialTemperature));
            });
        });

        await SetPower(false);
        await RunTicks(15);
        double offEnergy = 0;
        await Server.WaitAssertion(() =>
        {
            var regulator = Comp<ThermoregulatorComponent>();
            Assert.That(Comp<ApcPowerReceiverComponent>().PowerDisabled, Is.True);
            Assert.That(Comp<ApcPowerReceiverComponent>().PowerReceived, Is.Zero);
            Assert.That(regulator.ActiveMode, Is.EqualTo(ThermoregulatorActiveMode.Idle));
            Assert.That(Comp<ThermobathComponent>().MinEnergy, Is.Zero);
            Assert.That(Comp<ThermobathComponent>().MaxEnergy, Is.Zero);
            offEnergy = (double) regulator.Temperature * regulator.HeatCapacity + (double) solution.Temperature * capacity;
        });
        await RunSeconds(3f);
        await Server.WaitAssertion(() =>
        {
            var regulator = Comp<ThermoregulatorComponent>();
            Assert.That((double) regulator.Temperature * regulator.HeatCapacity + (double) solution.Temperature * capacity,
                Is.EqualTo(offEnergy).Within(0.1d), "Switching off must stop active energy, but not passive bath/beaker exchange.");
        });

        await SetPower(true);
        await RunSeconds(2f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Comp<ApcPowerReceiverComponent>().Powered, Is.True);
            Assert.That(Comp<ThermoregulatorComponent>().ActiveMode, Is.EqualTo(ThermoregulatorActiveMode.Heating));
            var battery = SEntMan.GetComponent<BatteryComponent>(apc);
            SEntMan.System<BatterySystem>().SetCharge((apc, battery), 0);
        });
        await RunTicks(15);
        double outageEnergy = 0;
        await Server.WaitAssertion(() =>
        {
            var regulator = Comp<ThermoregulatorComponent>();
            var receiver = Comp<ApcPowerReceiverComponent>();
            Assert.That(receiver.PowerDisabled, Is.False, "An outage must not change the user's enabled switch.");
            Assert.That(receiver.Powered, Is.False);
            Assert.That(receiver.PowerReceived, Is.Zero);
            Assert.That(regulator.ActiveMode, Is.EqualTo(ThermoregulatorActiveMode.Idle));
            Assert.That(Comp<ThermobathComponent>().MinEnergy, Is.Zero);
            Assert.That(Comp<ThermobathComponent>().MaxEnergy, Is.Zero);
            outageEnergy = (double) regulator.Temperature * regulator.HeatCapacity + (double) solution.Temperature * capacity;
        });
        await RunSeconds(3f);
        await Server.WaitAssertion(() =>
        {
            var regulator = Comp<ThermoregulatorComponent>();
            Assert.That((double) regulator.Temperature * regulator.HeatCapacity + (double) solution.Temperature * capacity,
                Is.EqualTo(outageEnergy).Within(0.1d), "An empty APC must stop active energy without faking receiver state.");
        });

        if (clickButton)
            await Client.WaitAssertion(() => Assert.That(powerRequests, Is.EqualTo(new[] { true, false, true })));

        async Task SetPower(bool enabled)
        {
            if (clickButton)
            {
                await ClickControl<ThermobathMenu>("PowerButton");
                return;
            }

            Assert.That(TryGetBui(ThermobathUiKey.Key, out var bui), Is.True);
            await Client.WaitPost(() => bui!.SendMessage(new ThermobathPowerChangedMessage(enabled)));
            await RunTicks(5);
        }
    }
}
