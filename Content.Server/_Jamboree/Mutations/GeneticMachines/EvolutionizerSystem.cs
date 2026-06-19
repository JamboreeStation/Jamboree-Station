// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Humanoid;
using Content.Server.Medical.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared._Jamboree.Mutations.GeneticMachines;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Polymorph;
using Content.Shared.Preferences;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Jamboree.Mutations.GeneticMachines;

public sealed class EvolutionizerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly GeneticMachineSystem _machine = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string MonkeySpecies = "Monkey";
    private const string KoboldSpecies = "Kobold";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EvolutionizerComponent, EvolutionizerActivateMessage>(OnActivate);
        SubscribeLocalEvent<EvolutionizerComponent, EvolutionizerSetDnaMessage>(OnSetDna);
        SubscribeLocalEvent<EvolutionizerComponent, AfterActivatableUIOpenEvent>((u, c, _) => UpdateUi(u, c));
        SubscribeLocalEvent<EvolutionizerComponent, GotEmaggedEvent>(OnEmagged);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<EvolutionizerComponent, GeneticMachineComponent>();
        while (query.MoveNext(out var uid, out var evo, out var machine))
        {
            if (evo.FinishesAt is not { } finishesAt)
                continue;
            if (_timing.CurTime < finishesAt)
                continue;
            CompleteRun(uid, evo, machine);
        }
    }

    private void OnActivate(EntityUid uid, EvolutionizerComponent component, EvolutionizerActivateMessage args)
    {
        if (!_power.IsPowered(uid))
            return;
        if (component.FinishesAt != null)
            return;
        if (!TryComp<GeneticMachineComponent>(uid, out var machine))
            return;

        _machine.RecheckRange(uid, machine);
        if (_machine.GetScannedBody(uid, machine) is not { } target)
            return;

        // A non-emagged Evolutionizer only operates on diminutive species
        // (monkey/kobold). An emagged one accepts anything since the DNA path
        // can copy arbitrary characters.
        var emagged = HasComp<EmaggedComponent>(uid);
        if (!emagged)
        {
            if (!TryComp<HumanoidAppearanceComponent>(target, out var appearance))
                return;
            if (appearance.Species.Id != MonkeySpecies && appearance.Species.Id != KoboldSpecies)
                return;
        }
        else if (!string.IsNullOrWhiteSpace(component.TargetDna)
                 && !TryFindDnaDonor(component.TargetDna, out _))
        {
            // Geneticist asked for a specific character but nobody matches —
            // refuse the run so they don't burn 10 seconds for a no-op.
            return;
        }

        component.CurrentTarget = target;
        component.StartedAt = _timing.CurTime;
        component.FinishesAt = _timing.CurTime + TimeSpan.FromSeconds(component.ProcessingTime);
        UpdateUi(uid, component);
    }

    private void OnSetDna(EntityUid uid, EvolutionizerComponent component, EvolutionizerSetDnaMessage args)
    {
        component.TargetDna = args.Dna;
        UpdateUi(uid, component);
    }

    /// <summary>
    ///     Accepts an Interaction-type emag. EmagSystem only attaches
    ///     EmaggedComponent if some handler marks the event Handled, so without
    ///     this the cryptographic sequencer would silently fizzle on the console.
    /// </summary>
    private void OnEmagged(Entity<EvolutionizerComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;
        if (_emag.CheckFlag(ent.Owner, EmagType.Interaction))
            return;

        args.Handled = true;
        UpdateUi(ent.Owner, ent.Comp);
    }

    private void CompleteRun(EntityUid uid, EvolutionizerComponent component, GeneticMachineComponent machine)
    {
        var target = component.CurrentTarget;
        component.CurrentTarget = null;
        component.StartedAt = null;
        component.FinishesAt = null;

        var resolved = _machine.GetScannedBody(uid, machine);
        if (target is { } original && resolved == original)
        {
            // Emag path: copy a specific DNA fingerprint onto the new humanoid.
            if (HasComp<EmaggedComponent>(uid) && !string.IsNullOrWhiteSpace(component.TargetDna)
                && TryFindDnaDonor(component.TargetDna, out var donor))
            {
                EvolveAsDnaCopy(uid, original, component, donor);
            }
            else if (TryComp<HumanoidAppearanceComponent>(original, out var appearance))
            {
                EntityUid? evolved = null;
                if (appearance.Species.Id == MonkeySpecies && component.MonkeyPolymorph is { } monkey)
                    evolved = _polymorph.PolymorphEntity(original, monkey);
                else if (appearance.Species.Id == KoboldSpecies && component.KoboldPolymorph is { } kobold)
                    evolved = _polymorph.PolymorphEntity(original, kobold);

                if (evolved is { } evolvedEnt)
                {
                    RandomizeEvolvedAppearance(evolvedEnt);
                    // PolymorphSystem's auto-insert into the original's container fails because the scanner's BodyContainer (a single-slot
                    //   ContainerSlot) still holds the original at insertion time. After the polymorph moves the original to the paused map
                    //   the slot is empty, so re-seat the new humanoid manually.
                    PlaceBackInScanner(uid, evolvedEnt);
                }
            }
        }
        UpdateUi(uid, component);
    }

    /// <summary>
    ///     Emag-DNA branch: spawn a fresh humanoid of the donor's base species via
    ///     a transient PolymorphConfiguration, then overwrite its appearance + name
    ///     so the result is a visual copy of the donor. The donor itself is not
    ///     affected — they're only used as a template.
    /// </summary>
    private void EvolveAsDnaCopy(EntityUid console, EntityUid original, EvolutionizerComponent component,
        Entity<HumanoidAppearanceComponent> donor)
    {
        // Pick a base entity matching the donor's species; fall back to a generic
        // humanoid if the species isn't in the configured map.
        var basePrototype = component.SpeciesBasePrototypes.TryGetValue(donor.Comp.Species, out var mapped)
            ? mapped
            : component.EmagFallbackBase;

        var config = new PolymorphConfiguration
        {
            Entity = basePrototype,
            Forced = true,
            Inventory = PolymorphInventoryChange.Drop,
            // We rewrite name + appearance ourselves below, so keep PolymorphSystem
            // out of those copies and avoid double-handling.
            TransferName = false,
            TransferHumanoidAppearance = false,
            TransferDamage = false,
        };

        if (_polymorph.PolymorphEntity(original, config) is not { } evolved)
            return;

        // Clone the donor's humanoid look + name onto the freshly-spawned body.
        _humanoid.CloneAppearance(donor.Owner, evolved);
        if (TryComp<MetaDataComponent>(donor.Owner, out var donorMeta))
            _metaData.SetEntityName(evolved, donorMeta.EntityName);

        PlaceBackInScanner(console, evolved);
    }

    /// <summary>
    ///     Find a humanoid (non-silicon — silicons don't have HumanoidAppearance)
    ///     carrying the typed DNA fingerprint. Used by the emag path to pick the
    ///     character whose appearance gets cloned onto the new humanoid.
    /// </summary>
    private bool TryFindDnaDonor(string dna, out Entity<HumanoidAppearanceComponent> donor)
    {
        donor = default;
        var needle = dna.Trim();
        var query = EntityQueryEnumerator<DnaComponent, HumanoidAppearanceComponent>();
        while (query.MoveNext(out var ent, out var dnaComp, out var humanoid))
        {
            if (dnaComp.DNA is not { } current
                || !string.Equals(current, needle, StringComparison.OrdinalIgnoreCase))
                continue;
            donor = (ent, humanoid);
            return true;
        }
        return false;
    }

    private void UpdateUi(EntityUid uid, EvolutionizerComponent component)
    {
        if (!_ui.HasUi(uid, EvolutionizerUiKey.Key))
            return;
        if (!TryComp<GeneticMachineComponent>(uid, out var machine))
            return;
        _machine.RecheckRange(uid, machine);
        var status = GetStatus(uid, machine, out var occupantName);
        var emagged = HasComp<EmaggedComponent>(uid);
        var cannotEvolve = false;
        if (!emagged && _machine.GetScannedBody(uid, machine) is { } occupant)
        {
            // Mirror the gate in OnActivate: a non-emagged Evolutionizer only
            // operates on monkeys/kobolds.
            cannotEvolve = !TryComp<HumanoidAppearanceComponent>(occupant, out var appearance)
                || (appearance.Species.Id != MonkeySpecies && appearance.Species.Id != KoboldSpecies);
        }
        // Emag + typed DNA + nobody on the server matches → flag for the UI so
        // the geneticist sees why the activate button is greyed.
        var dnaDonorMissing = emagged
            && !string.IsNullOrWhiteSpace(component.TargetDna)
            && !TryFindDnaDonor(component.TargetDna, out _);
        _ui.SetUiState(uid, EvolutionizerUiKey.Key, new EvolutionizerBoundUserInterfaceState(
            status, occupantName, component.FinishesAt != null,
            emagged, component.TargetDna, cannotEvolve, dnaDonorMissing,
            component.StartedAt, component.FinishesAt));
    }

    /// <summary>
    ///     Drops the freshly-polymorphed humanoid back into the linked scanner so
    ///     the geneticist can immediately keep working on them.
    /// </summary>
    private void PlaceBackInScanner(EntityUid console, EntityUid evolved)
    {
        if (!TryComp<GeneticMachineComponent>(console, out var machine))
            return;
        if (machine.Scanner is not { } scanner)
            return;
        if (!TryComp<MedicalScannerComponent>(scanner, out var scannerComp))
            return;
        _container.Insert(evolved, scannerComp.BodyContainer);
    }

    /// <summary>
    ///     After a monkey/kobold has been polymorphed into a fresh humanoid, roll a
    ///     random appearance + name so the result isn't always Urist McHands.
    /// </summary>
    private void RandomizeEvolvedAppearance(EntityUid evolved)
    {
        if (!TryComp<HumanoidAppearanceComponent>(evolved, out var humanoid))
            return;
        var profile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
        _humanoid.LoadProfile(evolved, profile, humanoid);
        _metaData.SetEntityName(evolved, profile.Name);
    }

    private GeneticMachineScannerStatus GetStatus(EntityUid uid, GeneticMachineComponent machine, out string occupantName)
    {
        occupantName = string.Empty;
        if (machine.Scanner is null)
            return GeneticMachineScannerStatus.NoScannerLinked;
        if (!machine.ScannerInRange)
            return GeneticMachineScannerStatus.ScannerOutOfRange;
        var body = _machine.GetScannedBody(uid, machine);
        if (body is null)
            return GeneticMachineScannerStatus.ScannerEmpty;
        occupantName = MetaData(body.Value).EntityName;
        return GeneticMachineScannerStatus.ScannerOccupied;
    }
}
