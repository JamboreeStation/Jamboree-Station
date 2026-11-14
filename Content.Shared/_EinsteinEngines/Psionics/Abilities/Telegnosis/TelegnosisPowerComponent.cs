using Content.Shared.Actions.Components;
using Robust.Shared.Prototypes;


namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class TelegnosisPowerComponent : Component
    {
        [DataField("prototype")]
        public string Prototype = "MobTelegnosisObserver";
        public InstantActionComponent? TelegnosisPowerAction = null;

        public const string TelegnosisActionPrototype = "ActionTelegnosis";
        [DataField("telegnosisActionId")]
        public ProtoId<EntityPrototype>? TelegnosisActionId = "ActionTelegnosis";


        [DataField("telegnosisActionEntity")]
        public EntityUid? TelegnosisActionEntity;
    }
}
