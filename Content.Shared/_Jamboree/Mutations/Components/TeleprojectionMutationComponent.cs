// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Jamboree.Abilities.Mutations
{
    [RegisterComponent]
    public sealed partial class TeleprojectionMutationComponent : Component
    {
        [DataField("prototype")]
        public string Prototype = "MobTelegnosisObserver";
    }
}
