// SPDX-License-Identifier: MIT

using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Funkystation.WashingMachine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class WashingMachineComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan WashTime = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? WashFinishTime;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(6);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextWashAllowed;

    [DataField]
    public SoundSpecifier? WashLoopSound;

    [DataField]
    public SoundSpecifier? WashFinishedSound;

    [DataField, AutoNetworkedField]
    public WashingMachineState State = WashingMachineState.Idle;

    public EntityUid? AudioStream;

    [DataField]
    public ProtoId<DamageTypePrototype> WashingDamageType = "Blunt";

    [DataField, AutoNetworkedField]
    public float BluntDamagePerSecond = 6.0f;

    [DataField, AutoNetworkedField]
    public float ThumpSoundChance = 0.8f;

    [DataField, AutoNetworkedField]
    public string WaterSprayReagent = "Water";

    [DataField, AutoNetworkedField]
    public float WaterSprayAmount = 150.0f;

    [DataField, AutoNetworkedField]
    public float WaterSprayChance = 1.0f;

    [DataField, AutoNetworkedField]
    public float SelfDamagePerSecond = 6.0f;

    [ViewVariables, AutoNetworkedField]
    public float AccumulatedSelfDamage = 0f;

    [DataField]
    public SoundSpecifier HitSound = new SoundCollectionSpecifier("MetalThud");
}

[Serializable, NetSerializable]
public enum WashingMachineState : byte
{
    Idle,
    Washing,
    Broken
}

[Serializable, NetSerializable]
public enum WashingMachineVisuals : byte
{
    State
}
