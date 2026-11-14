// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Speech;
using Robust.Shared.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests.Chat;

[TestFixture]
public sealed class EmotePrototypeTest
{
    [Test]
    public async Task PortedEmotesHaveSingleCanonicalDefinitions()
    {
        await using var pair = await PoolManager.GetServerClient();
        var resources = pair.Server.ResolveDependency<IResourceManager>();
        var expected = new Dictionary<(string Type, string Id), string>
        {
            [("emote", "Scree")] = "/Prototypes/_Goobstation/Voice/speech_emotes.yml",
            [("emote", "Call")] = "/Prototypes/_Goobstation/Voice/speech_emotes.yml",
            [("emote", "Squawk")] = "/Prototypes/_Goobstation/Voice/speech_emotes.yml",
            [("emote", "Blink")] = "/Prototypes/Eye/Blinking/blinking_emotes.yml",
            [("soundCollection", "ResomiScrees")] = "/Prototypes/_StarLight/SoundCollections/resomi.yml",
            [("soundCollection", "ResomiCalls")] = "/Prototypes/_StarLight/SoundCollections/resomi.yml",
        };
        var definitions = expected.ToDictionary(entry => entry.Key, _ => new List<ResPath>());
        var relevantId = new Regex(@"\b(?:Scree|Call|Squawk|Blink|ResomiScrees|ResomiCalls)\b");

        // The loaded manager can hide duplicate IDs, so inspect the actual resource files too.
        foreach (var path in resources.ContentFindFiles(new ResPath("/Prototypes/"))
                     .Where(path => path.Extension == "yml" && !path.Filename.StartsWith('.')))
        {
            using var reader = new StreamReader(resources.ContentFileRead(path));
            var text = reader.ReadToEnd();
            if (!relevantId.IsMatch(text))
                continue;

            var yaml = new YamlStream();
            using var yamlReader = new StringReader(text);
            Assert.DoesNotThrow(() => yaml.Load(yamlReader), $"Invalid YAML: {path}");
            foreach (var node in yaml.Documents.SelectMany(document => (YamlSequenceNode) document.RootNode)
                         .Cast<YamlMappingNode>())
            {
                var key = (node.GetNode("type").AsString(), node.GetNode("id").AsString());
                if (definitions.TryGetValue(key, out var paths))
                    paths.Add(path);
            }
        }

        var localeKeys = new[] { "scree", "call", "squawk", "blink" }
            .SelectMany(emote => new[] { $"chat-emote-name-{emote}", $"chat-emote-msg-{emote}" })
            .ToDictionary(key => key, _ => new List<ResPath>());
        var messageId = new Regex(@"(?m)^(chat-emote-(?:name|msg)-(?:scree|call|squawk|blink))[ \t]*=");
        foreach (var path in resources.ContentFindFiles(new ResPath("/Locale/en-US/"))
                     .Where(path => path.Extension == "ftl"))
        {
            using var reader = new StreamReader(resources.ContentFileRead(path));
            foreach (Match match in messageId.Matches(reader.ReadToEnd()))
                localeKeys[match.Groups[1].Value].Add(path);
        }

        Assert.Multiple(() =>
        {
            foreach (var (key, path) in expected)
                Assert.That(definitions[key], Is.EqualTo(new[] { new ResPath(path) }), $"{key}");

            foreach (var (key, paths) in localeKeys)
                Assert.That(paths, Is.EqualTo(new[] { new ResPath("/Locale/en-US/_Goobstation/chat/emotes.ftl") }), key);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConsolidationPreservesTriggersAllowancesAndSounds()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var resources = server.ResolveDependency<IResourceManager>();
        var prototypes = server.ResolveDependency<IPrototypeManager>();
        var coreIds = new[]
        {
            "MaleHuman", "FemaleHuman", "Cluwne", "UnisexSilicon",
            "GeneralBodyEmotes", "DionaBodyEmotes", "ReptilianBodyEmotes", "FeroxiBodyEmotes",
        };

        // A duplicate mapping key can make the entire core sound file fail to load.
        using var reader = new StreamReader(resources.ContentFileRead(new ResPath("/Prototypes/Voice/speech_emote_sounds.yml")));
        var yaml = new YamlStream();
        Assert.DoesNotThrow(() => yaml.Load(reader), "Core speech_emote_sounds.yml must parse without duplicate keys.");
        var rawIds = yaml.Documents.SelectMany(document => (YamlSequenceNode) document.RootNode)
            .Cast<YamlMappingNode>()
            .Where(node => node.GetNode("type").AsString() == "emoteSounds")
            .Select(node => node.GetNode("id").AsString()).ToArray();
        Assert.That(rawIds, Is.SupersetOf(coreIds));

        await server.WaitAssertion(() =>
        {
            foreach (var id in coreIds)
            {
                var sounds = prototypes.Index<EmoteSoundsPrototype>(id);
                Assert.That(sounds.Sounds.Count > 0 || sounds.FallbackSound != null, Is.True, id);
            }

            var forms = new Dictionary<string, string[]>
            {
                ["Scree"] = new[] { "scree", "screes", "screeing", "screed" },
                ["Call"] = new[] { "call", "calls", "calling", "called" },
                ["Squawk"] = new[] { "squawk", "squawks", "squawking", "squawked" },
            };
            foreach (var (id, triggers) in forms)
            {
                var emote = prototypes.Index<EmotePrototype>(id);
                Assert.That(emote.Available, Is.False, id);
                Assert.That(emote.Whitelist, Is.Null, id);
                Assert.That(emote.Blacklist?.Components, Does.Contain("BorgChassis"), id);
                var expectedTriggers = triggers.Take(3)
                    .SelectMany(trigger => new[] { "", ".", "!", "?" }.Select(suffix => trigger + suffix))
                    .Concat(triggers.Skip(3));
                Assert.That(emote.ChatTriggers, Is.SupersetOf(expectedTriggers), id);
            }

            Assert.That(prototypes.Index<EmotePrototype>("Blink").ChatTriggers,
                Is.SupersetOf(new[] { "blink", "blinks", "blinked" }));

            foreach (var id in new[] { "ResomiScrees", "ResomiCalls", "ResomiSquawk", "ResomiSquawks", "Blinks" })
                Assert.That(prototypes.Index<SoundCollectionPrototype>(id), Is.Not.Null, id);

            var blink = prototypes.Index<EmoteSoundsPrototype>("ReptilianBodyEmotes").Sounds["Blink"];
            Assert.That(blink, Is.TypeOf<SoundCollectionSpecifier>());
            Assert.That(((SoundCollectionSpecifier) blink).Collection?.ToString(), Is.EqualTo("Blinks"));

            foreach (var id in new[] { "MobHarpy", "MobResomi", "MobAvali" })
            {
                var mob = prototypes.Index<EntityPrototype>(id);
                Assert.That(mob.Components.TryGetComponent("Speech", out var component), Is.True, id);
                var speech = (SpeechComponent) component;
                foreach (var emote in forms.Keys)
                    Assert.That(speech.AllowedEmotes.Contains(emote), Is.True, $"{id}: {emote}");
            }

            var mimicKeys = new[]
            {
                "Hiss", "Meow", "Mew", "Growl", "Purr", "Ring", "Pew", "Bang", "Beep", "Rev",
                "Click", "Chitter", "Squeak", "Snarl", "Bark", "Whine", "Howl", "Awoo", "Honk", "Weh", "Yip", "Mars",
            };
            foreach (var id in new[] { "SoundsHarpyMale", "SoundsHarpyFemale" })
            {
                var sounds = prototypes.Index<EmoteSoundsPrototype>(id);
                foreach (var emote in mimicKeys.Concat(forms.Keys))
                    Assert.That(sounds.Sounds.ContainsKey(emote), Is.True, $"{id}: {emote}");
            }
        });

        await pair.CleanReturnAsync();
    }
}
