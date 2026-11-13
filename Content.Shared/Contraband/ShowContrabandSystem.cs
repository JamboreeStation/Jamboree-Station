// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Content.Shared.Inventory;

namespace Content.Shared.Contraband;

public sealed partial class ShowContrabandSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.SubscribeWithRelay<ShowContrabandDetailsComponent, GetContrabandDetailsEvent>(OnGetContrabandDetails);

    }

    private void OnGetContrabandDetails(Entity<ShowContrabandDetailsComponent> ent, ref GetContrabandDetailsEvent args)
    {
        args.CanShowContraband = true;
    }
}

/// <summary>
/// Raised on an entity and its inventory to determine if it can see contraband information in the examination window.
/// </summary>
[ByRefEvent]
public record struct GetContrabandDetailsEvent(bool CanShowContraband = false) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.EYES;
}
