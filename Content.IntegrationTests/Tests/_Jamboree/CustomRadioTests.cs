// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable

using System.Linq;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Goobstation.UIKit.UserInterface.Controls;
using Content.Server._EinsteinEngines.Language;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared._Jamboree.CustomRadio;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Client.UserInterface;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Jamboree;

[TestFixture]
public sealed class CustomRadioTests
{
    [TestCase(true, false, true, false, TestName = "ActiveAdminMonitorsCustomRadioWithoutHeadset")]
    [TestCase(true, true, true, false, TestName = "ActiveAdminReceivesOnlyObfuscatedCustomRadioThroughHeadset")]
    [TestCase(false, false, true, false, TestName = "DeadminDoesNotMonitorCustomRadio")]
    [TestCase(false, true, true, false, TestName = "DeadminReceivesObfuscatedCustomRadioThroughHeadset")]
    [TestCase(true, false, false, false, TestName = "ActiveAdminDoesNotMonitorOrdinaryRadio")]
    [TestCase(true, true, false, false, TestName = "ActiveAdminReceivesObfuscatedOrdinaryRadioThroughHeadset")]
    [TestCase(true, false, true, true, TestName = "ActiveAdminRendersPrefixedCustomRadioWithoutHeadset")]
    [TestCase(true, true, true, true, TestName = "ActiveAdminRendersOnlyObfuscatedPrefixedCustomRadioThroughHeadset")]
    public async Task RadioDelivery(bool activeAdmin, bool matchingHeadset, bool customChannel, bool viaChat)
    {
        // Dummy-ticker recycling does not clear runtime channel prototypes.
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Destructive = true });
        var server = pair.Server;
        var client = pair.Client;
        var entities = server.EntMan;
        var players = server.ResolveDependency<IPlayerManager>();
        var admins = server.ResolveDependency<IAdminManager>();
        var prototypes = server.ResolveDependency<IPrototypeManager>();
        var languageSystem = server.System<LanguageSystem>();
        var radioSystem = server.System<RadioSystem>();
        var map = await pair.CreateTestMap();
        var session = players.Sessions.Single();
        EntityUid source = default;
        EntityUid radio = default;
        NetEntity netSource = default;
        EntityUid customKey = default;
        var channelId = "Common";
        ProtoId<LanguagePrototype> languageId = "Draconic";
        const string message = "Custom radio integration monitoring message.";
        string obfuscated = string.Empty;

        await server.WaitAssertion(() =>
        {
            var listener = entities.SpawnAttachedTo("MobHuman", map.GridCoords);
            players.SetAttachedEntity(session, listener);
            source = entities.SpawnAttachedTo(viaChat ? "MobHuman" : null, map.GridCoords);
            radio = entities.SpawnAttachedTo(null, map.GridCoords);
            netSource = entities.GetNetEntity(source);

            // Isolate chat delivery from telecom infrastructure, not from headset reception.
            entities.AddComponent<TelecomExemptComponent>(radio);

            if (customChannel)
            {
                customKey = CreateCustomKey(entities, source, map.GridCoords);
                channelId = entities.GetComponent<CustomEncryptionKeyComponent>(customKey).ChannelId!;
                Assert.That(prototypes.Index<RadioChannelPrototype>(channelId).LocalizedName, Is.EqualTo("Rocco"));
            }

            ProtoId<RadioChannelPrototype> commonId = "Common";
            var common = prototypes.Index(commonId);
            Assert.That(server.ResolveDependency<ILocalizationManager>().TryGetString(common.Name, out var commonName), Is.True);
            Assert.That(common.LocalizedName, Is.EqualTo(commonName).And.EqualTo("Common"));

            var language = prototypes.Index(languageId);
            Assert.That(languageSystem.CanUnderstand(listener, language.ID), Is.False);
            obfuscated = languageSystem.ObfuscateSpeech(message, language);
            Assert.That(obfuscated, Is.Not.EqualTo(message));

            if (viaChat)
            {
                languageSystem.AddLanguage(source, languageId);
                languageSystem.SetLanguage(source, languageId);
            }

            foreach (var wearer in new[] { source, listener }.Where(wearer => wearer == source ? viaChat : matchingHeadset))
            {
                var headset = entities.SpawnAttachedTo("ClothingHeadsetGrey", map.GridCoords);
                var holder = entities.GetComponent<EncryptionKeyHolderComponent>(headset);
                if (customChannel)
                {
                    var key = CreateCustomKey(entities, source, map.GridCoords, customKey);
                    Assert.That(server.System<SharedContainerSystem>().Insert(key, holder.KeyContainer), Is.True);
                    Assert.That(holder.Channels, Does.Contain(new ProtoId<RadioChannelPrototype>(channelId)));
                }
                entities.EnsureComponent<TelecomExemptComponent>(headset);
                Assert.That(server.System<InventorySystem>().TryEquip(wearer, headset, "ears"), Is.True);
                Assert.That(entities.GetComponent<ActiveRadioComponent>(headset).Channels,
                    Does.Contain(new ProtoId<RadioChannelPrototype>(channelId)));
            }

            admins.PromoteHost(session);
            admins.DeAdmin(session);
            if (activeAdmin)
                admins.ReAdmin(session);
            Assert.That(admins.IsAdmin(session), Is.EqualTo(activeAdmin));
        });

        await pair.RunTicksSync(10);
        ChatUIController chat = default!;
        ChatBox mainChat = default!;
        CustomOutputPanel output = default!;
        var historyStart = 0;
        await client.WaitAssertion(() =>
        {
            var ui = client.ResolveDependency<IUserInterfaceManager>();
            chat = ui.GetUIController<ChatUIController>();
            mainChat = new ChatBox { Main = true };
            ui.StateRoot.AddChild(mainChat);
            var filters = mainChat.ChatInput.FilterButton.Popup;
            filters.SetChannels(ChatChannel.Radio);
            foreach (var channel in Enum.GetValues<ChatChannel>())
                filters.SetActive(channel, channel == ChatChannel.Radio);
            Assert.That(filters.IsActive(ChatChannel.Radio), Is.True);
            output = mainChat.FindControl<CustomOutputPanel>("Contents");
            output.Clear();
            historyStart = chat.History.Count;

            var clientPrototypes = client.ResolveDependency<IPrototypeManager>();
            if (customChannel)
                Assert.That(clientPrototypes.Index<RadioChannelPrototype>(channelId).LocalizedName, Is.EqualTo("Rocco"));
            ProtoId<RadioChannelPrototype> commonId = "Common";
            var common = clientPrototypes.Index(commonId);
            Assert.That(client.ResolveDependency<ILocalizationManager>().TryGetString(common.Name, out var commonName), Is.True);
            Assert.That(common.LocalizedName, Is.EqualTo(commonName).And.EqualTo("Common"));
        });

        await server.WaitPost(() =>
        {
            if (viaChat)
                server.System<ChatSystem>().TrySendInGameICMessage(
                    source, $":#rocco {message}", InGameICChatType.Speak, ChatTransmitRange.Normal);
            else
                radioSystem.SendRadioMessage(source, message, prototypes.Index<RadioChannelPrototype>(channelId), radio,
                    prototypes.Index(languageId));
        });
        await pair.RunTicksSync(10);

        await client.WaitAssertion(() =>
        {
            var received = chat.History.Skip(historyStart)
                .Where(entry => entry.Msg.Channel == ChatChannel.Radio && entry.Msg.SenderEntity == netSource)
                .Select(entry => entry.Msg)
                .ToArray();
            var expectedCount = matchingHeadset || activeAdmin && customChannel ? 1 : 0;
            Assert.That(received, Has.Length.EqualTo(expectedCount));
            Assert.That(output.EntryCount, Is.EqualTo(expectedCount), "Main ChatBox rendered radio lines");
            if (expectedCount == 0)
                return;

            var monitoringCopy = activeAdmin && customChannel && !matchingHeadset;
            var expectedText = monitoringCopy ? message : obfuscated;
            Assert.That(received[0].Message, Is.EqualTo(expectedText));
            Assert.That(received[0].WrappedMessage, Does.Contain(expectedText));
            Assert.That(received[0].Read, Is.True);
            if (customChannel)
                Assert.That(received[0].WrappedMessage, Does.Contain("Rocco"));

            if (monitoringCopy)
            {
                Assert.That(received[0].WrappedMessage, Does.Contain("\\[ADMIN|Rocco\\]"));
                Assert.That(received[0].MessageColorOverride, Is.EqualTo(Color.Lime));
            }
            else
            {
                Assert.That(received[0].WrappedMessage, Does.Not.Contain("ADMIN|"));
            }
        });

        await client.WaitPost(() => mainChat.Dispose());
        await pair.CleanReturnAsync();
    }

    [TestCase(false, false, TestName = "KeyedNonAdminAndUnkeyedAdminReceiveSameCustomTransmission")]
    [TestCase(false, true, TestName = "KeyedNonAdminReadsCustomRadioAlongsideUnkeyedAdmin")]
    [TestCase(true, false, TestName = "IntrinsicAdminReceptionSuppressesMonitoringAlongsideKeyedNonAdmin")]
    public async Task SimultaneousRadioRecipients(bool intrinsicAdmin, bool understandsLanguage)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Destructive = true });
        await using var userPair = await PoolManager.GetServerClient(new PoolSettings { Connected = false, Destructive = true });
        var server = pair.Server;
        var client = userPair.Client;
        var adminClient = pair.Client;
        var userNetwork = client.ResolveDependency<IClientNetManager>();
        var entities = server.EntMan;
        var players = server.ResolveDependency<IPlayerManager>();
        var admins = server.ResolveDependency<IAdminManager>();
        var prototypes = server.ResolveDependency<IPrototypeManager>();
        var languageSystem = server.System<LanguageSystem>();
        var map = await pair.CreateTestMap();
        var adminSession = players.Sessions.Single();

        // Keep the original local admin; the named second connection must never gain admin rights here.
        await server.WaitPost(() => server.CfgMan.SetCVar(CCVars.ConsoleLoginLocal, false));
        await client.WaitIdleAsync();
        await server.WaitIdleAsync();
        client.SetConnectTarget(server);
        await client.WaitPost(() => userNetwork.ClientConnect(null!, 0, "custom_radio_user"));
        await RunTicks(20);
        var userSession = players.Sessions.Single(session => session.Name == "custom_radio_user");

        EntityUid source = default;
        EntityUid transmitter = default;
        EntityUid telecom = default;
        NetEntity netSource = default;
        string channelId = default!;
        ProtoId<LanguagePrototype> languageId = "Draconic";
        const string message = "One custom transmission for a keyed user and a separate administrator.";
        string obfuscated = default!;

        await server.WaitAssertion(() =>
        {
            Assert.That(players.Sessions, Has.Length.EqualTo(2));
            Assert.That(adminSession, Is.Not.SameAs(userSession));
            var listener = entities.SpawnAttachedTo("MobHuman", map.GridCoords);
            var adminListener = entities.SpawnAttachedTo("MobHuman", map.GridCoords);
            players.SetAttachedEntity(userSession, listener);
            players.SetAttachedEntity(adminSession, adminListener);
            Assert.That(admins.IsAdmin(userSession), Is.False, "The keyed user is never promoted or deadminned.");
            Assert.That(admins.AllAdmins, Does.Not.Contain(userSession));
            admins.PromoteHost(adminSession);
            Assert.That(admins.IsAdmin(adminSession), Is.True);

            source = entities.SpawnAttachedTo("MobHuman", map.GridCoords);
            netSource = entities.GetNetEntity(source);
            languageSystem.AddLanguage(source, languageId);
            languageSystem.SetLanguage(source, languageId);
            if (understandsLanguage)
                languageSystem.AddLanguage(listener, languageId);
            Assert.That(languageSystem.CanUnderstand(listener, languageId), Is.EqualTo(understandsLanguage));
            Assert.That(languageSystem.CanUnderstand(adminListener, languageId), Is.False);
            obfuscated = languageSystem.ObfuscateSpeech(message, prototypes.Index(languageId));
            Assert.That(obfuscated, Is.Not.EqualTo(message));

            var key = CreateCustomKey(entities, source, map.GridCoords);
            channelId = entities.GetComponent<CustomEncryptionKeyComponent>(key).ChannelId!;
            transmitter = entities.SpawnAttachedTo("ClothingHeadsetGrey", map.GridCoords);
            var receiver = entities.SpawnAttachedTo("ClothingHeadsetGrey", map.GridCoords);
            telecom = entities.SpawnAttachedTo("TelecomServer", map.GridCoords);
            entities.SpawnAttachedTo("CableApcExtension", map.GridCoords);
            entities.SpawnAttachedTo("APCBasic", map.GridCoords);

            foreach (var holderEntity in new[] { transmitter, receiver, telecom })
            {
                var copy = CreateCustomKey(entities, source, map.GridCoords, key);
                var holder = entities.GetComponent<EncryptionKeyHolderComponent>(holderEntity);
                Assert.That(server.System<SharedContainerSystem>().Insert(copy, holder.KeyContainer), Is.True);
                Assert.That(holder.Channels, Does.Contain(new ProtoId<RadioChannelPrototype>(channelId)));
                Assert.That(entities.HasComponent<TelecomExemptComponent>(holderEntity), Is.False);
            }

            Assert.That(server.System<InventorySystem>().TryEquip(source, transmitter, "ears"), Is.True);
            Assert.That(server.System<InventorySystem>().TryEquip(listener, receiver, "ears"), Is.True);
            Assert.That(entities.GetComponent<ActiveRadioComponent>(receiver).Channels,
                Does.Contain(new ProtoId<RadioChannelPrototype>(channelId)));
            Assert.That(entities.HasComponent<WearingHeadsetComponent>(adminListener), Is.False);

            if (intrinsicAdmin)
            {
                entities.AddComponent<IntrinsicRadioReceiverComponent>(adminListener);
                entities.AddComponent<ActiveRadioComponent>(adminListener).Channels.Add(channelId);
            }
        });

        await RunTicks(120);
        await server.WaitAssertion(() =>
        {
            var power = entities.GetComponent<ApcPowerReceiverComponent>(telecom);
            Assert.That(power.NeedsPower, Is.True);
            Assert.That(power.Provider, Is.Not.Null);
            Assert.That(power.Powered, Is.True, "Normal headset reception requires a genuinely powered, keyed telecom.");
            Assert.That(power.PowerReceived, Is.GreaterThan(0));
        });

        ChatUIController userChat = default!;
        ChatUIController adminChat = default!;
        var userHistoryStart = 0;
        var adminHistoryStart = 0;
        await client.WaitAssertion(() =>
        {
            userChat = client.ResolveDependency<IUserInterfaceManager>().GetUIController<ChatUIController>();
            userHistoryStart = userChat.History.Count;
            Assert.That(client.ResolveDependency<IPrototypeManager>().Index<RadioChannelPrototype>(channelId).LocalizedName,
                Is.EqualTo("Rocco"));
        });
        await adminClient.WaitAssertion(() =>
        {
            adminChat = adminClient.ResolveDependency<IUserInterfaceManager>().GetUIController<ChatUIController>();
            adminHistoryStart = adminChat.History.Count;
            Assert.That(adminClient.ResolveDependency<IPrototypeManager>().Index<RadioChannelPrototype>(channelId).LocalizedName,
                Is.EqualTo("Rocco"));
        });

        await server.WaitPost(() => server.System<ChatSystem>().TrySendInGameICMessage(
            source, $":#rocco {message}", InGameICChatType.Speak, ChatTransmitRange.Normal));
        await RunTicks(10);

        await client.WaitAssertion(() =>
        {
            var received = userChat.History.Skip(userHistoryStart)
                .Where(entry => entry.Msg.Channel == ChatChannel.Radio && entry.Msg.SenderEntity == netSource)
                .Select(entry => entry.Msg).ToArray();
            Assert.That(received, Has.Length.EqualTo(1));
            var expectedText = understandsLanguage ? message : obfuscated;
            Assert.That(received[0].Message, Is.EqualTo(expectedText));
            Assert.That(received[0].WrappedMessage, Does.Contain("Rocco").And.Contain(expectedText).And.Not.Contain("ADMIN|"));
        });
        await adminClient.WaitAssertion(() =>
        {
            var received = adminChat.History.Skip(adminHistoryStart)
                .Where(entry => entry.Msg.Channel == ChatChannel.Radio && entry.Msg.SenderEntity == netSource)
                .Select(entry => entry.Msg).ToArray();
            Assert.That(received, Has.Length.EqualTo(1));
            Assert.That(received[0].Message, Is.EqualTo(intrinsicAdmin ? obfuscated : message));
            if (intrinsicAdmin)
                Assert.That(received[0].WrappedMessage, Does.Contain("Rocco").And.Contain(obfuscated).And.Not.Contain("ADMIN|"));
            else
            {
                Assert.That(received[0].WrappedMessage, Does.Contain("\\[ADMIN|Rocco\\]").And.Contain(message));
                Assert.That(received[0].MessageColorOverride, Is.EqualTo(Color.Lime));
            }
        });
        await server.WaitAssertion(() =>
        {
            Assert.That(admins.IsAdmin(userSession), Is.False);
            Assert.That(admins.AllAdmins, Does.Not.Contain(userSession));
        });

        await client.WaitPost(() => userNetwork.ClientDisconnect("Custom radio test complete"));
        await RunTicks(10);
        await userPair.CleanReturnAsync();
        await pair.CleanReturnAsync();

        async Task RunTicks(int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                await pair.RunTicksSync(1);
                await client.WaitRunTicks(1);
            }
        }
    }

    private static EntityUid CreateCustomKey(IEntityManager entities, EntityUid user, EntityCoordinates coordinates,
        EntityUid? copyFrom = null)
    {
        var key = entities.SpawnAttachedTo("EncryptionKeyBlank", coordinates);
        Assert.That(entities.GetComponent<CustomEncryptionKeyComponent>(key).ChannelId, Is.Null);
        if (copyFrom is { } original)
        {
            var copy = new InteractUsingEvent(user, original, key, coordinates);
            entities.EventBus.RaiseLocalEvent(key, copy);
            Assert.That(copy.Handled, Is.True);
            Assert.That(entities.GetComponent<CustomEncryptionKeyComponent>(key).ChannelId,
                Is.EqualTo(entities.GetComponent<CustomEncryptionKeyComponent>(original).ChannelId));
        }
        else
        {
            entities.EventBus.RaiseLocalEvent(key,
                new CustomEncryptionKeySetupMessage("Rocco", "rocco", Color.Lime) { Actor = user });
        }

        var channelId = entities.GetComponent<CustomEncryptionKeyComponent>(key).ChannelId;
        Assert.That(channelId, Is.Not.Null);
        Assert.That(entities.GetComponent<EncryptionKeyComponent>(key).Channels,
            Is.EquivalentTo(new[] { new ProtoId<RadioChannelPrototype>(channelId!) }));
        return key;
    }
}
