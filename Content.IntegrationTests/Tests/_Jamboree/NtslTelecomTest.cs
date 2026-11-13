// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Server._Jamboree.Telecomms;
using Content.Server.Speech.Components;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared.Radio;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Jamboree;

[TestFixture]
public sealed class NtslTelecomTest
{
    private static readonly ProtoId<RadioChannelPrototype> CommonChannel = "Common";
    private static readonly ProtoId<LanguagePrototype> BasicLanguage = "TauCetiBasic";

    private static readonly Regex MonoBlock = new(@"\[mono\](.*?)\[/mono\]", RegexOptions.Singleline | RegexOptions.Compiled);

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: NtslTestTelecomServer
  parent: TelecomServerFilled
  components:
  - type: ApcPowerReceiver
    needsPower: false
";

    private const string Script = @"
def process_signal(sig) {
    words = explode(sig.content, "" "");
    if (sig.source == ""Bob"") { sig.pass = 0; }
    if (words[1] == ""/block"") { return null; }
    if (words[1] == ""/med"") {
        sig.content = substr(sig.content, 6);
        sig.freq = channels.medical;
    }
    if (words[1] == ""/loud"") {
        sig.filters = list(filter_types.loud, ""italics"");
    }
    if (words[1] == ""/all"") {
        sig.pass = 0;
        sig.content = implode(words - words[1], "" "");
        freqs = list(1459, 1447, 1359, 1357, 1355, 1353, 1351, 1349, 1347);
        sent = 0;
        for (i = 1; i <= freqs.len; i += 1) {
            sig.freq = freqs[i];
            sent += broadcast(sig);
        }
        mem(""sent"", sent);
    }
    if (sig.raw == ""what I typed"") { sig.content = sig.raw; }
    if (sig.raw == ""/lang"") { sig.content = sig.language; }
    if (find(sig.raw, ""/cat"") == 1) { sig.content = accent(trim(substr(sig.raw, 5))) + "" nya""; }
    sig.source = upper(sig.source);
    return sig;
}
";

    [Test]
    public async Task ScriptsRewriteRerouteAndBlock()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var ntsl = entMan.System<NtslTelecomSystem>();

