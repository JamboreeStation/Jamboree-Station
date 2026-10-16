// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Content.Shared._Jamboree.CustomRadio;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server._Jamboree.CustomRadio;

/// <summary>
/// So, this creates, edits and removes the radio channels that players can make during a live round.
/// </summary>
public sealed class CustomRadioChannelSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public const int MaxChannels = 20;
    public const int MaxNameLength = 24;
    public const int MinKeyLength = 2; // default legacy radio prefixes are 1
    public const int MaxKeyLength = 6;
    public const float MinBrightness = 0.35f; // to make it readable in chat
    private const int BaseFrequency = 1600;

    private static readonly Regex NameRegex = new("^[A-Za-z0-9 '-]+$", RegexOptions.Compiled);
    private static readonly Regex KeyRegex = new("^[a-z]+$", RegexOptions.Compiled);

    private readonly Dictionary<string, CustomChannel> _channels = new();
    private int _nextId;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public bool IsCustomChannel(string channelId)
    {
        return _channels.ContainsKey(channelId);
    }

    public bool TryCreateChannel(EntityUid user, string name, string key, Color color, [NotNullWhen(true)] out string? channelId, out string? error)
    {
        channelId = null;

        if (_channels.Count >= MaxChannels)
        {
            error = "custom-radio-error-too-many";
            return false;
        }

        if (!TryValidate(ref name, ref key, color, null, out error))
            return false;

        _nextId++;
        channelId = $"JamboreeCustom{_nextId}";
        LoadChannel(channelId, name, key, color, BaseFrequency + _nextId);

        _adminLogger.Add(LogType.ItemConfigure, LogImpact.Low, $"{ToPrettyString(user):user} created a custom radio channel {channelId}: {name} (:#{key})");
        return true;
    }

    public bool TryEditChannel(EntityUid user, string channelId, string name, string key, Color color, out string? error)
    {
        if (!_channels.TryGetValue(channelId, out var channel))
        {
            error = "custom-radio-error-unknown";
            return false;
        }

        if (!TryValidate(ref name, ref key, color, channelId, out error))
            return false;

        LoadChannel(channelId, name, key, color, channel.Frequency);

        _adminLogger.Add(LogType.ItemConfigure, LogImpact.Low, $"{ToPrettyString(user):user} edited a custom radio channel {channelId}: {name} (:#{key})");
        return true;
    }

    private bool TryValidate(ref string name, ref string key, Color color, string? editingId, out string? error)
    {
        name = name.Trim();
        key = key.Trim().ToLowerInvariant();
        error = null;

        if (name.Length > MaxNameLength || !NameRegex.IsMatch(name))
            error = "custom-radio-error-name";
        else if (_loc.HasString(name) || IsNameTaken(name, editingId))
            error = "custom-radio-error-name-taken";
        else if (key.Length < MinKeyLength || key.Length > MaxKeyLength || !KeyRegex.IsMatch(key))
            error = "custom-radio-error-key";
        else if (IsKeyTaken(key, editingId))
            error = "custom-radio-error-key-taken";
        else if (0.299f * color.R + 0.587f * color.G + 0.114f * color.B < MinBrightness)
            error = "custom-radio-error-color";

        return error == null;
    }

    private bool IsNameTaken(string name, string? editingId)
    {
        foreach (var proto in _prototype.EnumeratePrototypes<RadioChannelPrototype>())
        {
            if (proto.ID != editingId && string.Equals(proto.LocalizedName, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private bool IsKeyTaken(string key, string? editingId)
    {
        foreach (var proto in _prototype.EnumeratePrototypes<RadioChannelPrototype>())
        {
            if (proto.ID != editingId && string.Equals(proto.LongKey, key, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void LoadChannel(string channelId, string name, string key, Color color, int frequency)
    {
        var yaml = $"""
            - type: radioChannel
              id: {channelId}
              name: "{name}"
              longKey: "{key}"
              frequency: {frequency}
              color: "{color.ToHexNoAlpha()}"
            """;

        var changed = new Dictionary<Type, HashSet<string>>();
        _prototype.LoadString(yaml, true, changed);
        _prototype.ReloadPrototypes(changed);

        _channels[channelId] = new CustomChannel(frequency, yaml);
        RaiseNetworkEvent(GetSyncEvent());
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        if (_channels.Count == 0)
            return;

        foreach (var channel in _channels.Values)
        {
            _prototype.RemoveString(channel.Yaml);
        }

        var removed = new Dictionary<Type, HashSet<string>>
        {
            [typeof(RadioChannelPrototype)] = _channels.Keys.ToHashSet(),
        };

        _channels.Clear();
        _prototype.ReloadPrototypes(new Dictionary<Type, HashSet<string>>(), removed);
        RaiseNetworkEvent(GetSyncEvent());
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.InGame)
            RaiseNetworkEvent(GetSyncEvent(), e.Session);
    }

    private CustomRadioChannelsSyncEvent GetSyncEvent()
    {
        return new CustomRadioChannelsSyncEvent(_channels.ToDictionary(c => c.Key, c => c.Value.Yaml));
    }

    private sealed record CustomChannel(int Frequency, string Yaml);
}