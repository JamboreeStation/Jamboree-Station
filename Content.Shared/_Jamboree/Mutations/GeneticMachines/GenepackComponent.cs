using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Item that stores a frozen set of mutations, ready to be combined into other
///     genepacks via a Recombiner or injected into a target via a Genepack Injector.
///     Looks like a silver vacuum flask with cyan accents.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GenepackComponent : Component
{
    /// <summary>
    ///     The mutation prototypes contained in this genepack.
    /// </summary>
    [DataField]
    public List<ProtoId<MutationPrototype>> Mutations = new();

    /// <summary>
    ///     Optional user-supplied label, e.g. "Teddy bear", shown in UIs.
    /// </summary>
    [DataField]
    public string Label = string.Empty;
}
