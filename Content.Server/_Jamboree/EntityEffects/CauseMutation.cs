using Content.Shared._Jamboree.Mutations;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._Jamboree.EntityEffects;

[UsedImplicitly]
public sealed partial class CauseMutation : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        // If not currently a mutant, roll for a mutation.
        // Current form only adds 1 mutation
        // May wish to change this in future to allow heaps of mutations.

        var potentialMutant = args.EntityManager.System<PotentialMutantSystem>();
        if (!args.EntityManager.TryGetComponent<PotentialMutantComponent>(args.TargetEntity, out var mutant))
            return;
        potentialMutant.TryGainRandomMutation(new(args.TargetEntity, mutant));
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-causes-mutations");
    }
}