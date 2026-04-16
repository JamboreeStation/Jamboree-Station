using Robust.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Actions;

namespace Content.Shared._Jamboree.Actions.Events;

public sealed partial class MutantHealOtherMutationActionEvent : EntityTargetActionEvent
{
    /// <summary>
    ///     Caster's Amplification that has been modified by the results of a MoodContest.
    /// </summary>
    // public float ModifiedAmplification = default!; #Jamboree - no mood system

    /// <summary>
    ///     Caster's Dampening that has been modified by the results of a MoodContest.
    /// </summary>
    // public float ModifiedDampening = default!; #Jamboree - no mood system

    [DataField]
    public DamageSpecifier? HealingAmount = default!;

    [DataField]
    public string AbilityName = default!;

    /// Controls whether or not a power fires immediately and with no DoAfter
    [DataField]
    public bool Immediate;

    [DataField]
    public string? PopupText;

    [DataField]
    public float? RotReduction;

    [DataField]
    public bool DoRevive;

    [DataField]
    public bool BreakOnMove = true;

    [DataField]
    public float UseDelay = 8f;

    [DataField]
    public PopupType PopupType = PopupType.Medium;

    [DataField]
    public AudioParams AudioParams = default!;

    [DataField]
    public bool PlaySound;

    [DataField]
    public SoundSpecifier SoundUse = new SoundPathSpecifier("/Audio/_EinsteinEngines/Psionics/heartbeat_fast.ogg");
}
