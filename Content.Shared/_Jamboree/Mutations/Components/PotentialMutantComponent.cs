using Content.Shared.Random;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Jamboree.Mutations;

[RegisterComponent, NetworkedComponent]
public sealed partial class PotentialMutantComponent : Component
{
    /// <summary>
    ///     The list of mutations that this potential mutant can roll into
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype> MutationPool = "RandomMutationPool";
}