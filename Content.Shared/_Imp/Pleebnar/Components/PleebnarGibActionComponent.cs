// SPDX-FileCopyrightText: 2025 Bibble-B-Boop <mysticball8@gmail.com>
// SPDX-FileCopyrightText: 2025 JamboreeBot <JamboreeBot@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Pleebnar.Components;
/// <summary>
/// gibbing action component, denotes that an entity has access to the pleebnar gibbing action,
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PleebnarGibActionComponent : Component
{
    [DataField]
    public EntityUid? gibAction;

    [DataField]
    public string? gibActionId = "ActionPleebnarGib";
}
