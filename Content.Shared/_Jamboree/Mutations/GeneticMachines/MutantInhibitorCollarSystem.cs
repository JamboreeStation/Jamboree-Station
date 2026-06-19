using Content.Shared.Abilities.Psionics;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

public sealed class MutantInhibitorCollarSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MutantInhibitorCollarComponent, BeingUnequippedAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<MutantInhibitorCollarComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<MutantInhibitorCollarComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<MutantSuppressedComponent, OnAttemptPowerUseEvent>(OnAttemptPower);
    }

    private void OnEquipped(EntityUid uid, MutantInhibitorCollarComponent component, GotEquippedEvent args)
    {
        EnsureComp<MutantSuppressedComponent>(args.Equipee);
    }

    private void OnUnequipped(EntityUid uid, MutantInhibitorCollarComponent component, GotUnequippedEvent args)
    {
        RemComp<MutantSuppressedComponent>(args.Equipee);
    }

    private void OnUnequipAttempt(EntityUid uid, MutantInhibitorCollarComponent component, BeingUnequippedAttemptEvent args)
    {
        if (args.UnEquipTarget != args.Unequipee)
            return;
        // Wearer trying to take it off themselves — denied like an electropack.
        args.Cancel();
        args.Reason = Loc.GetString("inhibitor-collar-cant-self-remove");
    }

    private void OnAttemptPower(EntityUid uid, MutantSuppressedComponent component, OnAttemptPowerUseEvent args)
    {
        args.Cancel();
        _popup.PopupClient(Loc.GetString("inhibitor-collar-suppressed"), uid, uid);
    }
}
