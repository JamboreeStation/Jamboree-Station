// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._RMC14.LinkAccount;
using Robust.Shared.Network;

namespace Content.Client._RMC14.LinkAccount;

public sealed class LinkAccountManager : IPostInjectInit
{
    [Dependency] private readonly INetManager _net = default!;

    public bool Linked { get; private set; }

    public event Action<Guid>? CodeReceived;

    private void OnCode(LinkAccountCodeMsg message)
    {
        CodeReceived?.Invoke(message.Code);
    }

    private void OnStatus(LinkAccountStatusMsg ev)
    {
        Linked = ev.Linked;
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<LinkAccountCodeMsg>(OnCode);
        _net.RegisterNetMessage<LinkAccountRequestMsg>();
        _net.RegisterNetMessage<LinkAccountStatusMsg>(OnStatus);
    }
}
