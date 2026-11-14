// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Temperature.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Handles thermobath settings, UI messages and appearance data.
/// </summary>
public abstract partial class ThermobathSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermobathComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ThermobathComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<ThermobathComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
        SubscribeLocalEvent<ThermobathComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ThermobathComponent, ThermobathPowerChangedMessage>(OnPowerChangeMessage);
        SubscribeLocalEvent<ThermobathComponent, ThermobathSetpointChangedMessage>(OnSetpointChangeMessage);
        SubscribeLocalEvent<ThermobathComponent, ThermobathModeChangedMessage>(OnModeChangeMessage);
    }

    [PublicAPI]
    public void SetSetpoint(Entity<ThermobathComponent?> ent, float setpoint)
    {
        if (!float.IsFinite(setpoint) || !Resolve(ent, ref ent.Comp))
            return;

        var bath = ent.Comp;
        var clampedSetpoint = Math.Clamp(setpoint, bath.MinTemperature, bath.MaxTemperature);
        if (MathHelper.CloseTo(bath.Setpoint, clampedSetpoint))
            return;

        bath.Setpoint = clampedSetpoint;
        DirtyField(ent, nameof(ThermobathComponent.Setpoint));
        OnControlChanged((ent.Owner, bath));
    }

    [PublicAPI]
    public void SetMode(Entity<ThermobathComponent?> ent, ThermobathMode mode)
    {
        if (!Enum.IsDefined(mode) || !Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Mode == mode)
            return;

        ent.Comp.Mode = mode;
        DirtyField(ent, nameof(ThermobathComponent.Mode));
        OnControlChanged((ent.Owner, ent.Comp));
    }

    protected virtual void OnControlChanged(Entity<ThermobathComponent> ent, bool? powered = null) { }

    private void OnStartup(Entity<ThermobathComponent> ent, ref ComponentStartup args)
    {
        UpdateState(ent);
    }

    private void OnEntInsertedIntoContainer(Entity<ThermobathComponent> ent, ref EntInsertedIntoContainerMessage args) =>
        OnContainerModified(ent, args);

    private void OnEntRemovedFromContainer(Entity<ThermobathComponent> ent, ref EntRemovedFromContainerMessage args) =>
        OnContainerModified(ent, args);

    private void OnContainerModified(Entity<ThermobathComponent> ent, ContainerModifiedMessage args)
    {
        if (args.Container.ID != ThermobathComponent.BeakerSlotId)
            return;

        if (_timing.ApplyingState)
        {
            UpdateUi(ent);
            return;
        }

        UpdateState(ent);
    }

    private void OnPowerChanged(Entity<ThermobathComponent> ent, ref PowerChangedEvent args)
    {
        if (_timing.ApplyingState)
        {
            UpdateUi(ent);
            return;
        }

        OnControlChanged(ent, args.Powered);
        UpdateState(ent, powered: args.Powered);
    }

    private void OnPowerChangeMessage(Entity<ThermobathComponent> ent, ref ThermobathPowerChangedMessage args)
    {
        SharedApcPowerReceiverComponent? receiver = null;
        if (!_power.ResolveApc(ent, ref receiver) || !receiver.NeedsPower)
            return;

        var currentEnabled = !receiver.PowerDisabled;
        if (currentEnabled == args.Enabled)
            return;

        _power.TogglePower(ent, receiver: receiver, user: args.Actor);
        var powered = args.Enabled && receiver.Powered;

        OnControlChanged(ent, powered);
        UpdateState(ent, powered: powered);
    }

    private void OnSetpointChangeMessage(Entity<ThermobathComponent> ent, ref ThermobathSetpointChangedMessage args)
    {
        SetSetpoint(ent.AsNullable(), args.Setpoint);
        UpdateUi(ent);
    }

    private void OnModeChangeMessage(Entity<ThermobathComponent> ent, ref ThermobathModeChangedMessage args)
    {
        SetMode(ent.AsNullable(), args.Mode);
        UpdateUi(ent);
    }

    private bool HasBeaker(EntityUid uid) =>
        _itemSlots.GetItemOrNull(uid, ThermobathComponent.BeakerSlotId) != null;

    // Powered can lag behind the switch until the power network's next update in this fork.
    protected bool IsPowered(EntityUid uid, bool? powered = null)
    {
        SharedApcPowerReceiverComponent? receiver = null;
        if (!_power.ResolveApc(uid, ref receiver))
            return powered ?? true;

        return !receiver.PowerDisabled && (powered ?? receiver.Powered);
    }

    private void UpdateState(Entity<ThermobathComponent> ent, bool? powered = null)
    {
        UpdateUi(ent);
        UpdateAppearance(ent, powered: powered);
    }

    protected void UpdateAppearance(
        Entity<ThermobathComponent> ent,
        ThermoregulatorComponent? thermoregulator = null,
        bool? powered = null)
    {
        var isPowered = IsPowered(ent.Owner, powered);
        thermoregulator ??= CompOrNull<ThermoregulatorComponent>(ent);

        var activeMode = isPowered
            ? thermoregulator?.ActiveMode ?? ThermoregulatorActiveMode.Idle
            : ThermoregulatorActiveMode.Idle;

        _appearance.SetData(ent, ThermobathVisuals.Powered, isPowered);
        _appearance.SetData(ent, ThermobathVisuals.HasBeaker, HasBeaker(ent));
        _appearance.SetData(ent, ThermobathVisuals.ActiveMode, activeMode);
    }

    protected virtual void UpdateUi(Entity<ThermobathComponent> ent) { }
}
