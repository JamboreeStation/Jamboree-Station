using Robust.Shared.Audio;

namespace Content.Server._Jamboree.Abilities.Mutations;

[RegisterComponent]
public sealed partial class MutantInvisibilityUsedComponent : Component
{
    [DataField]
    public float StunTime = 4f;

    [DataField]
    public float DamageToStun = 5f;

    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/_EinsteinEngines/Psionics/wavy.ogg");

    [DataField]
    public SoundSpecifier EndSound = new SoundPathSpecifier("/Audio/_EinsteinEngines/Psionics/wavy.ogg");
}
