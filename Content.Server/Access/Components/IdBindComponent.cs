// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Server.Access.Components;

[RegisterComponent]
public sealed partial class IdBindComponent : Component
{
    /// <summary>
    /// If true, also tries to get the PDA and set the owner to the entity
    /// </summary>
    [DataField]
    public bool BindPDAOwner = true;
}

