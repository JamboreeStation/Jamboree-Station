using Robust.Shared.GameStates;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Worn around the neck like an electropack. While equipped, every mutant ability
///     attempt by the wearer is cancelled. Can only be removed by another creature.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MutantInhibitorCollarComponent : Component
{
}

/// <summary>
///     Marker placed on creatures whose mutant abilities are being suppressed
///     (currently by a worn <see cref="MutantInhibitorCollarComponent"/>).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MutantSuppressedComponent : Component
{
}
