// SPDX-FileCopyrightText: 2025 JamboreeBot <JamboreeBot@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Speech;
using Content.Server.Speech;
using Content.Shared.Speech;
using Content.Shared.Whitelist;

namespace Content.Goobstation.Server.Speech;

/// <summary>
/// Handles <see cref="ListenAttemptEvent"/> for <see cref="ListenBlacklistComponent"/>.
/// </summary>
public sealed partial class ListenBlacklistSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ListenBlacklistComponent, ListenAttemptEvent>(OnListenAttempt);
    }

    // TODO: if chat ever gets refactored move this system to shared
    private void OnListenAttempt(Entity<ListenBlacklistComponent> ent, ref ListenAttemptEvent args)
    {
        if (_whitelist.IsWhitelistPass(ent.Comp.Blacklist, args.Source))
            args.Cancel();
    }
}
