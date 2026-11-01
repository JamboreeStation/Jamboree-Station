// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Chemistry.UI.Thermobath;
using Content.Shared.Chemistry.Components;
using Content.Shared.Temperature.Components;

namespace Content.IntegrationTests.Tests.UserInterface;

[TestFixture]
public sealed class ThermobathMenuTest
{
    [Test]
    public async Task NetworkUpdatesDoNotSendControlMessages()
    {
        await using var pair = await PoolManager.GetServerClient();

        await pair.Client.WaitAssertion(() =>
        {
            using var menu = new ThermobathMenu();
            var messages = 0;
            menu.OnPowerChanged += _ => messages++;
            menu.OnSetpointChanged += _ => messages++;
            menu.OnModeChanged += _ => messages++;

            menu.SetTemperatureLimits(253.15f, 573.15f);
            menu.SetPowered(true);
            menu.SetPowerSwitchState(true);
            menu.SetSetpoint(400f);
            menu.SetCurrentTemperature(350f);
            menu.SetMode(ThermobathMode.Heating);
            menu.SetActiveMode(ThermoregulatorActiveMode.Heating);
            menu.SetBeakerPresent(true);
            menu.SetSolutionTemperature(330f);
            menu.SetSolutionTemperature(null);
            menu.SetSolutionTemperature(340f);

            Assert.That(messages, Is.Zero);
        });

        await pair.CleanReturnAsync();
    }
}
