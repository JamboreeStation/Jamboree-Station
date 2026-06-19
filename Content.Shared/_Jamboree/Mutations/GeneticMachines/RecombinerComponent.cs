 using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Recombiner console. Accepts genepacks via item slots, lets the user pick
///     a balanced set of genes from those packs, and produces a single
///     <see cref="GenepackInjectorComponent"/> after a processing delay.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RecombinerComponent : Component
{
    /// <summary>
    ///     Maximum complexity of an assembled injector. Total complexity of
    ///     selected mutations cannot exceed this.
    /// </summary>
    [DataField]
    public int ComplexityCap = 6;

    [DataField]
    public float ProcessingTime = 30f;

    /// <summary>
    ///     Item slot ids that can hold loaded genepacks. Define enough slots in
    ///     the prototype to match the desired library size.
    /// </summary>
    [DataField]
    public List<string> SlotIds = new()
    {
        "GenepackSlot1", "GenepackSlot2", "GenepackSlot3", "GenepackSlot4",
        "GenepackSlot5", "GenepackSlot6", "GenepackSlot7", "GenepackSlot8",
    };

    /// <summary>
    ///     Genepacks marked for inclusion in the next assembly run, stored as
    ///     net entity ids. The final injector is built from the deduplicated
    ///     union of every selected pack's mutations.
    /// </summary>
    [DataField]
    public List<NetEntity> SelectedPacks = new();

    [DataField]
    public string GenotypeName = "teddy bear";

    [DataField]
    public TimeSpan? StartedAt;

    [DataField]
    public TimeSpan? FinishesAt;

    [DataField]
    public EntProtoId InjectorPrototype = "GenepackInjector";
}
