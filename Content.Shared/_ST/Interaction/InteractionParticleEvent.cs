// SPDX-FileCopyrightText: 2026 Janet Blackquill <uhhadd@gmail.com>
// SPDX-FileCopyrightText: 2026 mirrorcult <mirrorcult@pm.me>
//
// SPDX-License-Identifier: MIT
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._ST.Interaction;

/// <summary>
/// Data for interaction particles
/// </summary>
[Serializable, NetSerializable]
public sealed class StellarInteractionParticleEvent(NetEntity performer, NetEntity? used, NetEntity target, bool isClientEvent, StellarInteractionParticleType type) : EntityEventArgs
{
    public NetEntity Performer = performer;

    public NetEntity? Used = used;

    public NetEntity Target = target;

    /// <summary>
    /// Workaround for event subscription not working w/ the session overload
    /// </summary>
    public bool IsClientEvent = isClientEvent;

    public StellarInteractionParticleType Type = type;
}

[Serializable, NetSerializable]
public enum StellarInteractionParticleType
{
    Use,
    Pull,
    InHand,
}
