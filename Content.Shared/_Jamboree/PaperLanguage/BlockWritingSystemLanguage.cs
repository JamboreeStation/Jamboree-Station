// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later


// SPDX-License-Identifier PureBreadBagel 2026
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Systems;

namespace Content.Shared.Paper;

/* JAMBOREE - The whole point of this file is to just stop people writing in a paper
for a language they dont understand LOL
This is also a plain copy of BlockWritingSystem.cs but modified for our own custom language paper system. */
public sealed class BlockWritingSystemLanguage : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LanguageSpeakerComponent, PaperWriteAttemptEvent>(OnPaperWriteAttempt);
    }

    private void OnPaperWriteAttempt(Entity<LanguageSpeakerComponent> entity, ref PaperWriteAttemptEvent args)
    {
        if (!TryComp<PaperComponent>(args.Paper, out var paper))
            return;

        // JAMBOREE - i mean no default language written means it should be able to be written on regardless
        var written = paper.WrittenLanguage;
        if (written == null)
            return;

        // JAMBOREE - If the papers language just so happens to be Psychomantic, ignore and allow writing on it.
        if (written.Value.Id == SharedLanguageSystem.UniversalPrototype)
            return;

        // JAMBOREE - If the Player can understand the papers language, allow them to write in it.
        if (entity.Comp.UnderstoodLanguages.Contains(written.Value))
            return;

        // JAMBOREE - otherwise just deny writing in it. Its a temporary solution currently.
        // my next TODO is figuring out how to append the obfuscation text into the editing box but thats for aother time.
        args.FailReason = "paper-component-foreign-language";
        args.Cancelled = true;
    }
}
