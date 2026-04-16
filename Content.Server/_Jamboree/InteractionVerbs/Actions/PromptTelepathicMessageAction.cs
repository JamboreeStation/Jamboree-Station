// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Server.Prayer;
using Content.Shared.InteractionVerbs;
using Robust.Shared.Player;

namespace Content.Server._Jamboree.InteractionVerbs.Actions;

[Serializable]
public sealed partial class PromptTelepathicMessageAction : InteractionAction
{
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly PrayerSystem _prayer = default!;
    public override bool CanPerform(InteractionArgs args, InteractionVerbPrototype proto, bool beforeDelay, VerbDependencies deps)
    {
        return deps.EntMan.HasComponent<ActorComponent>(args.User) && deps.EntMan.HasComponent<ActorComponent>(args.Target);
    }

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        if (!deps.EntMan.TryGetComponent<ActorComponent>(args.User, out var actor)
         || !deps.EntMan.TryGetComponent<ActorComponent>(args.Target, out var actorTarget))
            return false;

        _quickDialog.OpenDialog(actor!.PlayerSession, "Subtle Message", "Message", "Popup Message", (string message, string popupMessage) =>
        {
            _prayer.SendSubtleMessage(actorTarget!.PlayerSession, actor!.PlayerSession, message, popupMessage == "" ? Loc.GetString("prayer-popup-subtle-default") : popupMessage);
        });
        return true;
    }
}