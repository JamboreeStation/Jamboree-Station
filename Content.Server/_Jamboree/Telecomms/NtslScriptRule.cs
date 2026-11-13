// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.StationEvents.Events;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Radio.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Jamboree.Telecomms;

/// <summary>
/// Puts an NTSL script on the station's telecom servers, like a virus or a filter from Central Command.
/// The crew can see and clear it with a traffic control console.
/// </summary>
public sealed class NtslScriptRule : StationEventSystem<NtslScriptRuleComponent>
{
    [Dependency] private readonly NtslTelecomSystem _ntsl = default!;

    private static readonly TimeSpan RemovalCheckDelay = TimeSpan.FromSeconds(5);

    protected override void Started(EntityUid uid, NtslScriptRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (comp.Scripts.Count == 0 || !TryGetRandomStation(out var station))
            return;

        var servers = new List<EntityUid>();
        var query = EntityQueryEnumerator<TelecomServerComponent>();
        while (query.MoveNext(out var server, out _))
        {
            if (StationSystem.GetOwningStation(server) == station)
                servers.Add(server);
        }

        if (servers.Count == 0)
            return;

        if (!comp.AllServers)
            servers = new List<EntityUid> { RobustRandom.Pick(servers) };

        comp.Script = comp.Header + RobustRandom.Pick(comp.Scripts);
        foreach (var server in servers)
        {
            var script = EnsureComp<NtslServerComponent>(server);
            var backup = new NtslScriptBackup(script.Code, script.Enabled);

            if (!_ntsl.TryInstall(server, comp.Script, out var error))
            {
                Log.Error($"{ToPrettyString(uid)} could not install its NTSL script: {error}");
                return;
            }

            script.Enabled = true;
            comp.Replaced[server] = backup;
            AdminLogManager.Add(LogType.EventRan, LogImpact.Medium, $"{ToPrettyString(uid)} installed an NTSL script on {ToPrettyString(server):server}");
        }
    }

    protected override void ActiveTick(EntityUid uid, NtslScriptRuleComponent comp, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, comp, gameRule, frameTime);

        if (comp.EarlyRemovalAnnouncement is not { } announcement
            || comp.EarlyRemovalNoted
            || Timing.CurTime < comp.NextRemovalCheck)
        {
            return;
        }

        comp.NextRemovalCheck = Timing.CurTime + RemovalCheckDelay;
        foreach (var server in comp.Replaced.Keys)
        {
            if (TryComp<NtslServerComponent>(server, out var script) && script.Code == comp.Script && script.Enabled)
                continue;

            comp.EarlyRemovalNoted = true;
            var players = Filter.Empty().AddWhere(GameTicker.UserHasJoinedGame);
            ChatSystem.DispatchFilteredAnnouncement(players, Loc.GetString(announcement), colorOverride: Color.Gold);
            AdminLogManager.Add(LogType.EventRan, LogImpact.Low, $"The crew removed the NTSL script of {ToPrettyString(uid)} early from {ToPrettyString(server):server}");
            return;
        }
    }

    protected override void Ended(EntityUid uid, NtslScriptRuleComponent comp, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, comp, gameRule, args);

        if (!comp.Restore)
            return;

        foreach (var (server, backup) in comp.Replaced)
        {
            // The crew already changed it, so their script stays.
            if (!TryComp<NtslServerComponent>(server, out var script) || script.Code != comp.Script)
                continue;

            if (backup.Code.Length == 0 || !_ntsl.TryInstall(server, backup.Code, out _))
                _ntsl.Clear(server, null);

            script.Enabled = backup.Enabled;
        }

        comp.Replaced.Clear();
    }
}
