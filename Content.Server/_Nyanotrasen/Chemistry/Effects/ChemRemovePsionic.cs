using Content.Server._EinsteinEngines.Abilities.Psionics;
using Content.Shared._EinsteinEngines.Psionics;
using Content.Shared.EntityEffects;

namespace Content.Server._Nyanotrasen.Chemistry.Effects;

public sealed partial class ChemRemovePsionicSystem : EntityEffectSystem<MetaDataComponent, ChemRemovePsionic>
{
    [Dependency] private PsionicAbilitiesSystem _psionics = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ChemRemovePsionic> args)
    {
        if (args.Scale == 1f)
            _psionics.MindBreak(entity);
    }
}
