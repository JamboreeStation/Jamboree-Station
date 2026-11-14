// SPDX-FileCopyrightText: 2025 Baine Junk <wym0n@proton.me>
// SPDX-FileCopyrightText: 2025 JamboreeBot <JamboreeBot@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Abilities.Psionics;
using Content.Shared._EinsteinEngines.Actions.Events;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map;

namespace Content.Server._EinsteinEngines.Abilities.Psionics;

public sealed partial class AnomalyPowerSystem
{
    private void DoPyroclasticAnomalyEffects(EntityUid uid, PsionicComponent component, AnomalyPowerActionEvent args, bool overcharged = false)
    {
        if (args.Pyroclastic is null)
            return;

        if (overcharged)
            PyroclasticSupercrit(uid, component, args);
        else PyroclasticPulse(uid, component, args);
    }

    private void PyroclasticSupercrit(EntityUid uid, PsionicComponent component, AnomalyPowerActionEvent args)
    {
        var pyroclastic = args.Pyroclastic!.Value;
        var xform = Transform(uid);
        var ignitionRadius = pyroclastic.SupercritMaximumIgnitionRadius * component.CurrentAmplification;
        IgniteNearby(uid, xform.Coordinates, component.CurrentAmplification, ignitionRadius);
    }

    private void PyroclasticPulse(EntityUid uid, PsionicComponent component, AnomalyPowerActionEvent args)
    {
        var pyroclastic = args.Pyroclastic!.Value;
        var xform = Transform(uid);
        var ignitionRadius = pyroclastic.MaximumIgnitionRadius * component.CurrentAmplification;
        IgniteNearby(uid, xform.Coordinates, component.CurrentAmplification, ignitionRadius);
    }

    private void IgniteNearby(EntityUid uid, EntityCoordinates coordinates, float severity, float radius)
    {
        var flammables = new HashSet<Entity<FlammableComponent>>();
        _lookup.GetEntitiesInRange(coordinates, radius, flammables);

        foreach (var flammable in flammables)
        {
            var ent = flammable.Owner;
            var stackAmount = 1 + (int) (severity / 0.15f);
            _flammable.AdjustFireStacks(ent, stackAmount, flammable.Comp);
            _flammable.Ignite(ent, uid, flammable.Comp);
        }
    }
}
