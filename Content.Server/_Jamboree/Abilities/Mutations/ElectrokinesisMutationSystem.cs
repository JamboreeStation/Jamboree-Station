// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffect;
using Content.Server.Stunnable;
using Content.Server.Beam;
using Content.Shared._Jamboree.Mutations;
using Content.Shared._Jamboree.Actions.Events;

namespace Content.Server._Jamboree.Abilities.Mutations
{
    public sealed class ElectrokinesisPowerSystem : EntitySystem
    {
        [Dependency] private readonly PotentialMutantSystem _mutant = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly BeamSystem _beam = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ElectrokinesisMutationActionEvent>(OnPowerUsed);
        }

        private void OnPowerUsed(ElectrokinesisMutationActionEvent args)
        {
            if (!_mutant.OnAttemptMutantAbilityUse(args.Performer, args.Target, "electrokinesis", true))
                return;

            _beam.TryCreateBeam(args.Performer, args.Target, "LightningNoospheric");

            _stunSystem.TryParalyze(args.Target, TimeSpan.FromSeconds(5), false);
            _statusEffectsSystem.TryAddStatusEffect(args.Target, "Stutter", TimeSpan.FromSeconds(10), false, "StutteringAccent");

            _mutant.LogAbilityUsed(args.Performer, "electrokinesis");
            args.Handled = true;
        }
    }
}
