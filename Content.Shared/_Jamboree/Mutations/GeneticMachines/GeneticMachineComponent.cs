using Robust.Shared.GameStates;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Base component for Jamboree's genetic console machines. Each console links to an
///     ordinary <c>MedicalScannerComponent</c> via the existing <c>MedicalScannerSender</c>
///     device-link source port, then operates on whoever is sealed inside the scanner.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GeneticMachineComponent : Component
{
    public const string ScannerPort = "MedicalScannerSender";

    /// <summary>
    ///     The medical scanner this console is currently linked to (server-side only).
    /// </summary>
    [ViewVariables]
    public EntityUid? Scanner;

    /// <summary>
    ///     Maximum distance allowed between this console and its linked scanner.
    /// </summary>
    [DataField]
    public float MaxDistance = 4f;

    [ViewVariables]
    public bool ScannerInRange = true;
}
