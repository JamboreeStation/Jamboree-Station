using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Power.EntitySystems;
using Content.Shared._Jamboree.Mutations;
using Content.Shared._Jamboree.Mutations.GeneticMachines;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Jamboree.Mutations.GeneticMachines;

public sealed class MutatorSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly GeneticMachineSystem _machine = default!;
    [Dependency] private readonly PotentialMutantSystem _potentialMutant = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MutatorComponent, MutatorActivateMessage>(OnActivate);
        SubscribeLocalEvent<MutatorComponent, AfterActivatableUIOpenEvent>((u, c, _) => UpdateUi(u, c));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MutatorComponent, GeneticMachineComponent>();
        while (query.MoveNext(out var uid, out var mutator, out var machine))
        {
            if (mutator.FinishesAt is not { } finishesAt)
                continue;
            if (_timing.CurTime < finishesAt)
                continue;

            CompleteRun(uid, mutator, machine);
        }
    }

    private void OnActivate(EntityUid uid, MutatorComponent component, MutatorActivateMessage args)
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

        // Mutator refuses creatures that are already carrying mutations — the
        // doc treats this machine as bootstrapping fresh mutants, not stacking
        // onto existing ones.
        if (HasComp<MutantComponent>(target))
            return;

        component.CurrentTarget = target;
        component.StartedAt = _timing.CurTime;
        component.FinishesAt = _timing.CurTime + TimeSpan.FromSeconds(component.ProcessingTime);
        UpdateUi(uid, component);
    }

    private void CompleteRun(EntityUid uid, MutatorComponent component, GeneticMachineComponent machine)
    {
        var target = component.CurrentTarget;
        component.CurrentTarget = null;
        component.StartedAt = null;
        component.FinishesAt = null;

        // Re-resolve the body from the scanner to honour anyone leaving mid-process.
        var resolved = _machine.GetScannedBody(uid, machine);
        if (target is { } original && resolved == original && TryComp<PotentialMutantComponent>(original, out var potential))
        {
            // If something gave them mutations during the run (e.g. another
            // machine), respect the "no stacking" rule.
            if (!HasComp<MutantComponent>(original))
            {
                var count = HasComp<HumanoidAppearanceComponent>(original)
                    ? component.HumanoidMutationCount
                    : component.SimpleMobMutationCount;
                for (var i = 0; i < count; i++)
                    _potentialMutant.TryGainRandomMutation((original, potential));

                // Insignificant Cellular hit per the design doc; mutation isn't free.
                if (component.GeneticDamageOnUse > 0
                    && _proto.TryIndex<DamageTypePrototype>("Cellular", out var cellular))
                {
                    var damage = new DamageSpecifier(cellular, FixedPoint2.New(component.GeneticDamageOnUse));
                    _damageable.TryChangeDamage(original, damage, true);
                }
            }
        }
        UpdateUi(uid, component);
    }

    private void UpdateUi(EntityUid uid, MutatorComponent component)
    {
        if (!_ui.HasUi(uid, MutatorUiKey.Key))
            return;
        if (!TryComp<GeneticMachineComponent>(uid, out var machine))
            return;

        _machine.RecheckRange(uid, machine);
        var status = GetStatus(uid, machine, out var occupantName);
        var alreadyMutated = false;
        if (_machine.GetScannedBody(uid, machine) is { } occupant)
            alreadyMutated = HasComp<MutantComponent>(occupant);
        _ui.SetUiState(uid, MutatorUiKey.Key, new MutatorBoundUserInterfaceState(
            status, occupantName, component.FinishesAt != null,
            alreadyMutated, component.StartedAt, component.FinishesAt));
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
