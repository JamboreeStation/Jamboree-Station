using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Jamboree.Mutations;

/// <summary>
/// Raised when the delay before a mutant collapses into a greater mutant elapses.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GreaterMutantTransformDoAfterEvent : SimpleDoAfterEvent;