using Content.Shared.Stealth.Components;
using Content.Shared.StatusIcon;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.StatusIcon;

/// <summary>
/// StatusIcons for showing the psionics status on the epi HUD
/// </summary>
[Prototype]
public sealed partial class PsionicsIconPrototype : StatusIconPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<PsionicsIconPrototype>))]
    public string[]? Parents { get; set; }


    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; set; }

}

