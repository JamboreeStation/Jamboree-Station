// SPDX-FileCopyrightText: 2026 PureBreadBagel
// SPDX-License-Identifier: MIT

using Content.Shared.DeviceLinking;
using Content.Shared.Materials;
using Robust.Shared.Prototypes;

namespace Content.Server._Jamboree.DnaCloner.Components
{

    [RegisterComponent]
    public sealed partial class CentralCommandDnaClonerVatComponent : Component
    {
        [DataField]
        public ProtoId<SinkPortPrototype> VatPort = "CentralCommandDnaClonerReceiver";

        [DataField]
        public string ContainerId = "centralcommand-dnaclone-container";

        /// <summary>
        /// The material fed into the machine to synthesise a body.
        /// </summary>
        [DataField]
        public ProtoId<MaterialPrototype> RequiredMaterial = "Biomass";

        [DataField]
        public int MaterialCost = 70;

        [ViewVariables]
        public EntityUid? ConnectedConsole;
    }
}
