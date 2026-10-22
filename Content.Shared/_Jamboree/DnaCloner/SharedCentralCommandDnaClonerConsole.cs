// SPDX-FileCopyrightText: 2026 PureBreadBagel
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Jamboree.DnaCloner
{
    [Serializable, NetSerializable]
    public sealed record CentralCommandDnaClonerMindRow(NetEntity Id, string Name, CentralCommandDnaClonerBodyState BodyState, bool IsObserver);

    [Serializable, NetSerializable]
    public enum CentralCommandDnaClonerBodyState : byte
    {
        // They still have a body, or lost it in a way the UI doesn't need to distinguish.
        Intact,

        // The corpse is gone and the mind can't get back into it.
        Lost,


        // A corpse is still lying there, but rotting. Worth flagging, because they're not getting that one back.

        Rotted,
    }

    [Serializable, NetSerializable]
    public class CentralCommandDnaClonerConsoleBoundUserInterfaceState : BoundUserInterfaceState
    {
        public List<CentralCommandDnaClonerMindRow> MindNames { get; set; } = new();
        public CentralCommandDnaClonerStatus Status { get; set; } = CentralCommandDnaClonerStatus.NoVat;

        // How much biomass the machine currently has.
        public int Biomass { get; set; }

        // How much one synthesis costs, so the client can render "have / need" without guessing.
        public int BiomassCost { get; set; }
    }

    [Serializable, NetSerializable]
    public enum CentralCommandDnaClonerVisuals : byte
    {
        // Whether the vat currently has a body in it. Two states, because the vat holds one body at a time.

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

        // No molecularsynthesis machine has been linked with a multitool or linker.
        NoVat,

        // A machine is wired, but it is too far away to use.
        VatOutOfRange,

        // A body is still being synthesised inside the machine.
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
