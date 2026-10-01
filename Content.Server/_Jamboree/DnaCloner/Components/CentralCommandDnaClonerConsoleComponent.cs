// SPDX-FileCopyrightText: 2022 Rane <60792108+Elijahrane@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 fishfish458 <fishfish458>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2026 PureBreadBagel
//
// SPDX-License-Identifier: AGPL-3.0-or-later



namespace Content.Server._Jamboree.DnaCloner.Components {
    [RegisterComponent]
    public sealed partial class CentralCommandDnaClonerConsoleComponent : Component
    {

        public const string PodPort = "CentralCommandDnaClonerSender";

        [ViewVariables]

        public EntityUid? DnaClonerVat = null; // Multitool link container. Need to have a vat right?

        [ViewVariables]

        public EntityUid? SelectedMind = null; // What mind being synthesised is getting unfortunatley made back to work lmao.


        [DataField("maxDistance")]
        public float MaxDistance = 4f; // 4 tiles...the max distance you can link it.

        [DataField("rottenBodyStage")]
        public int RottenBodyStage = 0; // Jam -  I mean someones gotta track the rotting right?

        public bool DnaClonerVatInRange = true;
    }
}
