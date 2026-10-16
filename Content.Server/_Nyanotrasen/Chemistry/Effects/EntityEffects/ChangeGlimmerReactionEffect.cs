using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared._EinsteinEngines.Psionics.Glimmer;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;

namespace Content.Server._EinsteinEngines.Chemistry.EntityEffects;

[DataDefinition]
public sealed partial class ChangeGlimmerReactionEffect : EntityEffect
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-change-glimmer-reaction-effect", ("chance", Probability),
            ("count", Count));

    /// <summary>
    ///     Added to glimmer when reaction occurs.
    /// </summary>
    [DataField]
    public float Count = 1;

    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GlimmerSystem>().DeltaGlimmerInput(Count * scale);
    }
}
