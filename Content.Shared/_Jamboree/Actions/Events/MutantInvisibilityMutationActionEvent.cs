using Content.Shared.Actions;

namespace Content.Shared._Jamboree.Actions.Events;

public sealed partial class MutantInvisibilityMutationActionEvent : InstantActionEvent
{
    [DataField]
    public float PowerTimer = 30f;
}
