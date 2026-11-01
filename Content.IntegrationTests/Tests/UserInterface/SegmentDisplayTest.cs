// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;

namespace Content.IntegrationTests.Tests.UserInterface;

[TestFixture]
public sealed class SegmentDisplayTest
{
    [Test]
    public async Task SignedValuesAndResizingStayWithinDisplayBounds()
    {
        await using var pair = await PoolManager.GetServerClient();

        await pair.Client.WaitAssertion(() =>
        {
            using var display = new SegmentDisplay { DigitCount = 4, Value = -200, DecimalPosition = 1 };
            Assert.That(display.Value, Is.EqualTo(-200));

            display.Value = int.MinValue;
            Assert.That(display.Value, Is.EqualTo(-999));
            display.Value = int.MaxValue;
            Assert.That(display.Value, Is.EqualTo(9999));

            display.DigitCount = 10;
            display.Value = int.MaxValue;
            Assert.That(display.Value, Is.EqualTo(int.MaxValue));
            display.Value = int.MinValue;
            Assert.That(display.Value, Is.EqualTo(-999999999));

            display.DigitCount = 0;
            Assert.Multiple(() =>
            {
                Assert.That(display.DigitCount, Is.EqualTo(1));
                Assert.That(display.Value, Is.Zero);
                Assert.That(display.DecimalPosition, Is.Zero);
            });

            display.DecimalPosition = int.MinValue;
            Assert.That(display.DecimalPosition, Is.EqualTo(-1));
            display.DigitCount = int.MaxValue;
            Assert.That(display.DigitCount, Is.EqualTo(10));
        });

        await pair.CleanReturnAsync();
    }
}
