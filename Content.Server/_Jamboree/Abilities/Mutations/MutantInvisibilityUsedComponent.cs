// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

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
