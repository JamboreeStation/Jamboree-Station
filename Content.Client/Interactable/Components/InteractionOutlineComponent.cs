// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

namespace Content.Client.Interactable.Components;

[RegisterComponent]
public sealed partial class InteractionOutlineComponent : Component
{
    public bool InRange;
    public int LastRenderScale;
    public bool Active;
}
