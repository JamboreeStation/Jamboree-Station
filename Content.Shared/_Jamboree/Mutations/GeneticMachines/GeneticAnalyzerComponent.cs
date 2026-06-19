using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Handheld scanner that catalogs the mutations carried by a target creature.
///     Hold in hand, use on a creature, wait through a do-after, then read a popup
///     listing each mutation name. Behaves like the forensic analyzer.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GeneticAnalyzerComponent : Component
{
    /// <summary>
    ///     How long it takes to scan a creature for mutations, in seconds.
    /// </summary>
    [DataField]
    public float ScanDelay = 5f;
}

[Serializable, NetSerializable]
public sealed partial class GeneticAnalyzerDoAfterEvent : SimpleDoAfterEvent
{
}
