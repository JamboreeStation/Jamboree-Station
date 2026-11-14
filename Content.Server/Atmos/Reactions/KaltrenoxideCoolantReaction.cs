// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
// SPDX-FileCopyrightText: 2026 PureBreadBagel <purebreadbagel@users.noreply.github.com>
// SPDX-License-Identifier: AGPL-3.0-or-later
//

using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

// THIS IS A JAMBOREE FORK GAS!
[UsedImplicitly]
[DataDefinition]
public sealed partial class KaltrenoxideCoolantReaction : IGasReactionEffect
{
    private const float TargetTemperature = 73.15f; // In Kelvin, so this is -200C. Kaltrenoxide wants to sit here.
    private const float CoolingPerSecond = 55000f; // How much energy Kalt removes so it can get to the target temp.

    // Slow passive decay, even when nothing is touching it.
    private const float PassiveDecayPerSecond = 0.05f;

    // Extra decay when Nitrogen is present. Adds to the passive rate, it doesn't replace it.
    private const float NitrogenDecayPerSecond = 0.25f;

    public ReactionResult React(
    GasMixture mixture,
    IGasMixtureHolder? holder,
    AtmosphereSystem atmosphereSystem,
    float reactionDelta)
    {
        if (reactionDelta <= 0f)
            return ReactionResult.NoReaction;

        var reacted = false;

        // Kaltrenoxide is stable inside sealed canisters and pipe nets, so nothing changes there.
        if (holder is GasCanisterComponent or IPipeNet)
            return ReactionResult.NoReaction;

        if (mixture.Temperature > TargetTemperature)
        {
            // Guarding the divisor keeps us from dividing by a near-zero heat capacity and spiking the temperature.
            var heatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);

            if (heatCapacity > Atmospherics.MinimumHeatCapacity)
            {
                mixture.Temperature = MathF.Max(
                    TargetTemperature,
                    mixture.Temperature - CoolingPerSecond * reactionDelta / heatCapacity);

                reacted = true;
            }
        }

        var passiveDecay = MathF.Min(mixture.GetMoles(Gas.Kaltrenoxide),
            PassiveDecayPerSecond * reactionDelta);

        if (passiveDecay > 0f)
        {
            mixture.AdjustMoles(Gas.Kaltrenoxide, -passiveDecay);
            reacted = true;
        }

        var nitrogen = mixture.GetMoles(Gas.Nitrogen);

        if (nitrogen > 0f)
        {
            // Same saturating ramp as the other reagents: full rate at 5 moles of Nitrogen.
            var nitrogenFactor = MathF.Min(1f, nitrogen / 5f);

            var nitrogenDecay = MathF.Min(
                mixture.GetMoles(Gas.Kaltrenoxide),
                NitrogenDecayPerSecond * reactionDelta * nitrogenFactor);

            if (nitrogenDecay > 0f)
            {
                // Nitrogen actively eats Kaltrenoxide, so this runs on top of the passive decay above
                // rather than replacing it.
                mixture.AdjustMoles(Gas.Kaltrenoxide, -nitrogenDecay);
                reacted = true;
            }
        }

        return reacted ? ReactionResult.Reacting : ReactionResult.NoReaction;
    }
}
