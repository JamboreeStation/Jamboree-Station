// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Jamboree.CustomRadio;
using Robust.Client.GameObjects;

namespace Content.Client._Jamboree.CustomRadio;

public sealed class CustomEncryptionKeyVisualizerSystem : VisualizerSystem<CustomEncryptionKeyComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, CustomEncryptionKeyComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !AppearanceSystem.TryGetData<Color>(uid, CustomEncryptionKeyVisuals.Color, out var color, args.Component))
            return;

        SpriteSystem.LayerSetColor((uid, args.Sprite), CustomEncryptionKeyLayers.Base, color);
    }
}