using Content.Server._EinsteinEngines.Psionics;
using Content.Shared._EinsteinEngines.Psionics;
using Content.Shared.EntityEffects;

namespace Content.Server._Nyanotrasen.Chemistry.Effects;

public sealed partial class ChemRerollPsionicSystem : EntityEffectSystem<MetaDataComponent, ChemRerollPsionic>
{
    [Dependency] private PsionicsSystem _psionics = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ChemRerollPsionic> args)
    {
        _psionics.RerollPsionics(entity, bonusMuliplier: args.Effect.BonusMuliplier);
    }
}
