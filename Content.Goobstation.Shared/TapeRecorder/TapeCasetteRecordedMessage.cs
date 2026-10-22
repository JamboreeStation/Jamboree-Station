// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech;
using Content.Shared._EinsteinEngines.Language;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.TapeRecorder;

/// <summary>
/// Every chat event recorded on a tape is saved in this format
/// </summary>
[DataDefinition]
public sealed partial class TapeCassetteRecordedMessage : IComparable<TapeCassetteRecordedMessage>
{
    /// <summary>
    /// Number of seconds since the start of the tape that this event was recorded at
    /// </summary>
    [DataField(required: true)]
    public float Timestamp = 0;

    /// <summary>
    /// The name of the entity that spoke
    /// </summary>
    [DataField]
    public string? Name;

    /// <summary>
    /// The verb used for this message.
    /// </summary>
    [DataField]
    public ProtoId<SpeechVerbPrototype>? Verb;

    /// <summary>
    /// What was spoken, as heard. This is what gets played back.
    /// </summary>
    [DataField]
    public string Message = string.Empty;

    /// <summary>
    /// What the recorder writes down for this message. Obfuscated for languages the
    /// recorder doesn't understand, null if it recorded the message verbatim.
    /// </summary>
    [DataField]
    public string? ObfuscatedMessage;

    /// <summary>
    /// The language the message was spoken in, so playback can reproduce it faithfully.
    /// </summary>
    [DataField]
    public ProtoId<LanguagePrototype>? Language;

    public TapeCassetteRecordedMessage(float timestamp, string name, ProtoId<SpeechVerbPrototype> verb, string message, ProtoId<LanguagePrototype>? language = null, string? obfuscatedMessage = null)
    {
        Timestamp = timestamp;
        Name = name;
        Verb = verb;
        Message = message;
        Language = language;
        ObfuscatedMessage = obfuscatedMessage;
    }

    public int CompareTo(TapeCassetteRecordedMessage? other)
    {
        if (other == null)
            return 0;

        return (int) (Timestamp - other.Timestamp);
    }
}
