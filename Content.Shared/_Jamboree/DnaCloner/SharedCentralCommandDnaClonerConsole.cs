// SPDX-FileCopyrightText: 2026 PureBreadBagel
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Jamboree.DnaCloner
{
    [Serializable, NetSerializable]
    public sealed record CentralCommandDnaClonerMindRow(NetEntity Id, string Name, CentralCommandDnaClonerBodyState BodyState);

    [Serializable, NetSerializable]
    public enum CentralCommandDnaClonerBodyState : byte
    {
        // Just basically means if they are alive and posessing body.
        Intact,

        // The corpse is gone and the mind cant get back into its body lol.
        Lost,


        // A corpse is still lying there but the whole point is that if its rotting isnt it destroyed?

        Rotted,
    }

    [Serializable, NetSerializable]
    public class CentralCommandDnaClonerConsoleBoundUserInterfaceState : BoundUserInterfaceState
    {
        public List<CentralCommandDnaClonerMindRow> MindNames { get; set; } = new();
        public CentralCommandDnaClonerStatus Status { get; set; } = CentralCommandDnaClonerStatus.NoVat;

        // How much biomass the machine has in right now????
        public int Biomass { get; set; }

        //How much biomass one synthesis costs.
        public int BiomassCost { get; set; }
    }

    [Serializable, NetSerializable]
    public enum CentralCommandDnaClonerVisuals : byte
    {
        // Whether the vat currently has a body inside it. Would be weird if you just had more than 3.

        Status
    }


    [Serializable, NetSerializable]
    public enum CentralCommandDnaClonerVatStatus : byte
    {
        Idle,
        Occupied
    }

    [Serializable, NetSerializable]
    public enum CentralCommandDnaClonerStatus : byte
    {
        // The machine is linked, in range, fed and free. Go ahead.
        Ready,

        // No molecularsynthesis machine is wired to this console...Kinda cringy.
        NoVat,

        // A machine is wired, but it is too far away to use.
        VatOutOfRange,

        // Something is still GETTING MADE  inside the machine.
        VatOccupied,

        // The machine does not have enough biomass for another body.
        InsufficientBiomass,
    }

    [Serializable, NetSerializable]
    public enum CentralCommandDnaClonerUiKey : byte
    {
        Key

    }


    [Serializable, NetSerializable]
    public sealed class CentralCommandDnaClonerRespawnMessage : BoundUserInterfaceMessage
    {

        public readonly NetEntity Mind;

        public CentralCommandDnaClonerRespawnMessage(NetEntity mind)
        {
            Mind = mind;
        }
    }

}
