using Content.Shared.DoAfter;

namespace Content.Shared._Jamboree.Mutations;

/// <summary>
/// Present on a mutant while it is collapsing into a greater mutant. Removed when the transform
/// completes or is aborted -- see GreaterMutantSystem.
/// </summary>
[RegisterComponent]
public sealed partial class PendingGreaterMutantComponent : Component
{
    /// <summary>
    /// The in-progress transform, so it can be cancelled if the mutation count drops back below
    /// the threshold. Not persisted; a pending transform is not worth saving.
    /// </summary>
    [ViewVariables]
    public DoAfterId? DoAfter;
}
