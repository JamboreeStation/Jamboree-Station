// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.DisplacementMap;
using Content.Shared._Tech.Displacement;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Tech.Displacement;

public sealed partial class HumanoidDisplacementSystem : EntitySystem
{
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private MarkingManager _markings = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidDisplacementComponent, HumanoidSpriteUpdatedEvent>(OnSpriteUpdated);
    }

    private void OnSpriteUpdated(Entity<HumanoidDisplacementComponent> ent, ref HumanoidSpriteUpdatedEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var spriteComp) || !TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;

        var sprite = (ent.Owner, spriteComp);

        foreach (var key in ent.Comp.AppliedLayers)
        {
            _sprite.RemoveLayer(sprite, key, false);
        }

        ent.Comp.AppliedLayers.Clear();

        foreach (var layer in Enum.GetValues<HumanoidVisualLayers>())
        {
            if (_sprite.LayerMapTryGet(sprite, layer, out var index, false))
                Displace(ent, sprite, index, $"enum.{nameof(HumanoidVisualLayers)}.{layer}");
        }

        foreach (var markings in humanoid.ClientOldMarkings.Markings.Values)
        {
            foreach (var marking in markings)
            {
                if (!_markings.TryGetMarking(marking, out var proto))
                    continue;

                foreach (var markingSprite in proto.Sprites)
                {
                    if (markingSprite is not SpriteSpecifier.Rsi rsi)
                        continue;

                    var key = $"{proto.ID}-{rsi.RsiState}";
                    if (_sprite.LayerMapTryGet(sprite, key, out var index, false))
                        Displace(ent, sprite, index, key);
                }
            }
        }
    }

    private void Displace(Entity<HumanoidDisplacementComponent> ent, Entity<SpriteComponent> sprite, int index, string key)
    {
        if (_displacement.TryAddDisplacement(ent.Comp.Displacement, sprite, index, key, out var displacementKey))
            ent.Comp.AppliedLayers.Add(displacementKey);
    }
}

[ByRefEvent]
public readonly record struct HumanoidSpriteUpdatedEvent;
