using System.Linq;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Medical.Components;
using Content.Shared._Jamboree.Mutations.GeneticMachines;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;

namespace Content.Server._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Maintains the multitool-link between every Jamboree genetic console
///     (<see cref="GeneticMachineComponent"/>) and a vanilla MedicalScanner. Each console
///     exposes a single <c>MedicalScannerSender</c> source port, which lets a Geneticist
///     wire a scanner in with a multitool exactly like a CloningConsole.
/// </summary>
public sealed class GeneticMachineSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeneticMachineComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GeneticMachineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GeneticMachineComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<GeneticMachineComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<GeneticMachineComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnInit(EntityUid uid, GeneticMachineComponent component, ComponentInit args)
    {
        _signalSystem.EnsureSourcePorts(uid, GeneticMachineComponent.ScannerPort);
    }

    private void OnMapInit(EntityUid uid, GeneticMachineComponent component, MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
            return;

        foreach (var port in source.Outputs.Values.SelectMany(ports => ports))
        {
            if (HasComp<MedicalScannerComponent>(port))
            {
                component.Scanner = port;
                break;
            }
        }
        RecheckRange(uid, component);
    }

    private void OnNewLink(EntityUid uid, GeneticMachineComponent component, NewLinkEvent args)
    {
        if (args.SourcePort != GeneticMachineComponent.ScannerPort)
            return;
        if (!HasComp<MedicalScannerComponent>(args.Sink))
            return;

        component.Scanner = args.Sink;
        RecheckRange(uid, component);
    }

    private void OnPortDisconnected(EntityUid uid, GeneticMachineComponent component, PortDisconnectedEvent args)
    {
        if (args.Port == GeneticMachineComponent.ScannerPort)
            component.Scanner = null;
    }

    private void OnAnchorChanged(EntityUid uid, GeneticMachineComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            RecheckRange(uid, component);
    }

    /// <summary>
    ///     Re-checks distance between console and linked scanner.
    /// </summary>
    public void RecheckRange(EntityUid console, GeneticMachineComponent? comp = null)
    {
        if (!Resolve(console, ref comp))
            return;
        if (comp.Scanner is not { } scanner)
        {
            comp.ScannerInRange = false;
            return;
        }

        Transform(scanner).Coordinates.TryDistance(EntityManager, Transform(console).Coordinates, out var dist);
        comp.ScannerInRange = dist <= comp.MaxDistance;
    }

    /// <summary>
    ///     Resolves the creature currently sealed inside the linked medical scanner, or null.
    /// </summary>
    public EntityUid? GetScannedBody(EntityUid console, GeneticMachineComponent? comp = null)
    {
        if (!Resolve(console, ref comp))
            return null;
        if (comp.Scanner is not { } scanner)
            return null;
        if (!comp.ScannerInRange)
            return null;
        if (!TryComp<MedicalScannerComponent>(scanner, out var scannerComp))
            return null;
        return scannerComp.BodyContainer.ContainedEntity;
    }
}
