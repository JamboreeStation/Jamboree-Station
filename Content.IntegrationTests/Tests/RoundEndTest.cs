// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using System.Threading;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.TextScreen;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class RoundEndTest
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task CancelPostRoundClearsScreens(bool cancelRestartFirst)
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                DummyTicker = false,
                Map = "Saltern",
                Dirty = true,
            });

            var server = pair.Server;
            var entMan = server.EntMan;
            var ticker = server.System<GameTicker>();
            var roundEnd = server.System<RoundEndSystem>();
            var appearance = server.System<SharedAppearanceSystem>();
            var timing = server.ResolveDependency<IGameTiming>();
            EntityUid screen = default;

            await server.WaitAssertion(() =>
            {
                server.CfgMan.SetCVar(CCVars.EmergencyShuttleEnabled, true);
                var shuttle = server.System<EmergencyShuttleSystem>().GetShuttle();
                Assert.That(shuttle, Is.Not.Null);
                screen = entMan.SpawnEntity("Screen", new EntityCoordinates(shuttle!.Value, Vector2.Zero));
                roundEnd.EndRound(TimeSpan.FromSeconds(2));
            });

            await pair.RunTicksSync(5);

            await server.WaitAssertion(() =>
            {
                Assert.That(appearance.TryGetData(screen, TextScreenVisuals.DefaultText, out string text), Is.True);
                Assert.That(text, Is.EqualTo(ShuttleTimerMasks.Bye));
                Assert.That(appearance.TryGetData(screen, TextScreenVisuals.TargetTime, out TimeSpan target), Is.True);
                Assert.That(target, Is.GreaterThan(timing.CurTime));

                if (cancelRestartFirst)
                    roundEnd.CancelRoundRestartTimer();

                ticker.CancelPostRound();
                Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            });

            await pair.RunTicksSync(5);

            await server.WaitAssertion(() =>
            {
                Assert.That(appearance.TryGetData(screen, TextScreenVisuals.DefaultText, out string text), Is.True);
                Assert.That(text, Is.Empty);
                Assert.That(appearance.TryGetData(screen, TextScreenVisuals.ScreenText, out string label), Is.True);
                Assert.That(label, Is.Empty);
                Assert.That(appearance.TryGetData(screen, TextScreenVisuals.TargetTime, out TimeSpan target), Is.True);
                Assert.That(target, Is.LessThanOrEqualTo(timing.CurTime));

                // A later round end must not be restarted by the old countdown.
                ticker.EndRound();
            });

            await pair.RunSeconds(3);

            await server.WaitAssertion(() =>
            {
                Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PostRound));
                Assert.That(roundEnd.IsRestartTimerActive(), Is.False);
                server.CfgMan.SetCVar(CCVars.EmergencyShuttleEnabled, false);
            });

            await pair.CleanReturnAsync();
        }

        private sealed class RoundEndTestSystem : EntitySystem
        {
            public int RoundCount;

            public override void Initialize()
            {
                base.Initialize();
                SubscribeLocalEvent<RoundEndSystemChangedEvent>(OnRoundEnd);
            }

            private void OnRoundEnd(RoundEndSystemChangedEvent ev)
            {
                RoundCount += 1;
            }
        }

        [Test]
        public async Task Test()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                DummyTicker = false,
                Connected = true,
                Dirty = true
            });

            var server = pair.Server;

            var config = server.ResolveDependency<IConfigurationManager>();
            var sysManager = server.ResolveDependency<IEntitySystemManager>();
            var ticker = sysManager.GetEntitySystem<GameTicker>();
            var roundEndSystem = sysManager.GetEntitySystem<RoundEndSystem>();
            var sys = server.System<RoundEndTestSystem>();
            sys.RoundCount = 0;

            await server.WaitAssertion(() =>
            {
                config.SetCVar(CCVars.GameLobbyEnabled, true);
                config.SetCVar(CCVars.EmergencyShuttleMinTransitTime, 1f);
                config.SetCVar(CCVars.EmergencyShuttleDockTime, 1f);
                config.SetCVar(CCVars.RoundRestartTime, 1f);

                roundEndSystem.DefaultCooldownDuration = TimeSpan.FromMilliseconds(100);
                roundEndSystem.DefaultCountdownDuration = TimeSpan.FromMilliseconds(300);
            });

            await server.WaitAssertion(() =>
            {

                // Press the shuttle call button
                roundEndSystem.RequestRoundEnd();
                Assert.Multiple(() =>
                {
                    Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Not.Null, "Shuttle was called, but countdown time was not set");
                    Assert.That(roundEndSystem.CanCallOrRecall(), Is.False, "Started the shuttle, but didn't have to wait cooldown to press cancel button");
                });
                // Check that we can't recall the shuttle yet
                roundEndSystem.CancelRoundEndCountdown();
                Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Not.Null, "Shuttle was cancelled, even though the button was on cooldown");
            });

            await WaitForEvent(); // Wait for Cooldown

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(roundEndSystem.CanCallOrRecall(), Is.True, "We waited a while, but the cooldown is not expired");
                    Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Not.Null, "We were waiting for the cooldown, but the round also ended");
                });
                // Recall the shuttle, which should trigger the cooldown again
                roundEndSystem.CancelRoundEndCountdown();
                Assert.Multiple(() =>
                {
                    Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Null, "Recalled shuttle, but countdown has not ended");
                    Assert.That(roundEndSystem.CanCallOrRecall(), Is.False, "Recalled shuttle, but cooldown has not been enabled");
                });
            });

            await WaitForEvent(); // Wait for Cooldown

            await server.WaitAssertion(() =>
            {
                Assert.That(roundEndSystem.CanCallOrRecall(), Is.True, "We waited a while, but the cooldown is not expired");
                // Press the shuttle call button
                roundEndSystem.RequestRoundEnd();
            });

            await WaitForEvent(); // Wait for Cooldown

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(roundEndSystem.CanCallOrRecall(), Is.True, "We waited a while, but the cooldown is not expired");
                    Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Not.Null, "The countdown ended, but we just wanted the cooldown to end");
                });
            });

            await WaitForEvent(); // Wait for countdown to end round

            await CheckRunLevel(GameRunLevel.PostRound);

            await WaitForEvent(); // Wait for Restart

            await CheckRunLevel(GameRunLevel.PreRoundLobby);

            Task CheckRunLevel(GameRunLevel level)
            {
                return server.WaitAssertion(() =>
                {
                    Assert.That(ticker.RunLevel, Is.EqualTo(level));
                });
            }

            async Task WaitForEvent()
            {
                const int maxTicks = 60;
                var currentCount = sys.RoundCount;
                for (var i = 0; i < maxTicks; i++)
                {
                    if (currentCount != sys.RoundCount)
                        return;

                    await pair.RunTicksSync(1);
                }

                throw new TimeoutException("Event took too long to trigger");
            }

            // Need to clean self up
            await server.WaitAssertion(() =>
            {
                config.SetCVar(CCVars.GameLobbyEnabled, false);
                config.SetCVar(CCVars.EmergencyShuttleMinTransitTime, CCVars.EmergencyShuttleMinTransitTime.DefaultValue);
                config.SetCVar(CCVars.EmergencyShuttleDockTime, CCVars.EmergencyShuttleDockTime.DefaultValue);
                config.SetCVar(CCVars.RoundRestartTime, CCVars.RoundRestartTime.DefaultValue);

                roundEndSystem.DefaultCooldownDuration = TimeSpan.FromSeconds(30);
                roundEndSystem.DefaultCountdownDuration = TimeSpan.FromMinutes(4);
                ticker.RestartRound();
            });
            await pair.CleanReturnAsync();
        }
    }
}
