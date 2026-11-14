// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Power.Components;

namespace Content.Client.Power.Components;

[RegisterComponent]
public sealed partial class ApcPowerReceiverComponent : SharedApcPowerReceiverComponent
{
    // Prototype-default states may be omitted from the initial network update.
    [DataField("needsPower")]
    public override bool NeedsPower { get; set; } = true;

    [DataField("powerDisabled")]
    public override bool PowerDisabled { get; set; }

    public override float Load { get; set; }
}
