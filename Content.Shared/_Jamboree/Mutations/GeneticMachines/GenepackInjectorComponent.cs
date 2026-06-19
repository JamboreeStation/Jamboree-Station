using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Single-use injector that grants its contained mutations to a target on use.
///     Looks like a cyan implanter; takes the same use time as a mindshield implant.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GenepackInjectorComponent : Component
{
    /// <summary>
    ///     The mutations granted to the target on a successful inject.
    /// </summary>
    [DataField]
    public List<ProtoId<MutationPrototype>> Mutations = new();

    /// <summary>
    ///     Time required to inject the genepack into the target.
    /// </summary>
    [DataField]
    public float InjectTime = 5f;

    /// <summary>
    ///     Has this injector already been used?
    /// </summary>
    [DataField]
    public bool Spent = false;
}

[Serializable, NetSerializable]
public sealed partial class GenepackInjectorDoAfterEvent : SimpleDoAfterEvent
{
}
