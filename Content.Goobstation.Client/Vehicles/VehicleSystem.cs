// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics; // Frontier
using Content.Goobstation.Shared.Vehicles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Graphics.RSI; // Frontier

namespace Content.Goobstation.Client.Vehicles;

public sealed partial class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private SpriteSystem _sprites = default!;
    [Dependency] private SharedTransformSystem _transform = default!; // Frontier

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<VehicleComponent, MoveEvent>(OnMove);
    }

    private void OnAppearanceChange(EntityUid uid, VehicleComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(uid, VehicleState.Animated, out bool animated))
            return;

        if (!TryComp<SpriteComponent>(uid, out var spriteComp))
            return;

        SpritePos(uid, comp);
        // Start Frontier - Handle AutoAnimate
        if (!spriteComp.LayerMapTryGet(VehicleVisualLayers.AutoAnimate, out var layer))
            layer = 0;
        spriteComp.LayerSetAutoAnimated(layer, animated);
        // End Frontier
    }

    private void OnMove(EntityUid uid, VehicleComponent component, ref MoveEvent args)
    {
        SpritePos(uid, component);
    }

    private void SpritePos(EntityUid uid, VehicleComponent comp)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComp))
            return;

        if (!_appearance.TryGetData<bool>(uid, VehicleState.DrawOver, out bool depth))
            return;

        spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.Objects;

        if (comp.RenderOver == VehicleRenderOver.None)
            return;

        var eye = _eye.CurrentEye;
        Direction vehicleDir = (Transform(uid).LocalRotation + eye.Rotation).GetCardinalDir();

        VehicleRenderOver renderOver = (VehicleRenderOver)(1 << (int)vehicleDir);

        if ((comp.RenderOver & renderOver) == renderOver)
        {
            spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.OverMobs;
        }
        else
        {
            spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.Objects;
        }
    }

    // Start Frontier - Extra Offset fields
    // Could potentially be merged into SpritePos but eh
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<VehicleComponent, SpriteComponent>();
        var eye = _eye.CurrentEye;
        while (query.MoveNext(out var uid, out var vehicle, out var sprite))
        {
            var angle = _transform.GetWorldRotation(uid) + eye.Rotation;
            if (angle < 0)
                angle += 2 * Math.PI;
            RsiDirection dir = SpriteComponent.Layer.GetDirection(RsiDirectionType.Dir4, angle);

            Vector2 offset = Vector2.Zero;
            if (vehicle.Driver != null)
            {
                switch (dir)
                {
                    case RsiDirection.South:
                    default:
                        offset = vehicle.SouthOffset;
                        break;
                    case RsiDirection.North:
                        offset = vehicle.NorthOffset;
                        break;
                    case RsiDirection.East:
                        offset = vehicle.EastOffset;
                        break;
                    case RsiDirection.West:
                        offset = vehicle.WestOffset;
                        break;
                }
            }

            // Avoid recalculating a matrix if we can help it.
            if (sprite.Offset != offset)
                sprite.Offset = offset;
        }
    }
    // End Frontier
}

// Start Frontier - Animate Vehicle Automatically
public enum VehicleVisualLayers : byte
{
    /// Layer for the vehicle's wheels/jets/etc.
    AutoAnimate,
}
// End Frontier
