// SPDX-FileCopyrightText: 2026 Goob Station Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Lobby;

namespace Content.Goobstation.Client.Lobby;

public sealed partial class LobbyRoundInfoSystem : EntitySystem
{
    public LobbyRoundInfoEvent? Info { get; private set; }

    public event Action<LobbyRoundInfoEvent>? Updated;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<LobbyRoundInfoEvent>(OnRoundInfo);
    }

    private void OnRoundInfo(LobbyRoundInfoEvent ev)
    {
        Info = ev;
        Updated?.Invoke(ev);
    }
}