        var speaker = EntityUid.Invalid;
        var telecom = EntityUid.Invalid;
        await server.WaitAssertion(() =>
        {
            telecom = entMan.SpawnEntity("NtslTestTelecomServer", testMap.GridCoords);
            speaker = entMan.SpawnEntity(null, testMap.GridCoords);

            Assert.That(ntsl.TryUpload(telecom, Script, speaker, out var error), Is.True, error);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var common = protoMan.Index<RadioChannelPrototype>("Common");
            var basic = protoMan.Index<LanguagePrototype>("TauCetiBasic");
            var draconic = protoMan.Index<LanguagePrototype>("Draconic");

            bool Send(string text, string sender, out string message, out RadioChannelPrototype channel,
                out string name, out NtslFilters filters, LanguagePrototype language = null)
            {
                message = text;
                channel = common;
                name = sender;
                string job = null;
                return ntsl.ProcessRadio(speaker, speaker, language ?? basic, text, ref message, ref channel, ref name, ref job, out filters);
            }

            var muted = Send("hello", "Bob", out _, out _, out _, out _);

            var hissedMessage = "whatsss I typed";
            var hissedChannel = common;
            var hissedName = "Alice";
            string hissedJob = null;
            ntsl.ProcessRadio(speaker, speaker, basic, "what I typed", ref hissedMessage, ref hissedChannel, ref hissedName, ref hissedJob, out _);

            var blocked = Send("/block this", "Alice", out _, out _, out _, out _);
            var rerouted = Send("/med help me", "Alice", out var medMessage, out var medChannel, out var medName, out _);
            var loud = Send("/loud HEY", "Alice", out var loudMessage, out _, out _, out var loudFilters);
            var commandOnly = Send("/loud", "Alice", out _, out _, out _, out _);
            Send("/nothing hi", "Alice", out var unknownMessage, out _, out _, out _);
            var all = Send("/all everyone listen", "Alice", out _, out _, out _, out _, draconic);
            var broadcastLanguages = ntsl.QueuedBroadcastLanguages.ToList();

            entMan.AddComponent<LizardAccentComponent>(speaker);
            Send("/catits so nice", "Alice", out var catMessage, out _, out _, out _);

            var langMessage = "/lang";
            var langChannel = common;
            var langName = "Alice";
            string langJob = null;
            ntsl.ProcessRadio(speaker, speaker, draconic, "/lang", ref langMessage, ref langChannel, ref langName, ref langJob, out _);

            Assert.Multiple(() =>
            {
                Assert.That(muted, Is.False);
                Assert.That(blocked, Is.False);
                Assert.That(rerouted, Is.True);
                Assert.That(medMessage, Is.EqualTo("help me"));
                Assert.That(medChannel.ID, Is.EqualTo("Medical"));
                Assert.That(medName, Is.EqualTo("ALICE"));
                Assert.That(hissedMessage, Is.EqualTo("what I typed"));
                Assert.That(loud, Is.True);
                Assert.That(loudFilters, Is.EqualTo(NtslFilters.Loud | NtslFilters.Italics));
                Assert.That(loudMessage, Is.EqualTo("HEY"));
                Assert.That(commandOnly, Is.False);
                Assert.That(unknownMessage, Is.EqualTo("/nothing hi"));
                Assert.That(all, Is.False);
                Assert.That(catMessage, Is.EqualTo("itsss ssso nice nya"));
                Assert.That(langMessage, Is.EqualTo(draconic.Name));
                Assert.That(ntsl.HideCommand(speaker, "/langHi", basic), Is.EqualTo("Hi"));
                Assert.That(ntsl.HideCommand(speaker, "/CATits so nice", basic), Is.EqualTo("itsss ssso nice"));
                Assert.That(ntsl.HideCommand(speaker, "/lang", basic), Is.Empty);
                Assert.That(ntsl.HideCommand(speaker, "/unknown hi", basic), Is.Null);
                Assert.That(ntsl.HideCommand(speaker, "hello", basic), Is.Null);
                Assert.That(entMan.GetComponent<NtslServerComponent>(telecom).Memory["sent"], Is.EqualTo(8.0));
                Assert.That(broadcastLanguages, Has.Count.EqualTo(8).And.All.EqualTo("Draconic"));
                Assert.That(NtslTelecomSystem.ApplyFilters("hi", NtslFilters.Loud | NtslFilters.Italics), Is.EqualTo("[bolditalic]hi[/bolditalic]"));
                Assert.That(NtslTelecomSystem.ApplyFilters("hi", NtslFilters.Loud | NtslFilters.Robot), Is.EqualTo("[font=\"MonospaceBold\"]hi[/font]"));
                Assert.That(NtslTelecomSystem.ApplyFilters("hi", NtslFilters.Commanding), Is.EqualTo($"[font size={NtslTelecomSystem.CommandingFontSize}][bold]hi[/bold][/font]"));
            });
        });

        await pair.RunTicksSync(5);

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Every full example script in the guidebook page must upload and run without errors.
    /// </summary>
    [Test]
    public async Task GuidebookExamplesWork()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var resMan = server.ResolveDependency<IResourceManager>();
        var ntsl = entMan.System<NtslTelecomSystem>();

        var guide = resMan.ContentFileReadAllText(new ResPath("/ServerInfo/_Jamboree/Guidebook/Engineering/Ntsl.xml"));
        var scripts = MonoBlock.Matches(guide)
            .Select(match => Unescape(match.Groups[1].Value))
            .Where(code => code.Contains("def process_signal"))
            .ToList();

        Assert.That(scripts, Has.Count.GreaterThanOrEqualTo(7));

        var servers = new List<EntityUid>();
        var speaker = EntityUid.Invalid;
        await server.WaitAssertion(() =>
        {
            speaker = entMan.SpawnEntity(null, testMap.GridCoords);
            foreach (var code in scripts)
            {
                var telecom = entMan.SpawnEntity("NtslTestTelecomServer", testMap.GridCoords);
                Assert.That(ntsl.TryUpload(telecom, code, speaker, out var error), Is.True, $"{error}\n{code}");
                ntsl.SetEnabled(telecom, false, speaker);
                servers.Add(telecom);
            }
        });

        await pair.RunTicksSync(5);

        var messages = new[]
        {
            "hello there",
            "/anon secret plan",
            "/anonsecret plan",
            "/all everyone listen",
            "/alleveryone listen",
            "/mute Bob",
            "/8ball will it work?",
            "call the shuttle",
        };

