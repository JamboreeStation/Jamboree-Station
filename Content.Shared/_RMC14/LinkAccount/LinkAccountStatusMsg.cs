// SPDX-License-Identifier: AGPL-3.0-or-later

using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.LinkAccount;

public sealed class LinkAccountStatusMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public bool Linked;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Linked = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Linked);
    }
}
