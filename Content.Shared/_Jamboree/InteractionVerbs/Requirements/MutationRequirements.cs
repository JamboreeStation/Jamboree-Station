// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.InteractionVerbs;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared._Jamboree.InteractionVerbs.Requirements;

[Serializable, NetSerializable]
public sealed partial class SelfWhitelistRequirement : InteractionRequirement
{
    [DataField] public EntityWhitelist Whitelist = new(), Blacklist = new();

    public override bool IsMet(InteractionArgs args, InteractionVerbPrototype proto, InteractionAction.VerbDependencies deps)
    {
        return deps.WhitelistSystem.IsValid(Whitelist, args.User) &&
               !deps.WhitelistSystem.IsValid(Blacklist, args.User);
    }
}