        for (var i = 0; i < servers.Count; i++)
        {
            var telecom = servers[i];
            var code = scripts[i];
            await server.WaitAssertion(() =>
            {
                ntsl.SetEnabled(telecom, true, speaker);
                foreach (var text in messages)
                {
                    var message = text;
                    var channel = protoMan.Index<RadioChannelPrototype>("Common");
                    var name = "Your Name";
                    string job = "Captain";
                    var language = protoMan.Index<LanguagePrototype>("TauCetiBasic");
                    ntsl.ProcessRadio(speaker, speaker, language, text, ref message, ref channel, ref name, ref job, out _);
                }

                ntsl.SetEnabled(telecom, false, speaker);
                Assert.That(entMan.GetComponent<NtslServerComponent>(telecom).Log, Is.Empty, code);
            });

            await pair.RunTicksSync(2);
        }

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Every script the NTSL station events can install compiles and handles messages without errors.
    /// </summary>
    [Test]
    public async Task StationEventScriptsWork()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var compFactory = server.ResolveDependency<IComponentFactory>();
        var ntsl = entMan.System<NtslTelecomSystem>();

        var scripts = protoMan.EnumeratePrototypes<EntityPrototype>()
            .Where(proto => !proto.Abstract)
            .SelectMany(proto => proto.TryComp<NtslScriptRuleComponent>(out var rule, compFactory)
                ? rule.Scripts.Select(script => rule.Header + script)
                : Enumerable.Empty<string>())
            .Select(code => code.Replace("payload_start = time();", "payload_start = time() - 1000;"))
            .ToList();

        Assert.That(scripts.Count(code => code.Contains("payload_start = time() - 1000;")), Is.GreaterThanOrEqualTo(6));

        Assert.That(scripts, Has.Count.GreaterThanOrEqualTo(7));

        var messages = new[]
        {
            "what the hell, shit happens",
            string.Join(" ", Enumerable.Repeat("Help! The captain is dead, the station is on fire and security arrested the clown.", 4)),
        };

        var servers = new List<EntityUid>();
        var speaker = EntityUid.Invalid;
        await server.WaitAssertion(() =>
        {
            speaker = entMan.SpawnAtPosition(null, testMap.GridCoords);
            foreach (var code in scripts)
            {
                var telecom = entMan.SpawnAtPosition("NtslTestTelecomServer", testMap.GridCoords);
                Assert.That(ntsl.TryInstall(telecom, code, out var error), Is.True, $"{error}\n{code}");
                ntsl.SetEnabled(telecom, false, speaker);
                servers.Add(telecom);
            }
        });

        await pair.RunTicksSync(5);

        for (var i = 0; i < servers.Count; i++)
        {
            var telecom = servers[i];
            var code = scripts[i];
            await server.WaitAssertion(() =>
            {
                ntsl.SetEnabled(telecom, true, speaker);

                foreach (var text in messages)
                {
                    var message = text;
                    var channel = protoMan.Index(CommonChannel);
                    var name = "Your Name";
                    string job = "Captain";
                    var passed = ntsl.ProcessRadio(speaker, speaker, protoMan.Index(BasicLanguage), text, ref message, ref channel, ref name, ref job, out _);
                    Assert.That(passed, Is.True, code);

                    if (code.Contains("Corporate Communications Standard") && text == messages[1])
                        Assert.That(message, Does.StartWith("Requesting synergy! The Chief Synergy Officer is permanently offboarded, the profit center is on unplanned thermal event"));

                    if (code.Contains("replace(text, \"a\", \"4\")") && text == messages[0])
                        Assert.That(message, Is.EqualTo("wh47 7h3 h3ll, 5h17 h4pp3n5"));

                    if (code.Contains("-kssht-") && text == messages[1])
                        Assert.That(message, Does.Contain("-kssht-"));
                }

                ntsl.SetEnabled(telecom, false, speaker);
                Assert.That(entMan.GetComponent<NtslServerComponent>(telecom).Log, Is.Empty, code);
            });
        }

        await pair.CleanReturnAsync();
    }

    private static string Unescape(string block)
    {
        var lines = block.Replace("\r", string.Empty)
            .Split('\n')
            .Select(line => line.Trim())
            .Select(line => line.EndsWith("\\n") ? line[..^2] : line)
            .Select(line => line.Replace("\\ ", " ").Replace("\\[", "[").Replace("\\<", "<").Replace("\\>", ">"));

        return string.Join("\n", lines);
    }
}
