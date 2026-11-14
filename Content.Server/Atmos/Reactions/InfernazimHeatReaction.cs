// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
// SPDX-FileCopyrightText: 2026 PureBreadBagel <purebreadbagel@users.noreply.github.com>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;


// THIS IS A JAMBOREE-FORK GAS!
[UsedImplicitly]
[DataDefinition]
public sealed partial class InfernazimHeatReaction : IGasReactionEffect
{
    private const float TargetTemperature = 6000f; // The target temp that Infernazim wants to be at. In Kelvin.
    private const float EnergyPerSecond = 400000f; // How much energy Infernazim generates so it can get to the target temp.

    private const float PassiveDecayPerSecond = 0.20f; // The mole decay rate of Infernazim when nothing is touching it.
    private const float ConversionPerSecond = 0.50f; // The mole conversion rate of Infernazim when it is touching Oxygen or Kaltrenoxide.

    public ReactionResult React(
    GasMixture mixture,
    IGasMixtureHolder? holder,
    AtmosphereSystem atmosphereSystem,
    float reactionDelta)
    {
        if (reactionDelta <= 0f)
            return ReactionResult.NoReaction;

        var reacted = false;

        // Infernazim is stable inside sealed canisters and pipe nets, so nothing changes there.
        if (holder is GasCanisterComponent or IPipeNet)
            return ReactionResult.NoReaction;

        var oxygen = mixture.GetMoles(Gas.Oxygen);
        var kaltrenoxide = mixture.GetMoles(Gas.Kaltrenoxide);

        var catalyst = oxygen + kaltrenoxide; // Infernazim reacts with both, so they pool into one catalyst term

        if (catalyst > 0f)
        {
            // Saturating ramp: full reaction rate at 5 moles of catalyst, tapering to nothing below that.
            var catalystFactor = MathF.Min(1f, catalyst / 5f);

            // Capped by how much Infernazim is actually present, so we never convert gas that isn't there.
            var converted = MathF.Min(mixture.GetMoles(Gas.Infernazim),
                catalyst * ConversionPerSecond * reactionDelta * catalystFactor);

            if (converted > 0f)
            {
                // 1:1 in, 1:1 out on Infernazim, and it burns through Kaltrenoxide first because it prefers it
                // over Oxygen. The mass balance isn't physical by design, it's just a gameplay curve.
                mixture.AdjustMoles(Gas.Infernazim, -converted);
                mixture.AdjustMoles(Gas.WaterVapor, converted);

                var kaltrenConsumed = MathF.Min(kaltrenoxide, converted);
                mixture.AdjustMoles(Gas.Kaltrenoxide, -kaltrenConsumed);

                reacted = true;
            }
        }

        // Passive decay runs regardless of catalyst, so a sealed pocket of Infernazim still burns off.
        var decay = MathF.Min(
            mixture.GetMoles(Gas.Infernazim),
            PassiveDecayPerSecond * reactionDelta);

        if (decay > 0f)
        {
            mixture.AdjustMoles(Gas.Infernazim, -decay);
            reacted = true;
        }

        var infernazimRemaining = mixture.GetMoles(Gas.Infernazim);

        if (infernazimRemaining > 0f && mixture.Temperature < TargetTemperature)
        {
            // Dividing by heat capacity is what makes the approach asymptotic rather than overshooting.
            var heatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);

            if (heatCapacity > Atmospherics.MinimumHeatCapacity)
            {
                mixture.Temperature = MathF.Min(
                    TargetTemperature,
                    mixture.Temperature + EnergyPerSecond * reactionDelta / heatCapacity);

                reacted = true;
            }
        }

        return reacted ? ReactionResult.Reacting : ReactionResult.NoReaction;
    }
}
