// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Popups;
using Content.Shared._Jamboree.Mutations;
using Content.Shared._Jamboree.Actions.Events;

namespace Content.Server._Jamboree.Abilities.Mutations
{
    public sealed class PyrokinesisMutationSystem : EntitySystem
    {
        [Dependency] private readonly FlammableSystem _flammableSystem = default!;
        [Dependency] private readonly PotentialMutantSystem _mutant = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PyrokinesisMutationActionEvent>(OnPowerUsed);
        }
        private void OnPowerUsed(PyrokinesisMutationActionEvent args)
        {
            if (!_mutant.OnAttemptMutantAbilityUse(args.Performer, args.Target, "pyrokinesis", true))
                return;

            if (!TryComp<FlammableComponent>(args.Target, out var flammableComponent))
                return;

            flammableComponent.FireStacks += 5;
            _flammableSystem.Ignite(args.Target, args.Target);
            _popupSystem.PopupEntity(Loc.GetString("pyrokinesis-power-used", ("target", args.Target)), args.Target, Shared.Popups.PopupType.LargeCaution);

            _mutant.LogAbilityUsed(args.Performer, "pyrokinesis");
            args.Handled = true;
        }
    }
}
