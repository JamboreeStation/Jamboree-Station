// SPDX-FileCopyrightText: 2024 Kill_Me_I_Noobs <118206719+Vonsant@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Kill_Me_I_Noobs <118206719+vonsant@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 JamboreeBot <JamboreeBot@proton.me>
// SPDX-FileCopyrightText: 2026 PureBreadBagel <PureBreadBagel@no=reply.github.com>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Runtime.InteropServices;
using Content.Shared._NF.Respawn;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._NF.Respawn;

public sealed class RespawnSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<ICommonSession, TimeSpan> _respawnResetTimes = [];





    public override void Initialize() // when the game starts
    {
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged); // MobStateChangedEvent is raised when a mob's state changes, such as when it dies or is revived.
        SubscribeLocalEvent<MindContainerComponent, MindRemovedMessage>(OnMindRemoved); // MindRemovedMessage is raised when a mind is removed from an entity, such as when a player disconnects or is killed.
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup); // RoundRestartCleanupEvent is raised when the round restarts, such as when the server is restarted or the round ends.

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }



    private void OnMobStateChanged(MobStateChangedEvent e)
    {
        // Jam, Find the player controlling the entity state being changed.
        if (!_player.TryGetSessionByEntity(e.Target, out var session)) // failsafe for if session not found. The session is the players client.
            return;

        // Respawn timer should start when the player is dead.
        if (e.NewMobState == MobState.Dead)
        {
            ResetRespawnTime(session); // e.Target is the entity that changed state.
            return;
        }

        if (e.NewMobState == MobState.Alive)
        {
            // If player is no longer dead, clear the respawn timer.
            ClearRespawnTime(session);
        }
    }

    private void ClearRespawnTime(ICommonSession session)
    {
        // Jam, reset the respawn timer for the player, and send a network event to the client to clear the respawn timer.
        if (_respawnResetTimes.Remove(session))
            SendRespawnResetTime(session, null);
    }

    private void OnMindRemoved(EntityUid entity, MindContainerComponent component, MindRemovedMessage e)
    {
        if (e.Mind.Comp.UserId is null)
            return;

        if (TryComp<MobStateComponent>(entity, out var state) && state.CurrentState == MobState.Dead)
            return;

        if (!_player.TryGetSessionById(e.Mind.Comp.UserId.Value, out var session))
            return;

        if (_respawnResetTimes.ContainsKey(session))
            return;

        ResetRespawnTime(session);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent e)
    {
        _respawnResetTimes.Clear(); // Clear literally everyones respawn timer when the round restarts, duh.
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == Robust.Shared.Enums.SessionStatus.Connected)
            SendRespawnResetTime(e.Session, GetRespawnResetTime(e.Session)); // If player reconnects, get their OG respawn timer.
    }

    private void ResetRespawnTime(ICommonSession session)
    {
        ref var respawnTime = ref CollectionsMarshal.GetValueRefOrAddDefault(_respawnResetTimes, session, out _);

        respawnTime = _timing.CurTime;

        SendRespawnResetTime(session, _timing.CurTime);
    }

    private void SendRespawnResetTime(ICommonSession session, TimeSpan? time)
    {
        RaiseNetworkEvent(new RespawnResetEvent(time), session); // Sends a network thing to the server to tell the client what the respawn timer is. The client will then display the respawn timer to the player.
    }

    public TimeSpan? GetRespawnResetTime(ICommonSession session)
    {
        return _respawnResetTimes.TryGetValue(session, out var time) ? time : null;
    }
}
