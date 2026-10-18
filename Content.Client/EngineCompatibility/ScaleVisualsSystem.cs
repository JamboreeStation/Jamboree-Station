// SPDX-FileCopyrightText: 2019 Space Station 14 Contributors
// SPDX-License-Identifier: MIT
// Adapted from RobustToolbox 56eda3ea92913e60c27aa029ba7877c8559dfe9d.

using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects;

public sealed class ScaleVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ScaleVisualsComponent, AppearanceChangeEvent>(OnChangeData);
    }

    private void OnChangeData(EntityUid uid, ScaleVisualsComponent component, ref AppearanceChangeEvent ev)
    {
        if (!ev.AppearanceData.TryGetValue(ScaleVisuals.Scale, out var scale) ||
            ev.Sprite == null) return;

        var vecScale = (Vector2)scale;

        // Set it directly because prediction may call this multiple times.
        ev.Sprite.Scale = vecScale;
    }
}
