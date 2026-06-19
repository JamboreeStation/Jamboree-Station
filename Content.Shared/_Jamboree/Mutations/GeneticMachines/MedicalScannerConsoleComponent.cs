using Robust.Shared.GameStates;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Console that pipes a linked medical scanner's occupant into the engine's
///     HealthAnalyzer UI. Pairs with <see cref="GeneticMachineComponent"/> for the
///     multitool scanner link and a vanilla <c>HealthAnalyzerComponent</c> for the
///     UI tick infrastructure.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MedicalScannerConsoleComponent : Component
{
}
