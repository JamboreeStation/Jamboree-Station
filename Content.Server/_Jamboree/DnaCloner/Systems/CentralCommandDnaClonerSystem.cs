// SPDX-FileCopyrightText: 2026 PureBreadBagel
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server._Jamboree.DnaCloner.Components;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.GameTicking;
using Content.Server.Materials;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.Traits;
using Content.Shared._Jamboree.DnaCloner;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.DetailExaminable;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Server.Ghost;
using Content.Shared.Roles.Components;
using Content.Server.Ghost.Roles;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Roles;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility; //JAMBOREE API SLOP PART 1

namespace Content.Server._Jamboree.DnaCloner.Systems
{

    // JAMBOREE - Central Command's DNA Molecularsynthesis console. Lists every player who has been round
    // removed so they can get a body back.

    public sealed class CentralCommandDnaClonerSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly MetaDataSystem _metaSystem = default!;
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _power = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly SharedRottingSystem _rotting = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly StationSpawningSystem _spawning = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly MaterialStorageSystem _material = default!;
        [Dependency] private readonly TraitSystem _traits = default!;
        [Dependency] private readonly SharedRoleSystem _roles = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ILogManager _log = default!; //JAMBOREE  I HATE API SLOP!!!!
        [Dependency] private readonly IGameTiming _timing = default!;
        private ISawmill _sawmill = default!;

        // How long the body stays in the vat once they are getting synthd.
        public static readonly TimeSpan SynthesisTime = TimeSpan.FromSeconds(10);

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = _log.GetSawmill("CentralCommandDnaCloner");

            SubscribeLocalEvent<CentralCommandDnaClonerConsoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<CentralCommandDnaClonerConsoleComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<CentralCommandDnaClonerConsoleComponent, AfterActivatableUIOpenEvent>(OnUIOpen);
            SubscribeLocalEvent<CentralCommandDnaClonerConsoleComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<CentralCommandDnaClonerConsoleComponent, NewLinkEvent>(OnNewLink);
            SubscribeLocalEvent<CentralCommandDnaClonerConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<CentralCommandDnaClonerConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<CentralCommandDnaClonerConsoleComponent, CentralCommandDnaClonerRespawnMessage>(OnRespawnRequested);

