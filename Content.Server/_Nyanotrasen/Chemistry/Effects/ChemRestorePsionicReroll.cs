using Content.Shared.Abilities.Psionics;
using Content.Shared._EinsteinEngines.Psionics;
using Content.Shared.EntityEffects;

namespace Content.Server._Nyanotrasen.Chemistry.Effects;

public sealed partial class ChemRestorePsionicRerollSystem : EntityEffectSystem<PsionicComponent, ChemRestorePsionicReroll>
{
    protected override void Effect(Entity<PsionicComponent> entity, ref EntityEffectEvent<ChemRestorePsionicReroll> args)
    {
        if (entity.Comp.Roller || args.Effect.BypassRoller)
            entity.Comp.CanReroll = true;
    }
}
