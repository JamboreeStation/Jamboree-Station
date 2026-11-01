// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Temperature.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chemistry;

[TestFixture]
public sealed class ThermobathTest
{
    [Test]
    public async Task PrototypeDefaultsAndControlValidation()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var uid = entMan.SpawnEntity("MachineThermobath", map.GridCoords);
            var bath = entMan.GetComponent<ThermobathComponent>(uid);
            var regulator = entMan.GetComponent<ThermoregulatorComponent>(uid);
            var receiver = entMan.GetComponent<ApcPowerReceiverComponent>(uid);
            var system = server.System<ServerThermobathSystem>();

            Assert.Multiple(() =>
            {
                Assert.That(bath.MinTemperature, Is.EqualTo(253.15f));
                Assert.That(bath.MaxTemperature, Is.EqualTo(573.15f));
                Assert.That(bath.HeatingPower, Is.EqualTo(1200f));
                Assert.That(bath.CoolingPower, Is.EqualTo(400f));
                Assert.That(regulator.HeatCapacity, Is.EqualTo(100f));
                Assert.That(receiver.PowerDisabled, Is.True);
                Assert.That(server.System<ItemSlotsSystem>().TryGetSlot(uid, "beakerSlot", out _), Is.True);
            });

            system.SetSetpoint(uid, 300f);
            foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                entMan.EventBus.RaiseLocalEvent(uid, new ThermobathSetpointChangedMessage(invalid));
                Assert.That(bath.Setpoint, Is.EqualTo(300f));
            }

            system.SetSetpoint(uid, float.MinValue);
            Assert.That(bath.Setpoint, Is.EqualTo(253.15f));
            entMan.EventBus.RaiseLocalEvent(uid, new ThermobathSetpointChangedMessage(float.MaxValue));
            Assert.That(bath.Setpoint, Is.EqualTo(573.15f));

            system.SetMode(uid, ThermobathMode.Cooling);
            system.SetMode(uid, (ThermobathMode) byte.MaxValue);
            entMan.EventBus.RaiseLocalEvent(uid, new ThermobathModeChangedMessage((ThermobathMode) byte.MaxValue));
            Assert.That(bath.Mode, Is.EqualTo(ThermobathMode.Cooling));
        });

        await pair.CleanReturnAsync();
    }

    [TestCase(ThermobathMode.Heating, 0f, 300f)]
    [TestCase(ThermobathMode.Cooling, -100f, 0f)]
    [TestCase(ThermobathMode.Auto, -100f, 300f)]
    public async Task ModesRespectEnergyBudgetsAndPowerOff(ThermobathMode mode, float minEnergy, float maxEnergy)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var uid = entMan.SpawnEntity("MachineThermobath", map.GridCoords);
            var regulator = entMan.GetComponent<ThermoregulatorComponent>(uid);
            var receiver = entMan.GetComponent<ApcPowerReceiverComponent>(uid);
            var bathSystem = server.System<ServerThermobathSystem>();
            var regulatorSystem = server.System<ThermoregulatorSystem>();
            regulator.UpdateInterval = TimeSpan.FromSeconds(0.25);

            // Keep all updates in this assertion so the power network cannot overwrite the supplied state.
            receiver.Powered = true;
            entMan.EventBus.RaiseLocalEvent(uid, new ThermobathPowerChangedMessage(true) { Actor = uid });
            bathSystem.SetMode(uid, mode);

            foreach (var target in new[] { 500f, 280f })
            {
                regulator.Temperature = 300f;
                bathSystem.SetSetpoint(uid, target);
                var control = new ThermoregulatorControlEvent(regulator.Temperature);
                entMan.EventBus.RaiseLocalEvent(uid, ref control);
                var activeMode = target > 300f && maxEnergy > 0f
                    ? ThermoregulatorActiveMode.Heating
                    : target < 300f && minEnergy < 0f
                        ? ThermoregulatorActiveMode.Cooling
                        : ThermoregulatorActiveMode.Idle;

                Assert.Multiple(() =>
                {
                    Assert.That(control.TargetTemperature, Is.EqualTo(target));
                    Assert.That(control.MinEnergy, Is.EqualTo(minEnergy));
                    Assert.That(control.MaxEnergy, Is.EqualTo(maxEnergy));
                    Assert.That(regulator.ActiveMode, Is.EqualTo(activeMode));
                });

                regulator.NextUpdate = TimeSpan.Zero;
                regulatorSystem.Update(0f);
                var energy = target > 300f ? maxEnergy : minEnergy;
                Assert.That(regulator.Temperature, Is.EqualTo(300f + energy / 100f).Within(0.0001f));

                if (activeMode == ThermoregulatorActiveMode.Idle)
                    continue;

                // A reachable target must be assigned exactly, not overshot by a full energy budget.
                var closeTarget = regulator.Temperature + (target > 300f ? 0.25f : -0.25f);
                bathSystem.SetSetpoint(uid, closeTarget);
                regulator.NextUpdate = TimeSpan.Zero;
                regulatorSystem.Update(0f);
                Assert.That(regulator.Temperature, Is.EqualTo(closeTarget));
                Assert.That(regulator.ActiveMode, Is.EqualTo(ThermoregulatorActiveMode.Idle));
            }

            bathSystem.SetSetpoint(uid, mode == ThermobathMode.Cooling ? 280f : 500f);
            entMan.EventBus.RaiseLocalEvent(uid, new ThermobathPowerChangedMessage(false) { Actor = uid });
            var offControl = new ThermoregulatorControlEvent(regulator.Temperature);
            entMan.EventBus.RaiseLocalEvent(uid, ref offControl);
            var before = regulator.Temperature;
            regulator.NextUpdate = TimeSpan.Zero;
            regulatorSystem.Update(0f);

            Assert.Multiple(() =>
            {
                Assert.That(receiver.PowerDisabled, Is.True);
                Assert.That(offControl.MinEnergy, Is.Zero);
                Assert.That(offControl.MaxEnergy, Is.Zero);
                Assert.That(regulator.Temperature, Is.EqualTo(before));
                Assert.That(regulator.ActiveMode, Is.EqualTo(ThermoregulatorActiveMode.Idle));
            });

            receiver.Powered = false;
            entMan.EventBus.RaiseLocalEvent(uid, new ThermobathPowerChangedMessage(true) { Actor = uid });
            regulator.NextUpdate = TimeSpan.Zero;
            regulatorSystem.Update(0f);
            Assert.That(receiver.PowerDisabled, Is.False);
            Assert.That(regulator.Temperature, Is.EqualTo(before));
            Assert.That(regulator.ActiveMode, Is.EqualTo(ThermoregulatorActiveMode.Idle));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BeakerExchangeConservesEnergyWithoutCrossing()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var prototypes = server.ResolveDependency<IPrototypeManager>();
            var solutions = server.System<SharedSolutionContainerSystem>();
            var slots = server.System<ItemSlotsSystem>();
            var uid = entMan.SpawnEntity("MachineThermobath", map.GridCoords);
            var regulator = entMan.GetComponent<ThermoregulatorComponent>(uid);
            regulator.UpdateInterval = TimeSpan.FromSeconds(1);
            var beaker = entMan.SpawnEntity("Beaker", map.GridCoords);
            Assert.That(solutions.TryGetSolution(beaker, "beaker", out var solutionEnt, out var solution), Is.True);
            Assert.That(slots.TryInsert(uid, ThermobathComponent.BeakerSlotId, beaker, null), Is.True);

            // An empty beaker is harmless, even with the machine switched off.
            regulator.Temperature = 300f;
            var updated = new ThermoregulatorUpdatedEvent(regulator);
            entMan.EventBus.RaiseLocalEvent(uid, ref updated);
            Assert.That(regulator.Temperature, Is.EqualTo(300f));

            foreach (var volume in new[] { FixedPoint2.Epsilon, FixedPoint2.New(10) })
            {
                solutions.RemoveAllSolution(solutionEnt.Value);
                Assert.That(solutions.TryAddReagent(solutionEnt.Value, "Water", volume), Is.True);

                foreach (var temperature in new[] { 280f, 350f })
                foreach (var conductance in new[] { 2f, 10000f })
                {
                    regulator.Temperature = 300f;
                    regulator.ThermalConductance = conductance;
                    solutions.SetTemperature(solutionEnt.Value, temperature);
                    var capacity = solution.GetHeatCapacity(prototypes);
                    var initialEnergy = 300d * regulator.HeatCapacity + (double) temperature * capacity;

                    entMan.EventBus.RaiseLocalEvent(uid, ref updated);

                    Assert.Multiple(() =>
                    {
                        Assert.That(solution.Volume, Is.EqualTo(volume));
                        Assert.That(regulator.Temperature, Is.InRange(Math.Min(300f, temperature), Math.Max(300f, temperature)));
                        Assert.That(solution.Temperature, Is.InRange(Math.Min(300f, temperature), Math.Max(300f, temperature)));
                        Assert.That((regulator.Temperature - solution.Temperature) * (300f - temperature), Is.GreaterThanOrEqualTo(0f));
                        Assert.That(solution.Temperature, Is.Not.EqualTo(temperature));
                        Assert.That((double) regulator.Temperature * regulator.HeatCapacity + (double) solution.Temperature * capacity,
                            Is.EqualTo(initialEnergy).Within(0.01d));
                    });

                    if (conductance == 10000f)
                        Assert.That(regulator.Temperature, Is.EqualTo(solution.Temperature));
                }
            }

            Assert.That(slots.TryEject(uid, ThermobathComponent.BeakerSlotId, null, out var ejected, doAfter: false), Is.True);
            Assert.That(ejected, Is.EqualTo(beaker));
            regulator.Temperature = 300f;
            solutions.SetTemperature(solutionEnt.Value, 350f);
            entMan.EventBus.RaiseLocalEvent(uid, ref updated);
            Assert.That(regulator.Temperature, Is.EqualTo(300f));
            Assert.That(solution.Temperature, Is.EqualTo(350f));
        });

        await pair.CleanReturnAsync();
    }
}
