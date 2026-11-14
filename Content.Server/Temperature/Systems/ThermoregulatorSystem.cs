// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Robust.Shared.Timing;

namespace Content.Server.Temperature.Systems;

public sealed partial class ThermoregulatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermoregulatorComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ThermoregulatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (curTime < comp.NextUpdate || comp.UpdateInterval <= TimeSpan.Zero)
                continue;

            UpdateThermoregulator((uid, comp));
        }
    }

    private void OnMapInit(Entity<ThermoregulatorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
    }

    private void UpdateThermoregulator(Entity<ThermoregulatorComponent> ent)
    {
        var control = GetControl(ent);
        var originalTemperature = ent.Comp.Temperature;
        if (float.IsFinite(ent.Comp.HeatCapacity) && ent.Comp.HeatCapacity > 0f &&
            float.IsFinite(originalTemperature) && originalTemperature >= 0f)
        {
            var energyToSetpoint = ((double) control.TargetTemperature - originalTemperature) * ent.Comp.HeatCapacity;
            var energy = Math.Clamp(energyToSetpoint, control.MinEnergy, control.MaxEnergy);

            // Assign the target exactly when reachable; float addition can otherwise overshoot it.
            ent.Comp.Temperature = energy == energyToSetpoint
                ? control.TargetTemperature
                : (float) (originalTemperature + energy / ent.Comp.HeatCapacity);
        }

        ent.Comp.NextUpdate += ent.Comp.UpdateInterval;

        var ev = new ThermoregulatorUpdatedEvent(ent.Comp);
        RaiseLocalEvent(ent, ref ev);

        UpdateActiveMode(ent, control);

        if (originalTemperature != ent.Comp.Temperature)
            DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.Temperature));
    }

    private static ThermoregulatorActiveMode GetActiveMode(
        ThermoregulatorComponent comp,
        ThermoregulatorControlEvent control,
        ThermoregulatorActiveMode previousMode)
    {
        if (!float.IsFinite(comp.HeatCapacity) || comp.HeatCapacity <= 0f ||
            !float.IsFinite(comp.Temperature) || comp.Temperature < 0f)
            return ThermoregulatorActiveMode.Idle;

        var difference = control.TargetTemperature - comp.Temperature;
        var canHeat = control.MaxEnergy > 0f;
        var canCool = control.MinEnergy < 0f;

        if (previousMode == ThermoregulatorActiveMode.Heating && canHeat && difference > 0f)
            return ThermoregulatorActiveMode.Heating;

        if (previousMode == ThermoregulatorActiveMode.Cooling && canCool && difference < 0f)
            return ThermoregulatorActiveMode.Cooling;

        if (canHeat && difference > comp.TemperatureTolerance)
            return ThermoregulatorActiveMode.Heating;

        if (canCool && difference < -comp.TemperatureTolerance)
            return ThermoregulatorActiveMode.Cooling;

        return ThermoregulatorActiveMode.Idle;
    }

    private void SetActiveMode(Entity<ThermoregulatorComponent> ent, ThermoregulatorActiveMode mode)
    {
        if (ent.Comp.ActiveMode == mode)
            return;

        ent.Comp.ActiveMode = mode;
        DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.ActiveMode));

        var ev = new ThermoregulatorActiveModeChangedEvent(ent.Comp);
        RaiseLocalEvent(ent, ref ev);
    }

    private ThermoregulatorControlEvent GetControl(Entity<ThermoregulatorComponent> ent)
    {
        var control = new ThermoregulatorControlEvent(ent.Comp.Temperature);
        RaiseLocalEvent(ent, ref control);

        if (!float.IsFinite(control.TargetTemperature) || control.TargetTemperature < 0f)
            return new ThermoregulatorControlEvent(ent.Comp.Temperature);

        if (!float.IsFinite(control.MinEnergy) || control.MinEnergy > 0f)
            control.MinEnergy = 0f;

        if (!float.IsFinite(control.MaxEnergy) || control.MaxEnergy < 0f)
            control.MaxEnergy = 0f;

        return control;
    }

    /// <summary>
    /// Recalculates the active mode after the target or energy limits change.
    /// </summary>
    public void RefreshActiveMode(Entity<ThermoregulatorComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var regulator = (ent.Owner, ent.Comp);
        UpdateActiveMode(regulator, GetControl(regulator));
    }

    private void UpdateActiveMode(Entity<ThermoregulatorComponent> ent, ThermoregulatorControlEvent control)
    {
        SetActiveMode(ent, GetActiveMode(ent.Comp, control, ent.Comp.ActiveMode));
    }

    /// <summary>
    /// Conducts heat between the thermoregulator and another heat container.
    /// </summary>
    public void ConductHeatWith(
        Entity<ThermoregulatorComponent?> ent,
        ref HeatContainer otherHeatContainer)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var capacity = ent.Comp.HeatCapacity;
        var otherCapacity = otherHeatContainer.HeatCapacity;
        var temperature = ent.Comp.Temperature;
        var otherTemperature = otherHeatContainer.Temperature;
        var conductance = ent.Comp.ThermalConductance;
        var dt = ent.Comp.UpdateInterval.TotalSeconds;
        if (!float.IsFinite(capacity) || capacity <= 0f ||
            !float.IsFinite(otherCapacity) || otherCapacity <= 0f ||
            !float.IsFinite(temperature) || temperature < 0f ||
            !float.IsFinite(otherTemperature) || otherTemperature < 0f ||
            !float.IsFinite(conductance) || conductance <= 0f || dt <= 0d)
            return;

        // Both finite bodies change temperature. The fork's helper only clamps against the first body.
        var difference = (double) otherTemperature - temperature;
        var equilibriumEnergy = difference / (1d / capacity + 1d / otherCapacity);
        var limit = Math.Abs(equilibriumEnergy);
        var energy = Math.Clamp(conductance * difference * dt, -limit, limit);
        if (energy == equilibriumEnergy)
        {
            var equilibriumTemperature = (float) (((double) capacity * temperature + (double) otherCapacity * otherTemperature) /
                                                  ((double) capacity + otherCapacity));
            ent.Comp.Temperature = equilibriumTemperature;
            otherHeatContainer = new HeatContainer(otherCapacity, equilibriumTemperature);
        }
        else
        {
            ent.Comp.Temperature = (float) (temperature + energy / capacity);
            otherHeatContainer = new HeatContainer(otherCapacity, (float) (otherTemperature - energy / otherCapacity));
        }

        if (temperature != ent.Comp.Temperature)
            DirtyField(ent, nameof(ThermoregulatorComponent.Temperature));
    }
}

[ByRefEvent]
public readonly record struct ThermoregulatorUpdatedEvent(ThermoregulatorComponent Thermoregulator);

[ByRefEvent]
public readonly record struct ThermoregulatorActiveModeChangedEvent(ThermoregulatorComponent Thermoregulator);

/// <summary>
/// Supplies the target temperature and energy limits in joules per regulator update.
/// The default limits keep the regulator idle.
/// </summary>
[ByRefEvent]
public record struct ThermoregulatorControlEvent(float TargetTemperature, float MinEnergy = 0f, float MaxEnergy = 0f);
