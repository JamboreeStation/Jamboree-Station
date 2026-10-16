// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._EinsteinEngines.Language;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaperComponent : Component
{
    public PaperAction Mode;
    [DataField("content"), AutoNetworkedField]
    public string Content { get; set; } = "";

    /// <summary>
    ///     The language the paper was written in, used to gate readability for entities
    ///     that do not understand it. Null if the paper has never been written on.
    /// </summary>
    [DataField("writtenLanguage"), AutoNetworkedField]
    public ProtoId<LanguagePrototype>? WrittenLanguage = null;

    [DataField("contentSize")]
    public int ContentSize { get; set; } = 10000;

    [DataField("stampedBy"), AutoNetworkedField]
    public List<StampDisplayInfo> StampedBy { get; set; } = new();

    /// <summary>
    ///     Stamp to be displayed on the paper, state from bureaucracy.rsi
    /// </summary>
    [DataField("stampState"), AutoNetworkedField]
    public string? StampState { get; set; }

    [DataField, AutoNetworkedField]
    public bool EditingDisabled;

    /// <summary>
    /// Sound played after writing to the paper.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound { get; private set; } = new SoundCollectionSpecifier("PaperScribbles", AudioParams.Default.WithVariation(0.1f));

    [Serializable, NetSerializable]
    public sealed class PaperBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string Text;
        public readonly List<StampDisplayInfo> StampedBy;
        public readonly PaperAction Mode;
        public readonly ProtoId<LanguagePrototype>? WrittenLanguage;

        public PaperBoundUserInterfaceState(string text, List<StampDisplayInfo> stampedBy, PaperAction mode = PaperAction.Read, ProtoId<LanguagePrototype>? writtenLanguage = null)
        {
            Text = text;
            StampedBy = stampedBy;
            Mode = mode;
            WrittenLanguage = writtenLanguage;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperInputTextMessage : BoundUserInterfaceMessage
    {
        public readonly string Text;
        public readonly ProtoId<LanguagePrototype>? Language;

        public PaperInputTextMessage(string text, ProtoId<LanguagePrototype>? language = null)
        {
            Text = text;
            Language = language;
        }
    }

    // Starlight-start
    [Serializable, NetSerializable]
    public sealed class PaperSignatureRequestMessage : BoundUserInterfaceMessage
    {
        public readonly int SignatureIndex;

        public PaperSignatureRequestMessage(int signatureIndex)
        {
            SignatureIndex = signatureIndex;
        }
    }
    // Starlight-end
    [Serializable, NetSerializable]
    public enum PaperUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum PaperAction
    {
        Read,
        Write,
    }

    [Serializable, NetSerializable]
    public enum PaperVisuals : byte
    {
        Status,
        Stamp
    }

    [Serializable, NetSerializable]
    public enum PaperStatus : byte
    {
        Blank,
        Written
    }
}