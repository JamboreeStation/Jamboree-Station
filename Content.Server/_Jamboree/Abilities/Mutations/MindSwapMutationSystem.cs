// SPDX-FileCopyrightText: 2025 Baine Junk <wym0n@proton.me>
// SPDX-FileCopyrightText: 2025 JamboreeBot <JamboreeBot@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Speech;
using Content.Shared.Stealth.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Damage;
using Content.Server.Mind;
using Content.Shared.Mobs.Systems;
using Content.Server.Popups;
using Content.Shared._EinsteinEngines.Psionics;
using Content.Shared.Mind;
using Content.Server._EinsteinEngines.Abilities.Psionics;
using Content.Shared._Jamboree.Actions.Events;
using Content.Shared._Jamboree.Mutations;

namespace Content.Server._Jamboree.Abilities.Mutations
{
    public sealed class MindSwapMutationSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly PotentialMutantSystem _mutant = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MindSwapMutationActionEvent>(OnPowerUsed);
            SubscribeLocalEvent<MindSwappedComponent, MindSwapMutationReturnActionEvent>(OnPowerReturned);
            // SubscribeLocalEvent<MindSwappedComponent, DispelledEvent>(OnDispelled);
            // SubscribeLocalEvent<MindSwappedComponent, MobStateChangedEvent>(OnMobStateChanged);
            // SubscribeLocalEvent<GhostAttemptHandleEvent>(OnGhostAttempt);
            //
            // SubscribeLocalEvent<MindSwappedComponent, ComponentInit>(OnSwapInit);
        }

        private void OnPowerUsed(MindSwapMutationActionEvent args)
        {
            if (!_mutant.OnAttemptMutantAbilityUse(args.Performer, args.Target, "mind swap", true)
                || !(TryComp<DamageableComponent>(args.Target, out var damageable) && damageable.DamageContainerID == "Biological"))
                return;

            Swap(args.Performer, args.Target);

            _mutant.LogAbilityUsed(args.Performer, "mind swap");
            args.Handled = true;
        }

        private void OnPowerReturned(EntityUid uid, MindSwappedComponent component, MindSwapMutationReturnActionEvent args)
        {
            if (HasComp<MobStateComponent>(uid) && !_mobStateSystem.IsAlive(uid))
                return;

            // How do we get trapped?
            // 1. Original target doesn't exist
            if (!component.OriginalEntity.IsValid() || Deleted(component.OriginalEntity))
            {
                GetTrapped(uid);
                return;
            }
            // 1. Original target is no longer mindswapped
            if (!TryComp<MindSwappedComponent>(component.OriginalEntity, out var targetMindSwap))
            {
                GetTrapped(uid);
                return;
            }

            // 2. Target has undergone a different mind swap
            if (targetMindSwap.OriginalEntity != uid)
            {
                GetTrapped(uid);
                return;
            }

            // 3. Target is dead
            if (HasComp<MobStateComponent>(component.OriginalEntity) && _mobStateSystem.IsDead(component.OriginalEntity))
            {
                GetTrapped(uid);
                return;
            }

            Swap(uid, component.OriginalEntity, true);
        }

        public void Swap(EntityUid performer, EntityUid target, bool end = false)
        {
            if (!Exists(performer) || !Exists(target)
                || end && (!HasComp<MindSwappedComponent>(performer) || !HasComp<MindSwappedComponent>(target)))
                return;

            // Get the minds first. On transfer, they'll be gone.
            MindComponent? performerMind = null;
            MindComponent? targetMind = null;

            // This is here to prevent missing MindContainerComponent Resolve errors.
            if (!_mindSystem.TryGetMind(performer, out var performerMindId, out performerMind))
                performerMind = null;

            if (!_mindSystem.TryGetMind(target, out var targetMindId, out targetMind))
                targetMind = null;

            //This is a terrible way to 'unattach' minds. I wanted to use UnVisit but in TransferTo's code they say
            //To unnatch the minds, do it like this.
            //Have to unnattach the minds before we reattach them via transfer. Still feels weird, but seems to work well.
            _mindSystem.TransferTo(performerMindId, null);
            // Do the transfer.
            if (targetMind != null)
                _mindSystem.TransferTo(targetMindId, performer, ghostCheckOverride: true, false, targetMind);

            if (performerMind != null)
                _mindSystem.TransferTo(performerMindId, target, ghostCheckOverride: true, false, performerMind);

            if (end)
            {
                var performerMindPowerComp = Comp<MindSwappedComponent>(performer);
                var targetMindPowerComp = Comp<MindSwappedComponent>(target);
                _actions.RemoveAction(performer, performerMindPowerComp.MindSwapReturnActionEntity);
                _actions.RemoveAction(target, targetMindPowerComp.MindSwapReturnActionEntity);

                RemComp<MindSwappedComponent>(performer);
                RemComp<MindSwappedComponent>(target);
                return;
            }

            var perfComp = EnsureComp<MindSwappedComponent>(performer);
            var targetComp = EnsureComp<MindSwappedComponent>(target);

            perfComp.OriginalEntity = target;
            targetComp.OriginalEntity = performer;
        }

        public void GetTrapped(EntityUid uid)
        {

            _popupSystem.PopupEntity(Loc.GetString("mindswap-trapped"), uid, uid, Shared.Popups.PopupType.LargeCaution);
            var perfComp = EnsureComp<MindSwappedComponent>(uid);
            _actions.RemoveAction(perfComp.MindSwapReturnActionEntity);

            if (HasComp<TelegnosticProjectionComponent>(uid))
            {
                RemComp<PsionicallyInvisibleComponent>(uid);
                RemComp<StealthComponent>(uid);
                EnsureComp<SpeechComponent>(uid);
                EnsureComp<DispellableComponent>(uid);
                _metaDataSystem.SetEntityName(uid, Loc.GetString("telegnostic-trapped-entity-name"));
                _metaDataSystem.SetEntityDescription(uid, Loc.GetString("telegnostic-trapped-entity-desc"));
            }
        }
    }
}
