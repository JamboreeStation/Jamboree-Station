// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Localization;

[TestFixture]
public sealed class MissingLocalizationTest
{
    [Test]
    public async Task RepairedMessagesFormatAtRuntime()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var loc = server.ResolveDependency<ILocalizationManager>();
        var entities = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            Assert.That(loc.DefaultCulture?.Name, Is.EqualTo("en-US"));
            var subject = entities.SpawnEntity(null, MapCoordinates.Nullspace);
            entities.System<MetaDataSystem>().SetEntityName(subject, "localization subject");
            try
            {
                Assert.Multiple(() =>
                {
                    // Explicit bodies, not document titles or a prefix-based existence check.
                    foreach (var id in new[]
                    {
                        "doc-text-printer-report-on-elimination-of-violations",
                        "doc-text-printer-internal-affairs-agents-report",
                        "doc-text-printer-experiment-report",
                        "doc-text-printer-disposal-report",
                        "doc-text-printer-appeal",
                        "doc-text-printer-request-call-members-central-committee-dso",
                        "doc-text-printer-request-to-establish-threat-level",
                        "doc-text-printer-request-change-salary",
                        "doc-text-printer-complaint-violation-labor-rules",
                        "doc-text-printer-permission-to-travel-in-case-of-threat",
                        "doc-text-printer-permission-to-extend-marriage",
                        "doc-text-printer-order-purchase-resources-equipment",
                        "doc-text-printer-ordering-special-equipment",
                        "doc-text-printer-order-purchase-weapons",
                        "doc-text-printer-certificate",
                    })
                    {
                        Assert.That(Markup(id), Does.Contain("\n").And.Contain("[head=3]"), id);
                    }

                    foreach (var id in new[]
                    {
                        "alert-level-amber-unlocked-announcement", "alert-level-amber-locked",
                        "alert-level-amber-verb-text", "alert-level-amber-verb-message",
                        "alert-level-amber-unlocked", "alert-level-amber-no-id",
                        "alert-level-amber-needs-command", "alert-level-amber-first-swipe",
                        "alert-level-amber-same-id", "alert-level-amber-needs-second-command",
                        "alert-level-amber-unknown-name",
                        "ghost-role-information-pirate-description", "ghost-role-information-pirate-captain-description",
                        "ghost-role-information-pirate-irs-name", "ghost-role-information-pirate-irs-captain-name",
                        "job-name-boxer", "job-name-zookeeper", "job-description-boxer", "job-description-zookeeper",
                        "mindcontrolled", "mindcontrol-role-objective", "roles-antag-contractor-description",
                        "ghost-role-information-cburn-agent-description", "ghost-role-information-Death-Squad-description",
                        "ghost-role-information-listeningop-name", "ghost-role-information-listeningop-description",
                        "ghost-role-information-special-operations-officer-name", "job-description-navy-special-operations-officer",
                        "ghost-role-information-inspector-description", "abductor-victim-role-desc",
                        "ghost-role-information-lost-challenge-cargo-technican-rules",
                        "ghost-role-information-slaughter-demon-rules", "ghost-role-information-nukeop-reinforcement-rules",
                        "admin-verb-make-changeling", "admin-verb-text-make-changeling",
                        "admin-verb-make-space-ninja", "admin-verb-text-make-space-ninja",
                        "admin-verb-make-wizard", "admin-verb-text-make-wizard",
                        "time-transfer-panel-warning-no-perms",
                        "blob-no-using-guns-popup", "blob-verb-upgrade", "speak-vv-blob",
                        "revival-contract-no-mind", "gps-window-position-unknown", "xenomorphs-acid-not-enough-plasma",
                        "cargo-pallet-menu-no-goods-text", "implanter-invalid-mode",
                        "station-ai-fixer-console-window-controls", "list-view-window-default-title",
                        "glimmer-monitor-interval", "glimmer-monitor-sync",
                        "emergency-shuttle-command-round-yes", "emergency-shuttle-command-round-no",
                        "grinder-menu-auto-button-off", "portable-generator-ui-status-stopped",
                        "crew-monitoring-ui-job-label", "round-end-system-shuttle-sender-announcement",
                        "injector-component-needle-injecting-user", "changeling-action-fail-onfire",
                        "cmd-setmind-command-target-has-no-mind-message", "cmd-setmind-command-target-has-no-content-data-message",
                        "cmd-rmobjective-invalid-args", "cmd-rmobjective-player-not-found", "cmd-rmobjective-mind-not-found",
                        "cmd-dungen-arg-count", "cmd-dungen-map-parse", "cmd-dungen_pack_vis-success", "whitelist-manual",
                        "cmd-mappinglavaland-desc", "cmd-mappinglavaland-help",
                        "cmd-mappinglavaland-missing-prototype", "cmd-mappinglavaland-load-failed",
                    })
                    {
                        Format(id);
                    }

                    // Includes the dynamic PsionicComponent descriptors used by Oracle and Sophia.
                    foreach (var descriptor in new[]
                    {
                        "ignorant", "male", "female", "old", "young", "masculine", "feminine",
                        "liminal", "hylic", "hungry", "pneumatic", "light", "heavy", "emanative",
                        "vampiric", "dumb", "passive", "kinetic", "gnostic", "demiurgic", "mysterious",
                    })
                    {
                        Format($"p-descriptor-{descriptor}");
                    }

                    Assert.That(loc.TryGetString("never-shown", out var hidden), Is.True);
                    Assert.That(hidden, Is.EqualTo(string.Empty), "never-shown is intentionally empty");

                    foreach (var (id, argument) in new[]
                    {
                        ("injector-component-target-already-full-message", "target"),
                        ("revival-contract-use-failed", "target"),
                        ("cqc-fail-used", "manual"),
                        ("strippable-component-item-slot-occupied-message", "owner"),
                        ("chitzite-mask", "mask"),
                        ("mining-voucher-unpowered", "vendor"),
                    })
                    {
                        Assert.That(Markup(id, (argument, subject)), Does.Contain("localization subject"), id);
                    }
                    Assert.That(Markup("bloodstream-component-profusely-bleeding", ("target", subject)),
                        Does.Contain("profusely bleeding"));

                    foreach (var (id, args, expected) in new (string, (string, object)[], string)[]
                    {
                        ("cargo-console-menu-order-row-product-description", new (string, object)[] { ("orderReason", "Engine repairs") }, "Engine repairs"),
                        ("glimmer-monitor-current-glimmer", new (string, object)[] { ("glimmer", 123) }, "123"),
                        ("alert-level-amber-authorized-initiated-announcement", new (string, object)[] { ("name", "Alex Command") }, "Alex Command"),
                        ("alert-level-amber-authorized-announcement", new (string, object)[] { ("name", "Alex Command") }, "Alex Command"),
                        ("ntr-console-invalid-reagent-proto", new (string, object)[] { ("reagentId", "MissingReagent") }, "MissingReagent"),
                        ("traitor-role-uplink-pen-code", new (string, object)[] { ("code", "90-180-270") }, "90-180-270"),
                        ("set-game-preset-preset-error", new (string, object)[] { ("preset", "MissingPreset") }, "MissingPreset"),
                        ("gun-set-fire-mode-popup", new (string, object)[] { ("mode", "stun") }, "stun"),
                        ("cmd-mappinglavaland-invalid-prototype", new (string, object)[] { ("prototype", "MissingLavaland") }, "MissingLavaland"),
                        ("cmd-mappinglavaland-invalid-seed", new (string, object)[] { ("seed", "not-a-number") }, "not-a-number"),
                        ("cmd-mappinglavaland-success", new (string, object)[] { ("map", "Lavaland (42)") }, "Lavaland (42)"),
                        ("shell-need-between-arguments", new (string, object)[] { ("lower", 1), ("upper", 2) }, "1 to 2"),
                        ("ship-gun-class-component-examine-detail", new (string, object)[] { ("processingPower", 17) }, "17"),
                    })
                    {
                        Assert.That(Markup(id, args), Does.Contain(expected), id);
                    }
                    foreach (var count in new[] { 0, 1, 3 })
                    {
                        Assert.That(Markup("bin-component-on-examine-text", ("count", count)), Does.Contain(count.ToString()));
                    }

                    foreach (var chance in new[] { 1f, .25f })
                    {
                        foreach (var (id, certain, possible) in new[]
                        {
                            ("reagent-effect-guidebook-mutate-plants-nearby", "Increases", "increase"),
                            ("reagent-effect-guidebook-dnascramble", "Scrambles", "scramble"),
                            ("reagent-effect-guidebook-oxygenate-nearby", "Adjusts", "adjust"),
                            ("reagent-effect-guidebook-extinguish-nearby", "Extinguishes", "extinguish"),
                            ("reagent-effect-guidebook-ignite-nearby", "Adjusts", "adjust"),
                            ("reagent-effect-guidebook-scramble-nearby", "Scrambles", "scramble"),
                            ("entity-effect-guidebook-emp", "Causes", "cause"),
                            ("entity-effect-guidebook-flash", "Causes", "cause"),
                            ("entity-effect-guidebook-random-polymorph", "Polymorphs", "polymorph"),
                            ("reagent-effect-guidebook-chem-reroll-psionic", "Attempts", "attempt"),
                            ("reagent-effect-guidebook-chem-remove-psionic", "Removes", "remove"),
                            ("reagent-effect-guidebook-chem-restorereroll-psionic", "Restores", "restore"),
                        })
                        {
                            Assert.That(Markup(id, ("chance", chance)), Does.StartWith(chance == 1f ? certain : possible), id);
                        }

                        foreach (var amount in new[] { 1, 3 })
                        {
                            var text = Markup("reagent-effect-guidebook-create-entity-reaction-effect",
                                ("chance", chance), ("entname", "banana"), ("amount", amount));
                            Assert.That(text, Does.StartWith(chance == 1f ? "Creates" : "create"));
                            Assert.That(text, Does.Contain(amount == 1 ? "a banana" : "between 1 and 3 bananas"));
                        }
                        foreach (var count in new[] { -2.5f, 2.5f })
                        {
                            var text = Markup("reagent-effect-guidebook-change-glimmer-reaction-effect",
                                ("chance", chance), ("count", count));
                            Assert.That(text, Does.StartWith(chance == 1f ? "Adjusts" : "adjust"));
                            Assert.That(text, Does.Contain(count < 0 ? "-2.5" : "2.5"));
                        }
                        foreach (var conditionCount in new[] { 0, 1, 2 })
                        {
                            var effect = Markup("entity-effect-guidebook-emp", ("chance", chance));
                            var conditions = conditionCount switch
                            {
                                0 => "",
                                1 => "the target is alive",
                                _ => "the target is alive and standing on a tile",
                            };
                            var text = Markup("guidebook-nested-effect-description", ("effect", effect),
                                ("chance", chance), ("conditionCount", conditionCount), ("conditions", conditions));
                            Assert.That(text, Does.Contain(effect));
                            Assert.That(text.StartsWith("Has a 25% chance to"), Is.EqualTo(chance != 1f));
                            Assert.That(text, Does.EndWith(conditionCount == 0 ? "." : $" when {conditions}."));
                            Assert.That(text.Contains(" when "), Is.EqualTo(conditionCount != 0));
                        }
                    }
                });
            }
            finally
            {
                entities.DeleteEntity(subject);
            }

            string Format(string id, params (string, object)[] args)
            {
                Assert.That(loc.TryGetString(id, out var text, args), Is.True, id);
                Assert.That(text, Is.Not.Null.And.Not.Empty.And.Not.EqualTo(id), id);
                return text ?? string.Empty;
            }

            string Markup(string id, params (string, object)[] args)
            {
                var text = Format(id, args);
                Assert.That(FormattedMessage.TryFromMarkup(text, out _, out var error), Is.True, $"{id}: {error}");
                return text;
            }
        });

        await pair.CleanReturnAsync();
    }
}
