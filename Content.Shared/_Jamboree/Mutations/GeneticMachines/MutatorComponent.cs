using Robust.Shared.GameStates;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Mutator console. Applies random mutations to whoever is sealed in the linked
///     medical scanner after a fixed processing time. Per design doc:
///     2 mutations for monkeys/kobolds, 4 mutations for humanoids.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MutatorComponent : Component
{
    [DataField]
    public float ProcessingTime = 30f;

    [DataField]
    public int HumanoidMutationCount = 4;

    [DataField]
    public int SimpleMobMutationCount = 2;

    /// <summary>
    ///     Insignificant amount of Cellular damage applied to the occupant when
    ///     a Mutator run completes. Tuned to be a flavour-text hint that the
    ///     process isn't free, not a meaningful health threat.
    /// </summary>
    [DataField]
    public float GeneticDamageOnUse = 3f;

    /// <summary>
    ///     Time at which the current processing run started. Null = idle.
    /// </summary>
    [DataField]
    public TimeSpan? StartedAt;

    /// <summary>
    ///     Time at which the current processing run finishes. Null = idle.
    /// </summary>
    [DataField]
    public TimeSpan? FinishesAt;

    /// <summary>
    ///     Body that was occupying the scanner when the run started.
    /// </summary>
    [DataField]
    public EntityUid? CurrentTarget;
}
