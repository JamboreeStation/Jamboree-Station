// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._EinsteinEngines.Language;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.TapeRecorder;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedTapeRecorderSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TapeRecorderComponent : Component
{
    /// <summary>
    /// Languages the recorder understands and records verbatim. Any other language is recorded as
    /// the same gibberish a non-understanding listener would hear, so a tape can't be used as a
    /// universal translator for exotic or secret languages.
    /// </summary>
    [DataField]
    public List<ProtoId<LanguagePrototype>> UnderstoodLanguages = [];

    /// <summary>
    /// The current tape recorder mode, controls what using the item will do
    /// </summary>
    [DataField, AutoNetworkedField]
    public TapeRecorderMode Mode = TapeRecorderMode.Stopped;

    /// <summary>
    /// Paper that will spawn when printing transcript
    /// </summary>
    [DataField]
    public EntProtoId PaperPrototype = "TapeRecorderTranscript";

    /// <summary>
    /// How fast can this tape recorder rewind
    /// Acts as a multiplier for the frameTime
    /// </summary>
    [DataField]
    public float RewindSpeed = 3f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan CooldownEndTime = TimeSpan.Zero;

    /// <summary>
    /// Cooldown of print button
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Default name as fallback if a message doesn't have one.
    /// </summary>
    [DataField]
    public LocId DefaultName = "tape-recorder-voice-unknown";

    /// <summary>
    /// Sound on print transcript
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };

    /// <summary>
    /// What sound is used when play mode is activated
    /// </summary>
    [DataField]
    public SoundSpecifier PlaySound = new SoundPathSpecifier("/Audio/_DV/Items/TapeRecorder/play.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };

    /// <summary>
    /// What sound is used when stop mode is activated
    /// </summary>
    [DataField]
    public SoundSpecifier StopSound = new SoundPathSpecifier("/Audio/_DV/Items/TapeRecorder/stop.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };

    /// <summary>
    /// What sound is used when rewind mode is activated
    /// </summary>
    [DataField]
    public SoundSpecifier RewindSound = new SoundPathSpecifier("/Audio/_DV/Items/TapeRecorder/rewind.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };
}
