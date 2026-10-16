// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Radiation.Events;

/// <summary>
///     Raised on entity when it was irradiated
///     by some radiation source.
/// </summary>
public readonly record struct OnIrradiatedEvent(float FrameTime, float RadsPerSecond, EntityUid? Origin, List<EntityUid>? Sources = null)
{
    public float TotalRads => RadsPerSecond * FrameTime;
}
