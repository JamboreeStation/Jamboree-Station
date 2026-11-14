// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RMC14.LinkAccount;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.LinkAccount;

public sealed class LinkAccountManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private readonly Dictionary<NetUserId, TimeSpan> _lastRequest = new();
    private readonly TimeSpan _minimumWait = TimeSpan.FromSeconds(0.5);
    private readonly Dictionary<NetUserId, bool> _connected = new();

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        var linked = await _db.HasLinkedAccount(player.UserId, cancel);
        cancel.ThrowIfCancellationRequested();

        _connected[player.UserId] = linked;
    }

    private void FinishLoad(ICommonSession player)
    {
        SendStatus(player);
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _connected.Remove(player.UserId);
    }

    private void SendStatus(ICommonSession player)
    {
        var msg = new LinkAccountStatusMsg { Linked = _connected.GetValueOrDefault(player.UserId) };
        _net.ServerSendMessage(msg, player.Channel);
    }

    private void OnRequest(LinkAccountRequestMsg message)
    {
        var user = message.MsgChannel.UserId;
        var time = _timing.RealTime;
        if (_lastRequest.TryGetValue(user, out var last) &&
            last + _minimumWait > time)
        {
            return;
        }

        _lastRequest[user] = time;

        var code = Guid.NewGuid();
        _db.SetLinkingCode(user, code);

        var response = new LinkAccountCodeMsg { Code = code };
        _net.ServerSendMessage(response, message.MsgChannel);
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<LinkAccountRequestMsg>(OnRequest);
        _net.RegisterNetMessage<LinkAccountCodeMsg>();
        _net.RegisterNetMessage<LinkAccountStatusMsg>();
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
