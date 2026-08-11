using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Tech.Clothing;

[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingEquipmentRequirementsComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;
}
