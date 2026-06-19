using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._Jamboree.Mutations;

[Prototype]
public sealed partial class MutationPrototype : IPrototype, IInheritingPrototype
{
    /// <summary>
    ///     The ID of the mutation.
    /// </summary>
    [IdDataField]
    public string ID { get; } = default!;

    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<MutationPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

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
    ///     Complexity cost of this gene when assembling in a Recombiner.
    ///     Total complexity of a genepack must fit within the Recombiner's complexity cap.
    /// </summary>
    [DataField]
    public int Complexity = 1;

    /// <summary>
    ///     Metabolic efficiency contribution. Positive values reduce hunger drain,
    ///     negative values raise hunger drain. Shown in the Recombiner UI.
    /// </summary>
    [DataField]
    public int MetabolicEfficiency = -1;

    /// <summary>
    ///     Icon used to display this gene in the Recombiner / gene UIs.
    ///     Falls back to a generic mutation icon when unset.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public SpriteSpecifier? Icon;

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