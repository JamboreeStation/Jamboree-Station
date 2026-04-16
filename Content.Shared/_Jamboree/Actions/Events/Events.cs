using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Jamboree.Actions.Events;

[Serializable, NetSerializable]
public sealed partial class MutantHealOtherDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public TimeSpan StartedAt;

    [DataField]
    public DamageSpecifier? HealingAmount = default!;

    [DataField]
    public float? RotReduction;

    [DataField]
    public bool DoRevive;

    /// <summary>
    ///     Caster's Amplification that has been modified by the results of a MoodContest.
    /// </summary>
    // public float ModifiedAmplification = default!; # Jamboree - No mood system

    /// <summary>
    ///     Caster's Dampening that has been modified by the results of a MoodContest.
    /// </summary>
    // public float ModifiedDampening = default!; # Jamboree - No mood system

    public MutantHealOtherDoAfterEvent(TimeSpan startedAt)
    {
        StartedAt = startedAt;
    }

    public override DoAfterEvent Clone() => this;
}