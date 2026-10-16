// SPDX-FileCopyrightText: 2026 Goob Station Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Goobstation.UIKit.UserInterface.Controls;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Goobstation.UIKit.UserInterface.RichText;

public sealed partial class IconTag : IMarkupTagHandler
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IEntityManager _entMan = default!;
    private SpriteSystem? _spriteSystem;

    public string Name => "icon";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Attributes.TryGetValue("src", out var id) || id.StringValue == null)
        {
            control = null;
            return false;
        }
        _spriteSystem ??= _entMan.System<SpriteSystem>();
        var texture = _prototype.TryIndex<JobIconPrototype>(id.StringValue, out var iconPrototype)
                ? _spriteSystem.Frame0(iconPrototype.Icon)
                : null;
        if (texture == null)
        {
            control = null;
            return false;
        }

        // Desired draw size, in virtual pixels.
        var size = 20f;
        if (node.Attributes.TryGetValue("size", out var sizeAttr)
            && sizeAttr.TryGetLong(out var s)
            && s is { } sizeValue
            && sizeValue > 0)
        {
            size = sizeValue;
        }

        // This is where the icons get drawn. They use bounding boxes!
        var scale = size / texture.Size.X;

        // Optional pixel offset, useful for vertically centering the icon
        // against the text line :P
        float offsetX = 0f;
        float offsetY = 0f;
        if (node.Attributes.TryGetValue("offsetX", out var offXAttr)
            && offXAttr.TryGetLong(out var ox)
            && ox is { } offsetXValue)
        {
            offsetX = offsetXValue;
        }
        if (node.Attributes.TryGetValue("offsetY", out var offYAttr)
            && offYAttr.TryGetLong(out var oy)
            && oy is { } offsetYValue)
        {
            offsetY = offsetYValue;
        }

        string? tooltipValue = null;
        if (node.Attributes.TryGetValue("tooltip", out var tooltip) && tooltip.StringValue != null)
            tooltipValue = tooltip.StringValue;

        var icon = new TooltipTextureRect(tooltipValue, new Vector2(offsetX, offsetY))
        {
            Texture = texture,
            TextureScale = new Vector2(scale, scale),
            SetWidth = size,
            SetHeight = size,
            MouseFilter = Control.MouseFilterMode.Stop,
        };
        control = icon;
        return true;
    }
}
