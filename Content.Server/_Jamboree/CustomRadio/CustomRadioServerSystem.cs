// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Jamboree.CustomRadio;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Jamboree.CustomRadio;

/// <summary>
/// Lists and edits the custom channels whose keys are installed in a telecommunication server.
/// </summary>
public sealed class CustomRadioServerSystem : EntitySystem
{
    [Dependency] private readonly CustomRadioChannelSystem _channels = default!;
    [Dependency] private readonly CustomEncryptionKeySystem _keys = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelecomServerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<TelecomServerComponent, CustomRadioServerEditMessage>(OnEdit);
        SubscribeLocalEvent<TelecomServerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUiOpened(Entity<TelecomServerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (CustomRadioServerUiKey.Key.Equals(args.UiKey))
            UpdateUi(ent);
    }

    private void OnEdit(Entity<TelecomServerComponent> ent, ref CustomRadioServerEditMessage args)
    {
        if (!TryComp<EncryptionKeyHolderComponent>(ent, out var holder) || !holder.Channels.Contains(args.ChannelId))
            return;

        if (!_channels.TryEditChannel(args.Actor, args.ChannelId, args.Name, args.Key, args.Color, out var error))
        {
            _popup.PopupEntity(Loc.GetString(error!), ent, args.Actor);
            return;
        }

        var query = EntityQueryEnumerator<CustomEncryptionKeyComponent>();
        while (query.MoveNext(out var uid, out var key))
        {
            if (key.ChannelId == args.ChannelId)
                _keys.UpdateKey((uid, key));
        }

        _popup.PopupEntity(Loc.GetString("custom-radio-server-saved"), ent, args.Actor);
        UpdateUi(ent);
    }

    private void OnExamined(Entity<TelecomServerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(CustomRadioServerSystem)))
        {
            foreach (var entry in GetEntries(ent))
            {
                args.PushMarkup(Loc.GetString("custom-radio-server-examine", ("color", entry.Color), ("name", entry.Name), ("key", entry.Key), ("count", entry.Headsets)));
            }
        }
    }

    private void UpdateUi(EntityUid server)
    {
        var state = new CustomRadioServerBoundUserInterfaceState(GetEntries(server));
        _ui.SetUiState(server, CustomRadioServerUiKey.Key, state);
    }

    private List<CustomRadioChannelEntry> GetEntries(EntityUid server)
    {
        var entries = new List<CustomRadioChannelEntry>();
        if (!TryComp<EncryptionKeyHolderComponent>(server, out var holder))
            return entries;

        var mapId = Transform(server).MapID;
        foreach (var id in holder.Channels)
        {
            if (!_channels.IsCustomChannel(id) || !_prototype.TryIndex<RadioChannelPrototype>(id, out var proto))
                continue;

            entries.Add(new CustomRadioChannelEntry(id, proto.LocalizedName, proto.LongKey ?? string.Empty, proto.Color, CountHeadsets(id, mapId)));
        }

        return entries;
    }

    private int CountHeadsets(string channelId, MapId mapId)
    {
        var count = 0;
        var query = EntityQueryEnumerator<HeadsetComponent, EncryptionKeyHolderComponent, TransformComponent>();
        while (query.MoveNext(out _, out var headset, out var holder, out var xform))
        {
            if (headset.IsEquipped && xform.MapID == mapId && holder.Channels.Contains(channelId))
                count++;
        }

        return count;
    }
}