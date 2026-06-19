using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Evolutionizer console. Transforms a monkey into a random Urist McHuman and a
///     kobold into a random Urist McReptile via polymorph prototypes. Emagged variant
///     accepts a DNA fingerprint and copies that character instead.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EvolutionizerComponent : Component
{
    [DataField]
    public float ProcessingTime = 10f;

    /// <summary>
    ///     Polymorph applied to entities matching <see cref="MonkeyTags"/>.
    /// </summary>
    [DataField]
    public ProtoId<PolymorphPrototype>? MonkeyPolymorph = "EvolutionizerMonkeyToHuman";

    /// <summary>
    ///     Polymorph applied to entities matching <see cref="KoboldTags"/>.
    /// </summary>
    [DataField]
    public ProtoId<PolymorphPrototype>? KoboldPolymorph = "EvolutionizerKoboldToReptilian";

    /// <summary>
    ///     Optional DNA fingerprint set via emag UI. When set, attempts to copy the
    ///     matching character instead of using the default polymorph.
    /// </summary>
    [DataField]
    public string TargetDna = string.Empty;

    [DataField]
    public TimeSpan? StartedAt;

    [DataField]
    public TimeSpan? FinishesAt;

    [DataField]
    public EntityUid? CurrentTarget;

    /// <summary>
    ///     Species → base entity prototype map used by the emag-DNA branch when
    ///     building a transient PolymorphConfiguration. The base entity is what
    ///     the donor's appearance is then cloned onto.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SpeciesPrototype>, EntProtoId> SpeciesBasePrototypes = new()
    {
        { "Human",     "MobHuman" },
        { "Reptilian", "MobReptilian" },
    };

    /// <summary>
    ///     Fallback base entity for emag-DNA cloning when the donor's species
    ///     isn't in <see cref="SpeciesBasePrototypes"/>.
    /// </summary>
    [DataField]
    public EntProtoId EmagFallbackBase = "MobHuman";
}
