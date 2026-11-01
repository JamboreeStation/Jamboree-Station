// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Temperature.Components;

namespace Content.Client.Chemistry.EntitySystems;

/// <inheritdoc/>
public sealed partial class ClientThermobathSystem : ThermobathSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityQuery<ThermobathComponent> _thermobathQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermoregulatorComponent, AfterAutoHandleStateEvent>(OnThermoregulatorState);
        SubscribeLocalEvent<ThermobathComponent, AfterAutoHandleStateEvent>(OnThermobathState);
    }

    private void OnThermoregulatorState(Entity<ThermoregulatorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_thermobathQuery.TryComp(ent, out var thermobath))
            UpdateUi((ent, thermobath));
    }

    private void OnThermobathState(Entity<ThermobathComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    protected override void UpdateUi(Entity<ThermobathComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, ThermobathUiKey.Key, out var bui))
            bui.Update();
    }
}
