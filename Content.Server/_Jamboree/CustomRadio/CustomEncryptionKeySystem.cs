// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Jamboree.CustomRadio;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._Jamboree.CustomRadio;

/// <summary>
/// This lets players set up a blank encryption key with a multitool.
/// </summary>
public sealed class CustomEncryptionKeySystem : EntitySystem
{
    [Dependency] private readonly CustomRadioChannelSystem _channels = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CustomEncryptionKeyComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CustomEncryptionKeyComponent, CustomEncryptionKeySetupMessage>(OnSetup);
    }

    private void OnInteractUsing(Entity<CustomEncryptionKeyComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<CustomEncryptionKeyComponent>(args.Used, out var source) && source.ChannelId != null)
        {
            args.Handled = true;
            CopyKey((args.Used, source), ent, args.User);
            return;
        }

        if (!_tool.HasQuality(args.Used, ent.Comp.QualityNeeded))
            return;

        args.Handled = true;

        if (ent.Comp.ChannelId != null)
        {
            _popup.PopupEntity(Loc.GetString("custom-key-already-set-up"), ent, args.User);
            return;
        }

        _ui.OpenUi(ent.Owner, CustomEncryptionKeyUiKey.Key, args.User);
    }

    private void OnSetup(Entity<CustomEncryptionKeyComponent> ent, ref CustomEncryptionKeySetupMessage args)
    {
        if (ent.Comp.ChannelId != null || !TryComp<EncryptionKeyComponent>(ent, out var key))
            return;

        if (!_channels.TryCreateChannel(args.Actor, args.Name, args.Key, args.Color, out var channelId, out var error))
        {
            _popup.PopupEntity(Loc.GetString(error!), ent, args.Actor);
            return;
        }

        SetChannel(ent, key, channelId);

        _ui.CloseUi(ent.Owner, CustomEncryptionKeyUiKey.Key);
        _popup.PopupEntity(Loc.GetString("custom-key-set-up", ("name", args.Name.Trim()), ("key", args.Key.Trim().ToLowerInvariant())), ent, args.Actor);
    }

    public void UpdateKey(Entity<CustomEncryptionKeyComponent> ent)
    {
        if (ent.Comp.ChannelId == null || !_prototype.TryIndex<RadioChannelPrototype>(ent.Comp.ChannelId, out var channel))
            return;

        _appearance.SetData(ent, CustomEncryptionKeyVisuals.Color, channel.Color);
        _metaData.SetEntityName(ent, Loc.GetString("custom-key-name", ("name", channel.LocalizedName)));
        _metaData.SetEntityDescription(ent, Loc.GetString("custom-key-desc"));
    }

    private void CopyKey(Entity<CustomEncryptionKeyComponent> source, Entity<CustomEncryptionKeyComponent> target, EntityUid user)
    {
        if (target.Comp.ChannelId != null || !TryComp<EncryptionKeyComponent>(target, out var key))
        {
            _popup.PopupEntity(Loc.GetString("custom-key-copy-not-blank"), target, user);
            return;
        }

        SetChannel(target, key, source.Comp.ChannelId!);
        _popup.PopupEntity(Loc.GetString("custom-key-copied"), target, user);
    }

    private void SetChannel(Entity<CustomEncryptionKeyComponent> ent, EncryptionKeyComponent key, string channelId)
    {
        ent.Comp.ChannelId = channelId;
        key.Channels.Clear();
        key.Channels.Add(channelId);
        UpdateKey(ent);
    }
}