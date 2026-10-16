// SPDX-License-Identifier: MIT

using Content.Shared._EinsteinEngines.Language;

namespace Content.Shared.Speech;

public sealed class ListenEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;
    public readonly LanguagePrototype Language; // Einstein Engines - Language

    public ListenEvent(string message, EntityUid source, LanguagePrototype language) // Einstein Engines - Language
    {
        Message = message;
        Source = source;
        Language = language;
    }
}

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;

    public ListenAttemptEvent(EntityUid source)
    {
        Source = source;
    }
}
