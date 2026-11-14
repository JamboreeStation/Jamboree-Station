// SPDX-FileCopyrightText: 2026 PureBreadBagel <purebreadbagel@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared._Jamboree.Chemistry;
using Robust.Client.GameObjects;

namespace Content.Goobstation.Client._Jamboree.Chemistry.Visualizers;

/// <summary>
/// Toggles a beaker overlay layer and the powered state of the dispenser sprite.
/// </summary>
public sealed class EnergyReagentDispenserVisualizerSystem : VisualizerSystem<EnergyReagentDispenserVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, EnergyReagentDispenserVisualsComponent component, ref AppearanceChangeEvent args)
    // This method gets called when the appearance changes...So like, if i add beaker it updates. or powercell. etc.
    {
        // If the entity has no sprite, there is nothing to update, so do absolutley NOTHING.
        if (args.Sprite == null)
            return;

        //   TryGetData returns false if the key is missing, a key is an identifier like "Powered". So if the key is missing then we treat it as powered!.
        //   If the key exists, use its value. So: unpowered only when the flag is explicitly set to false.
        var powered = !AppearanceSystem.TryGetData<bool>(uid, EnergyReagentDispenserVisuals.Powered, out var poweredData, args.Component) || poweredData;

        // "HasBeaker": true only if the Beaker key exists AND its value is true.
        //   (Missing key, or key present with value false, both mean "no beaker".)
        var hasBeaker = AppearanceSystem.TryGetData<bool>(uid, EnergyReagentDispenserVisuals.Beaker, out var beaker, args.Component) && beaker;

        // Set the base sprite (layer 0) to the powered or unpowered RSI state.
        SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, powered ? component.BaseState : component.NoPowerState);

        // Only try to toggle the beaker overlay if the sprite actually defines a beaker layer.
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), EnergyReagentDispenserVisualLayers.Beaker, out var beakerLayer, false))
            // Show the beaker overlay whenever a beaker is present.
            SpriteSystem.LayerSetVisible((uid, args.Sprite), beakerLayer, hasBeaker);
    }
}
