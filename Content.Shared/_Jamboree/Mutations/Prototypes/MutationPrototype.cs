using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._Jamboree.Mutations;

[Prototype]
public sealed partial class MutationPrototype : IPrototype
{
    /// <summary>
    ///     The ID of the mutation.
    /// </summary>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     The name of the mutation.
    /// </summary>
    [DataField(required: true)]
    public string Name = default!;

    /// <summary>
    ///     The description the Gene UI / Ability UI
    /// </summary>
    [DataField(required: true)]
    public string Description = default!;

    /// <summary>
    ///     Is this power an active power (Or a passive, otherwise)
    /// </summary>
    [DataField]
    public bool IsActive = false;

    /// <summary>
    ///     Is this power a mental power (as opposed to physical?)
    /// </summary>
    [DataField]
    public bool IsMental = false;

    /// <summary>
    ///     Is this mutation considered beneficial?
    /// </summary>
    [DataField]
    public bool Beneficial = true;

    /// <summary>
    ///     These functions are called when a Mutation is gained.
    /// </summary>
    [DataField(serverOnly: true)]
    public MutationFunction[] InitializeFunctions { get; private set; } = Array.Empty<MutationFunction>();

    /// <summary>
    ///     These functions are called when a Mutation is lost,
    ///     as a rule of thumb these should do the exact opposite of most of a mutations's init functions.
    /// </summary>
    [DataField(serverOnly: true)]
    public MutationFunction[] RemovalFunctions { get; private set; } = Array.Empty<MutationFunction>();
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class MutationFunction
{
    public abstract void OnMutate(
        EntityUid mob,
        IComponentFactory factory,
        IEntityManager entityManager,
        ISerializationManager serializationManager,
        ISharedPlayerManager playerManager,
        ILocalizationManager loc,
        MutantComponent mutantComponent,
        MutationPrototype proto);
}