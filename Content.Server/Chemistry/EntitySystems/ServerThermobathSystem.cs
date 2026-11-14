// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ServerThermobathSystem : ThermobathSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ThermoregulatorSystem _thermoregulator = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermobathComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ThermobathComponent, ThermoregulatorControlEvent>(OnThermoregulatorControl);
        SubscribeLocalEvent<ThermobathComponent, ThermoregulatorUpdatedEvent>(OnThermoregulatorUpdated);
        SubscribeLocalEvent<ThermobathComponent, ThermoregulatorActiveModeChangedEvent>(OnThermoregulatorActiveModeChanged);
    }

    private void OnMapInit(Entity<ThermobathComponent> ent, ref MapInitEvent args)
    {
        OnControlChanged(ent);
        UpdateAppearance(ent);
    }

    private void OnThermoregulatorControl(Entity<ThermobathComponent> ent, ref ThermoregulatorControlEvent args)
    {
        UpdateEnergyLimits(ent);
        args.TargetTemperature = ent.Comp.Setpoint;
        args.MinEnergy = ent.Comp.MinEnergy;
        args.MaxEnergy = ent.Comp.MaxEnergy;
    }

    protected override void OnControlChanged(Entity<ThermobathComponent> ent, bool? powered = null)
    {
        UpdateEnergyLimits(ent, powered);
        _thermoregulator.RefreshActiveMode(ent.Owner);
    }

    private void UpdateEnergyLimits(Entity<ThermobathComponent> ent, bool? powered = null)
    {
        ent.Comp.MinEnergy = 0f;
        ent.Comp.MaxEnergy = 0f;

        var regulator = CompOrNull<ThermoregulatorComponent>(ent);
        if (regulator == null || !IsPowered(ent.Owner, powered) || !Enum.IsDefined(ent.Comp.Mode))
            return;

        var dt = (float) regulator.UpdateInterval.TotalSeconds;
        if (dt <= 0f || !float.IsFinite(dt))
            return;

        if (ent.Comp.Mode != ThermobathMode.Heating && float.IsFinite(ent.Comp.CoolingPower))
            ent.Comp.MinEnergy = -Math.Max(0f, ent.Comp.CoolingPower) * dt;

        if (ent.Comp.Mode != ThermobathMode.Cooling && float.IsFinite(ent.Comp.HeatingPower))
            ent.Comp.MaxEnergy = Math.Max(0f, ent.Comp.HeatingPower) * dt;
    }

    private void OnThermoregulatorUpdated(Entity<ThermobathComponent> ent, ref ThermoregulatorUpdatedEvent args)
    {
        var thermoregulator = args.Thermoregulator;
        if (!TryGetSolutionFromContainer(ent, out var soln))
            return;

        var solution = soln.Value.Comp.Solution;
        if (solution.Volume <= 0)
            return;

        var solutionHeatContainer = new HeatContainer(solution.GetHeatCapacity(_proto), solution.Temperature);
        _thermoregulator.ConductHeatWith((ent, thermoregulator), ref solutionHeatContainer);
        _solutionContainer.SetTemperature(soln.Value, solutionHeatContainer.Temperature);
    }

    private void OnThermoregulatorActiveModeChanged(Entity<ThermobathComponent> ent, ref ThermoregulatorActiveModeChangedEvent args)
    {
        UpdateAppearance(ent, args.Thermoregulator);
    }

    private bool TryGetSolutionFromContainer(
        Entity<ThermobathComponent> ent,
        [NotNullWhen(true)] out Entity<SolutionComponent>? soln)
    {
        var beaker = _itemSlots.GetItemOrNull(ent.Owner, ThermobathComponent.BeakerSlotId);
        if (beaker != null)
            return _solutionContainer.TryGetFitsInDispenser(beaker.Value, out soln, out _);

        soln = null;
        return false;
    }
}
