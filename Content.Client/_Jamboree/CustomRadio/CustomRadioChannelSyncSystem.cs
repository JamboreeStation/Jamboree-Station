// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Jamboree.CustomRadio;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Client._Jamboree.CustomRadio;

/// <summary>
/// This loads the server's custom radio channels so that the chat box can show their names.
/// </summary>
public sealed class CustomRadioChannelSyncSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly Dictionary<string, string> _loaded = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CustomRadioChannelsSyncEvent>(OnSync);
    }

    private void OnSync(CustomRadioChannelsSyncEvent ev)
    {
        foreach (var yaml in _loaded.Values)
        {
            _prototype.RemoveString(yaml);
        }

        var removed = _loaded.Keys.Where(id => !ev.Channels.ContainsKey(id)).ToHashSet();
        _loaded.Clear();

        var changed = new Dictionary<Type, HashSet<string>>();
        foreach (var (id, yaml) in ev.Channels)
        {
            _prototype.LoadString(yaml, true, changed);
            _loaded[id] = yaml;
        }

        _prototype.ReloadPrototypes(changed, new Dictionary<Type, HashSet<string>>
        {
            [typeof(RadioChannelPrototype)] = removed,
        });
    }
}