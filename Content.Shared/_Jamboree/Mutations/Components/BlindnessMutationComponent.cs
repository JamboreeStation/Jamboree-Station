using Robust.Shared.GameStates;

namespace Content.Shared._Jamboree.Mutations;

/// <summary>
/// Blinds the entity for as long as it is present. Applied and removed by the genetics blindness
/// mutation.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlindnessMutationComponent : Component;