            SubscribeLocalEvent<CentralCommandDnaClonerVatComponent, ComponentInit>(OnVatInit);
            SubscribeLocalEvent<CentralCommandDnaClonerVatComponent, AnchorStateChangedEvent>(OnVatAnchorChanged);
            SubscribeLocalEvent<CentralCommandDnaClonerVatComponent, ExaminedEvent>(OnVatExamined);
            SubscribeLocalEvent<CentralCommandDnaClonerVatComponent, EntInsertedIntoContainerMessage>(OnVatInserted);
            SubscribeLocalEvent<CentralCommandDnaClonerVatComponent, EntRemovedFromContainerMessage>(OnVatRemoved);
        } // JAMBOREE Even more API slop. TODO: Make these easier to read.

        #region Setup

        private void OnInit(EntityUid uid, CentralCommandDnaClonerConsoleComponent component, ComponentInit args)
        {
            _signalSystem.EnsureSourcePorts(uid, CentralCommandDnaClonerConsoleComponent.PodPort);
        }

        private void OnVatInit(EntityUid uid, CentralCommandDnaClonerVatComponent component, ComponentInit args)
        {
            _signalSystem.EnsureSinkPorts(uid, component.VatPort);

            // JAMBOREE - Give the visualizer a definite starting value.
            UpdateVatVisual(uid, component);
        }

        private void OnMapInit(EntityUid uid, CentralCommandDnaClonerConsoleComponent component, MapInitEvent args)
        {
            if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
                return;  // this is just for maps. if it already has connections in a map yml then preload them on intialisation.

            foreach (var port in source.Outputs.Values.SelectMany(ports => ports))
            {
                if (TryComp<CentralCommandDnaClonerVatComponent>(port, out var vat))
                {
                    component.DnaClonerVat = port;
                    vat.ConnectedConsole = uid;
                }
            }

            RecheckConnections(uid, component);
        }

        private void OnNewLink(EntityUid uid, CentralCommandDnaClonerConsoleComponent component, NewLinkEvent args)
        {
            if (args.SourcePort != CentralCommandDnaClonerConsoleComponent.PodPort)
                return;

            if (!TryComp<CentralCommandDnaClonerVatComponent>(args.Sink, out var vat))
                return;

            component.DnaClonerVat = args.Sink;
            vat.ConnectedConsole = uid;
            RecheckConnections(uid, component);
        }

        private void OnPortDisconnected(EntityUid uid, CentralCommandDnaClonerConsoleComponent component, PortDisconnectedEvent args)
        {
            if (args.Port != CentralCommandDnaClonerConsoleComponent.PodPort)
                return;

            if (component.DnaClonerVat is { } oldVat
                && TryComp<CentralCommandDnaClonerVatComponent>(oldVat, out var oldVatComp))
            {
                oldVatComp.ConnectedConsole = null;
            }

            component.DnaClonerVat = null;
            UpdateUserInterface(uid, component);
        }

        private void OnAnchorChanged(EntityUid uid, CentralCommandDnaClonerConsoleComponent component, ref AnchorStateChangedEvent args)
        {
            RecheckConnections(uid, component);
        }

        private void OnVatAnchorChanged(EntityUid uid, CentralCommandDnaClonerVatComponent component, ref AnchorStateChangedEvent args)
        {
            if (component.ConnectedConsole is not { } console)
                return;

            RecheckConnections(console);
        }

        #endregion

        #region User interface

        private void OnUIOpen(EntityUid uid, CentralCommandDnaClonerConsoleComponent component, AfterActivatableUIOpenEvent args)
        {
            UpdateUserInterface(uid, component);


            Speak(uid, $"centralcommand-dnacloner-status-{GetStatus(component)}");
        }

        private void OnPowerChanged(EntityUid uid, CentralCommandDnaClonerConsoleComponent component, ref PowerChangedEvent args)
        {
            UpdateUserInterface(uid, component);
        } // basically update the UI if power goes on or off.

        private void OnVatExamined(EntityUid uid, CentralCommandDnaClonerVatComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            args.PushMarkup(Loc.GetString("centralcommand-dnacloner-examine-biomass",
                ("number", GetBiomass(uid, component)),
                ("cost", component.MaterialCost)));
        }

        private void OnVatInserted(EntityUid uid, CentralCommandDnaClonerVatComponent component, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID != component.ContainerId)
                return;

            UpdateVatVisual(uid, component);

            if (component.ConnectedConsole is { } console)
                UpdateUserInterface(console);
        }

        private void OnVatRemoved(EntityUid uid, CentralCommandDnaClonerVatComponent component, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID != component.ContainerId)
                return;

            UpdateVatVisual(uid, component);

            if (component.ConnectedConsole is { } console)
                UpdateUserInterface(console);
        }

        // Swaps the vat sprite so a busy machine reads as busy on the map, not just in the UI.
        private void UpdateVatVisual(EntityUid uid, CentralCommandDnaClonerVatComponent component)
        {
            var occupied = TryGetVatContainer(uid, out var container) && container.ContainedEntity != null;
            var status = occupied
                ? CentralCommandDnaClonerVatStatus.Occupied
                : CentralCommandDnaClonerVatStatus.Idle; // we have two sprites, one for nothing in it and one for SYNTHESISING BODY

            _appearance.SetData(uid, CentralCommandDnaClonerVisuals.Status, status);
        }

        public void UpdateUserInterface(EntityUid uid, CentralCommandDnaClonerConsoleComponent? component = null)
        {
            if (!_uiSystem.HasUi(uid, CentralCommandDnaClonerUiKey.Key))
                return;

            if (!Resolve(uid, ref component))
                return;

            var newState = new CentralCommandDnaClonerConsoleBoundUserInterfaceState
            {
                MindNames = GetRespawnableMinds(component.RottenBodyStage),
                Status = GetStatus(component),
            };

            if (component.DnaClonerVat is { } vat
                && TryComp<CentralCommandDnaClonerVatComponent>(vat, out var vatComp))
            {
                newState.Biomass = GetBiomass(vat, vatComp);
                newState.BiomassCost = vatComp.MaterialCost;
            }

            _uiSystem.SetUiState(uid, CentralCommandDnaClonerUiKey.Key, newState);
        }

        private CentralCommandDnaClonerStatus GetStatus(CentralCommandDnaClonerConsoleComponent component)
        {
            if (component.DnaClonerVat is not { } vat || Deleted(vat))
                return CentralCommandDnaClonerStatus.NoVat;

            if (!component.DnaClonerVatInRange || !Transform(vat).Anchored)
                return CentralCommandDnaClonerStatus.VatOutOfRange;

            if (!TryGetVatContainer(vat, out var container) || container.ContainedEntity != null)
                return CentralCommandDnaClonerStatus.VatOccupied;

            if (TryComp<CentralCommandDnaClonerVatComponent>(vat, out var vatComp)
                && GetBiomass(vat, vatComp) < vatComp.MaterialCost)
            {
                return CentralCommandDnaClonerStatus.InsufficientBiomass;
            }

            return CentralCommandDnaClonerStatus.Ready;
        }

        #endregion

        #region Eligibility

        // JAMBOREE - Who the console is allowed to synthesise a body for. Anything that still has a
        // usable body, is already walking around, or was a deliberate role pick is off the menu.
        private bool IsRespawnable(MindComponent mind, MindSituation situation)
        {

            if (mind.CharacterName == null
                || mind.UserId == null
                || !_players.ValidSessionId(mind.UserId.Value))
            {
                return false;
            }

            // A player can have more than one mind i found out... so only ever list the one they're actually using.
            if (!_mind.TryGetMind(mind.UserId.Value, out var activeMindId, out var activeMind)
                || activeMind != mind)
            {
                return false;
            }

            // JAMBOREE Took a ghost role (MothRoach, etc). The profile won't rebuild that body for us,
            // so synthesising them would hand back their normal character instead. Leave em be.
            if (_roles.MindHasRole<GhostRoleMarkerRoleComponent>(activeMindId.Value))
                return false;

            return situation switch
            {
                // Still walking around in a body. There's nothing to fix.
                MindSituation.LiveBody => false,

                // Ghosts that can still pop back into a body don't need us, only the properly stuck ones do.
                MindSituation.GhostOwned => IsStuckGhost(mind.OwnedEntity),

                // Round removed, rotted out, or never had one. Their ghost is what we'd be reviving.
                MindSituation.BodyDeleted or MindSituation.BodyRotten or MindSituation.NoBody =>
                    IsReturnableGhost(mind.VisitingEntity),


                _ => false,
            };
        }

        private bool IsObserver(EntityUid mindEntity)
        {
            return HasComp<ObserverRoleComponent>(mindEntity);
        }



        private List<CentralCommandDnaClonerMindRow> GetRespawnableMinds(int rottenBodyStage)
        {
            var nameQuery = EntityQueryEnumerator<MindComponent>();
            var names = new List<CentralCommandDnaClonerMindRow>();

            while (nameQuery.MoveNext(out var mindEntity, out var mindComp))
            {
                var situation = CheckHowMindGotRemoved(mindComp, rottenBodyStage);

                if (!IsRespawnable(mindComp, situation))
                    continue;
                // JAMBOREE the EntityUid is translated into a NetEntity so the client can send it back on click.
                names.Add(new CentralCommandDnaClonerMindRow(
                    GetNetEntity(mindEntity),
                    mindComp.CharacterName!,
                    GetBodyState(situation),
                    IsObserver(mindEntity)));
            }

            names.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            _sawmill.Debug($"Found {names.Count} candidate minds.");
            return names;
        }


        private int GetBiomass(EntityUid vat, CentralCommandDnaClonerVatComponent comp)
        {
            return _material.GetMaterialAmount(vat, comp.RequiredMaterial);
        }


        // JAMBOREE the whole class that checks if they are round removed or rotting.
        private bool IsRespawnable(MindComponent mind, int rottenBodyStage)
        {
            return IsRespawnable(mind, CheckHowMindGotRemoved(mind, rottenBodyStage));
        }


        private MindSituation CheckHowMindGotRemoved(MindComponent mind, int rottenBodyStage)
        {
            if (mind.OwnedEntity is not { } owned)
                return MindSituation.NoBody;

            if (HasComp<GhostComponent>(owned))
                return MindSituation.GhostOwned;

            if (Deleted(owned))
                return MindSituation.BodyDeleted;

            return IsTheirBodyRotting(owned, rottenBodyStage)
                ? MindSituation.BodyRotten
                : MindSituation.LiveBody;
        }


        private bool IsReturnableGhost(EntityUid? uid)
        {
            if (uid is not { } ghostUid)
                return false;

            if (!TryComp<GhostComponent>(ghostUid, out var ghost))
                return false;

            return ghost.CanReturnToBody && !ghost.CanGhostInteract;
        }

        // JAMBOREE - The other half of the ghost check: owns its own ghost but can't get back into a body.
        // CanGhostInteract is specifically for aGhosts, and admins no like being DNA molecularsynthesised.
        private bool IsStuckGhost(EntityUid? uid)
        {
            if (uid is not { } ghostUid)
                return false;

            if (!TryComp<GhostComponent>(ghostUid, out var ghost))
                return false;

            return !ghost.CanReturnToBody && !ghost.CanGhostInteract;
        }

        // JAMBOREE - Whether the corpse is rotted far enough to count. A stage below zero turns the check off.
        private bool IsTheirBodyRotting(EntityUid body, int stage)
        {
            if (stage < 0 || !_rotting.IsRotten(body))
                return false;

            return _rotting.RotStage(body) >= stage;
        }

        // JAMBOREE - Picks the bracket the client shows on the list. Rotted and lost read differently, everything
        // else falls through as intact.
        private CentralCommandDnaClonerBodyState GetBodyState(MindSituation situation)
        {
            return situation switch
            {
                MindSituation.BodyRotten => CentralCommandDnaClonerBodyState.Rotted,
                MindSituation.BodyDeleted => CentralCommandDnaClonerBodyState.Lost,

                _ => CentralCommandDnaClonerBodyState.Intact,
            };
        }

        // Just my way of untangling the eligibility switch above. Separates the scenarios so each one reads on its own.
        private enum MindSituation
        {
            Unknown = 0,
            NoBody,
            BodyDeleted,
            BodyRotten,
            LiveBody,
            GhostOwned,
        }

        #endregion

        #region Respawning


        // JAMBOREE - This is where the body actually gets made. Every guard re-checks eligibility and machine
        // state up front, because the list the player clicked can be stale by the time they hit the button.
        private void OnRespawnRequested(EntityUid uid, CentralCommandDnaClonerConsoleComponent component, CentralCommandDnaClonerRespawnMessage args)
        {
            var mind = GetEntity(args.Mind);
            if (!TryComp<MindComponent>(mind, out var mindComp) || !IsRespawnable(mindComp, component.RottenBodyStage))
            {
                Speak(uid, "centralcommand-dnacloner-invalid-target");
                UpdateUserInterface(uid, component);
                return;
            }

            if (component.DnaClonerVat is not { } vat)
            {
                Speak(uid, "centralcommand-dnacloner-unavailable", CentralCommandDnaClonerStatus.NoVat);
                UpdateUserInterface(uid, component);
                return;
            }

            if (!TryComp<CentralCommandDnaClonerVatComponent>(vat, out var vatComp))
            {
                Speak(uid, "centralcommand-dnacloner-unavailable", GetStatus(component));
                UpdateUserInterface(uid, component);
                return;
            }

            if (!_power.IsPowered(uid))
            {
                Speak(uid, "centralcommand-dnacloner-unpowered");
                UpdateUserInterface(uid, component);
                return;
            }

            var status = GetStatus(component);
            if (status != CentralCommandDnaClonerStatus.Ready)
            {
                Speak(uid, "centralcommand-dnacloner-unavailable", status);
                UpdateUserInterface(uid, component);
                return;
            }

            if (!TryGetVatContainer(vat, out var container))
            {
                Speak(uid, "centralcommand-dnacloner-failed");
                UpdateUserInterface(uid, component);
                return;
            }

            if (GetBiomass(vat, vatComp) < vatComp.MaterialCost)
            {
                Speak(uid, "centralcommand-dnacloner-unavailable", CentralCommandDnaClonerStatus.InsufficientBiomass);
                UpdateUserInterface(uid, component);
                return;
            }

            var success = GetBodyState(CheckHowMindGotRemoved(mindComp, component.RottenBodyStage)) switch
            {
                CentralCommandDnaClonerBodyState.Lost => "centralcommand-dnacloner-success-recovered",
                CentralCommandDnaClonerBodyState.Rotted => "centralcommand-dnacloner-success-rotted",
                _ => "centralcommand-dnacloner-success",
            };

            if (!TryToGetThierBodyBack(mind, mindComp, vat, vatComp, container, out var body))
            {
                Speak(uid, "centralcommand-dnacloner-failed");
                UpdateUserInterface(uid, component);
                return;
            }

            _adminLogger.Add(LogType.Mind, LogImpact.High,
                $"{ToPrettyString(uid):entity} synthesized {mindComp.CharacterName:characterName} " +
                $"{ToPrettyString(mind):mind} into {ToPrettyString(body.Value):body}.");

            Speak(uid, success, name: mindComp.CharacterName!);
            UpdateUserInterface(uid, component);
        }

        // JAMBOREE - The console talks in its own voice. Kept off the public chat log so a busy CC doesn't
        // spam everyone with every failed synthesis.
        private void Speak(EntityUid uid, string key, CentralCommandDnaClonerStatus? status = null, string? name = null)
        {
            var message = Loc.GetString(key,
                ("reason", status is null ? string.Empty : Loc.GetString($"centralcommand-dnacloner-status-{status.Value}")),
                ("name", name ?? string.Empty));

            _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, hideChat: true);
        }


        // JAMBOREE - Builds the body off the player's profile, then hands the mind over. Nothing here reads the
        // old corpse, because by this point it's already gone, so DNA and fingerprints are freshly rolled.

        private bool TryToGetThierBodyBack(
            EntityUid mindUid,
            MindComponent mindComp,
            EntityUid vat,
            CentralCommandDnaClonerVatComponent vatComp,
            ContainerSlot container,
            [NotNullWhen(true)] out EntityUid? body)
        {
            body = null;

            if (mindComp.UserId is not { } userId || !_players.TryGetSessionById(userId, out var session))
                return false;

            // The body comes from the profile, but the name comes from the mind, which is what the list showed them.
            var profile = _gameTicker.GetPlayerProfile(session);
            var station = _station.GetOwningStation(vat) ?? _station.GetStations().FirstOrNull();
            var mob = _spawning.SpawnPlayerMob(Transform(vat).Coordinates, null, profile, station);

            _metaSystem.SetEntityName(mob, mindComp.CharacterName!);

            if (profile != null)
                _traits.ApplyTraits(mob, profile);

            // Lets them examine the finished result. DNA and fingerprints are already on the mob from its species
            // prototype, so there's nothing to carry over from a corpse that no longer exists.
            EnsureComp<DetailExaminableComponent>(mob);

            if (!_container.Insert(mob, container))
            {
                _sawmill.Error($"Failed to insert {ToPrettyString(mob)} into the vat {ToPrettyString(vat)}.");
                QueueDel(mob);
                return false;
            }

            // This deletes the ghost the mind was wearing and hands the body over.
            _mind.TransferTo(mindUid, mob, true, mind: mindComp);

            // JAMBOREE - Only burn biomass once we've actually got a body, so a failed vat insert costs them nothing.
            _material.TryChangeMaterialAmount(vat, vatComp.RequiredMaterial, -vatComp.MaterialCost);

            body = mob;
            QueuePopOut(vat, container, mob);
            return true;
        }

        private void QueuePopOut(EntityUid vat, ContainerSlot container, EntityUid body)
        {
            Timer.Spawn(SynthesisTime, () => PopOut(vat, container, body));
        }

        private void PopOut(EntityUid vat, ContainerSlot container, EntityUid body)
        {
            if (Deleted(vat) || Terminating(vat) || Deleted(body) || Terminating(body))
                return;

            if (container.ContainedEntity != body)
                return;

            _container.Remove(body, container);

            // Nudge them clear of the machine so nobody gets stuck inside a wall.
            var offset = new Vector2(_random.NextFloat(-0.5f, 0.5f), _random.NextFloat(-0.5f, 0.5f));
            _transform.SetCoordinates(body, Transform(vat).Coordinates.Offset(offset));
            _popup.PopupEntity(Loc.GetString("centralcommand-dnacloner-synthesized"), body, body);
        }

        #endregion

        //  Remeasures how far away the wired machine is so the console knows whether it can be used.

        public void RecheckConnections(EntityUid console, CentralCommandDnaClonerConsoleComponent? component = null)
        {
            if (!Resolve(console, ref component))
                return;

            if (component.DnaClonerVat is { } vat && !Deleted(vat))
            {
                Transform(vat).Coordinates.TryDistance(EntityManager, Transform(console).Coordinates, out var distance);
                component.DnaClonerVatInRange = distance <= component.MaxDistance;
            }
            else
            {
                component.DnaClonerVatInRange = false;
            }

            UpdateUserInterface(console, component);
        }

        private bool TryGetVatContainer(EntityUid vat, [NotNullWhen(true)] out ContainerSlot? container)
        {
            container = null;

            if (!TryComp<CentralCommandDnaClonerVatComponent>(vat, out var comp))
                return false;

            if (!_container.TryGetContainer(vat, comp.ContainerId, out var found))
                return false;

            if (found is not ContainerSlot slot)
                return false;

            container = slot;
            return true;
        }
    }
}
