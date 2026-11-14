// SPDX-FileCopyrightText: 2025 Baine Junk <wym0n@proton.me>
// SPDX-FileCopyrightText: 2025 JamboreeBot <JamboreeBot@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Server._EinsteinEngines.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class MindSwappedComponent : Component
    {
        [ViewVariables]
        public EntityUid OriginalEntity = default!;
        [DataField("mindSwapReturnActionId")]
        public ProtoId<EntityPrototype>? MindSwapReturnActionId = "ActionMindSwapReturn";

        [DataField("mindSwapReturnActionEntity")]
        public EntityUid? MindSwapReturnActionEntity;
    }
}
