using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._EinsteinEngines.Psionics;

public sealed partial class ChemRerollPsionic : EntityEffectBase<ChemRerollPsionic>
{
    [DataField("bonusMultiplier")]
    public float BonusMuliplier = 1f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-chem-reroll-psionic", ("chance", Probability));
}

public sealed partial class ChemRemovePsionic : EntityEffectBase<ChemRemovePsionic>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-chem-remove-psionic", ("chance", Probability));
}

public sealed partial class ChemRestorePsionicReroll : EntityEffectBase<ChemRestorePsionicReroll>
{
    [DataField]
    public bool BypassRoller;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-chem-restorereroll-psionic");
}
