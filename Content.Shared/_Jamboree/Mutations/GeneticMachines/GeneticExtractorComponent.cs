using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Genetic Extractor console. Two modes:
///     Cleanse — strip all mutations from the occupant after 120s + apply nausea.
///     Isolate — kill the occupant after 60s, drop a genepack of every mutation
///               they had EXCEPT the one selected as the isolation target, and
///               strip all mutations from the (deceased) subject.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GeneticExtractorComponent : Component
{
    [DataField]
    public float CleanseTime = 120f;

    [DataField]
    public float IsolateTime = 60f;

    [DataField]
    public GeneticExtractorMode Mode = GeneticExtractorMode.Cleanse;

    /// <summary>
    ///     For Isolate mode, the mutation that will be excluded from the resulting
    ///     genepack (i.e. the gene the geneticist is trying to remove from the pool).
    /// </summary>
    [DataField]
    public ProtoId<MutationPrototype>? IsolatedMutation;

    [DataField]
    public TimeSpan? StartedAt;

    [DataField]
    public TimeSpan? FinishesAt;

    [DataField]
    public EntityUid? CurrentTarget;

    /// <summary>
    ///     Spawned when an Isolate run completes.
    /// </summary>
    [DataField]
    public EntProtoId GenepackPrototype = "Genepack";

    /// <summary>
    ///     Label applied to the produced Genepack on Isolate. Lets the geneticist
    ///     tag packs by intended use (e.g. "teddy bear", "captain backup"). Surfaces
    ///     via NameModifierSystem, so the entity displays as
    ///     "genepack (teddy bear)" everywhere it's referenced.
    /// </summary>
    [DataField]
    public string GenepackName = string.Empty;
}
