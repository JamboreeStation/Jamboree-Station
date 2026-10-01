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
using Content.Server.UserInterface;
using Content.Shared._Jamboree.DnaCloner;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.DetailExaminable;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Forensics.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Log;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility; //JAMBOREE API SLOP PART 1

namespace Content.Server._Jamboree.DnaCloner.Systems
{

    //JAMBOREE Central Command's DNA Molecularsynthesis Console. Lists every player who has been ROUND REMOVED so they can get back.

    public sealed class CentralCommandDnaClonerSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
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
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ILogManager _log = default!; //JAMBOREE  I HATE API SLOP!!!!
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

            //JAMBOREE Give the visualizer a definite starting value.
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

            // JAMBOREE The console announces its own states in the new bureaucratic language.
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
                ("cost", component.MaterialCost))); // just displays the inspect menu. nothing special
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

        // JAMBOREE Getting MOLECULAR SYNTHESISED? Well change its sprites of course!
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
            if (component.DnaClonerVat is not { } vat)
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

        private int GetBiomass(EntityUid vat, CentralCommandDnaClonerVatComponent comp)
        {
            return _material.GetMaterialAmount(vat, comp.RequiredMaterial);
        }

        // JAMBOREE  Every mind that belongs to a connected player currently wandering around as a ghost with no body to get back to. I.e RR'd

        private List<CentralCommandDnaClonerMindRow> GetRespawnableMinds(int rottenBodyStage)
        {
            var nameQuery = EntityQueryEnumerator<MindComponent>();
            var names = new List<CentralCommandDnaClonerMindRow>();

            while (nameQuery.MoveNext(out var mindEntity, out var mindComp))
            {
                if (!IsRespawnable(mindComp, rottenBodyStage))
                    continue;

                // JAMBOREE the EntityUid is translated into a NetEntity so the client can send it back on click.
                names.Add(new CentralCommandDnaClonerMindRow(
                    GetNetEntity(mindEntity),
                    mindComp.CharacterName!,
                    GetBodyState(mindComp, rottenBodyStage)));
            }

            names.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            _sawmill.Debug($"Found {names.Count} candidate minds.");
            return names;
        }

        // JAMBOREE the whole class that checks if they are round removed or rotting.
        private bool IsRespawnable(MindComponent mind, int rottenBodyStage)
        {
            if (mind.CharacterName == null
                || mind.UserId == null
                || !_players.ValidSessionId(mind.UserId.Value))
            {
                return false;
            }

            if (mind.OwnedEntity is { } owned)
            {
                // Round-removed: no body. Cant return back.
                if (TryComp<GhostComponent>(owned, out var ownedGhost))
                    return !ownedGhost.CanReturnToBody;

                if (!Deleted(owned) && !IsRottenPast(owned, rottenBodyStage))
                    return false;
            }
            return mind.VisitingEntity is { } visiting
                   && TryComp<GhostComponent>(visiting, out var ghost)
                   && ghost.CanReturnToBody;
        }


       // JAMBOREE - Checks if the body is ROTTING.
        private bool IsRottenPast(EntityUid body, int stage)
        {
            if (stage < 0 || !_rotting.IsRotten(body))
                return false;

            return _rotting.RotStage(body) >= stage;
        }


        // JAMBOREE - Depending on if they got round removed or rotted, then it should return variables for the name of the minds.
        // for example if they are rotted it should show "(name) (rotted)"
        private CentralCommandDnaClonerBodyState GetBodyState(MindComponent mind, int rottenBodyStage)
        {
            if (mind.OwnedEntity is not { } owned || HasComp<GhostComponent>(owned))
                return CentralCommandDnaClonerBodyState.Intact;

            if (Deleted(owned))
                return CentralCommandDnaClonerBodyState.Lost;

            return IsRottenPast(owned, rottenBodyStage)
                ? CentralCommandDnaClonerBodyState.Rotted
                : CentralCommandDnaClonerBodyState.Intact;
        }

        #endregion

        #region Respawning


        // JAMBOREE - THIS IS WHERE WE BRING BACK THE BODIES FROM ROUND REMOVAL!!!
        private void OnRespawnRequested(EntityUid uid, CentralCommandDnaClonerConsoleComponent component, CentralCommandDnaClonerRespawnMessage args)
        {
            var mind = GetEntity(args.Mind);
            if (!TryComp<MindComponent>(mind, out var mindComp) || !IsRespawnable(mindComp, component.RottenBodyStage))
            {
                Speak(uid, "centralcommand-dnacloner-invalid-target"); // if somehow they get revived the moment you click the button. Highly unlikely but possible.
                UpdateUserInterface(uid, component);
                return;
            }

            if (!_power.IsPowered(uid))
            {
                Speak(uid, "centralcommand-dnacloner-unpowered"); //JAMBOREE basically say I AM DEAD if no power. rip.
                return;
            }

            var status = GetStatus(component);
            if (status != CentralCommandDnaClonerStatus.Ready)
            {
                Speak(uid, "centralcommand-dnacloner-unavailable", status); // if its not ready then say why
                return;
            }

            var vat = component.DnaClonerVat!.Value;
            if (!TryComp<CentralCommandDnaClonerVatComponent>(vat, out var vatComp))
            {
                Speak(uid, "centralcommand-dnacloner-failed"); // JAMBOREE This should not happen. And should be reported in a bug report if so.
                return;
            }

            if (!TryGetVatContainer(vat, out var container))
            {
                Speak(uid, "centralcommand-dnacloner-failed");
                return;
            }

            // JAMBOREE - Read what became of the old body BEFORE the synthesis. So i can get its comps back n that.
            var name = mindComp.CharacterName!;
            var success = GetBodyState(mindComp, component.RottenBodyStage) switch
            {
                CentralCommandDnaClonerBodyState.Lost => "centralcommand-dnacloner-success-recovered",
                CentralCommandDnaClonerBodyState.Rotted => "centralcommand-dnacloner-success-rotted",
                _ => "centralcommand-dnacloner-success",
            };

            if (!TrySnatchBack(mind, mindComp, vat, vatComp, container, out var body))
            {
                Speak(uid, "centralcommand-dnacloner-failed");
                UpdateUserInterface(uid, component);
                return;
            }

            component.SelectedMind = mind;
            _adminLogger.Add(LogType.Mind,
                LogImpact.High,
                $"{ToPrettyString(uid):entity} synthesized {mindComp.CharacterName:characterName} " +
                $"{ToPrettyString(mind):mind} into {ToPrettyString(body.Value):body}.");

            Speak(uid, success, name: name);
            UpdateUserInterface(uid, component);
        }

        // JAMBOREE - Has the console announce things inits own language n stuff.

        private void Speak(EntityUid uid, string key, CentralCommandDnaClonerStatus? status = null, string? name = null)
        {
            var message = Loc.GetString(key,
                ("reason", status is null ? string.Empty : Loc.GetString($"centralcommand-dnacloner-status-{status.Value}")),
                ("name", name ?? string.Empty));

            _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, hideChat: true);
        }


        // JAMBOREE Builds a brand new body for the given round-removed player inside the vat and hands their mind back to it.

        private bool TrySnatchBack(
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

            if (GetBiomass(vat, vatComp) < vatComp.MaterialCost)
                return false;

            // Their old body is long gone, so there is no appearance to copy!!
            //  So why not make it copy the appearence from the players profile?
            var profile = _gameTicker.GetPlayerProfile(session);
            var station = _station.GetOwningStation(vat) ?? _station.GetStations().FirstOrNull();
            var mob = _spawning.SpawnPlayerMob(Transform(vat).Coordinates, null, profile, station);


            if (profile != null)
                _traits.ApplyTraits(mob, profile);

            // Everything below is what the vanilla pod would have carried across, minus the corpse it read it off.
            ApplyRetainedComponents(mob);

            // This deletes the ghost the mind was wearing and hands the body over.
            _mind.TransferTo(mindUid, mob, true, mind: mindComp);

            if (!_container.Insert(mob, container))
            {
                _sawmill.Error($"Failed to insert {ToPrettyString(mob)} into the vat {ToPrettyString(vat)}.");
                return false;
            }

            // JAMBOREE Only consume biomass if it actually made a body...cant be losing biobaaaaas.
            _material.TryChangeMaterialAmount(vat, vatComp.RequiredMaterial, -vatComp.MaterialCost);

            body = mob;
            QueuePopOut(vat, container, mob);
            return true;
        }

        private void ApplyRetainedComponents(EntityUid mob)
        {
            // This is how we copy the components over to the body. Like their language knowledge n that.
            EnsureComp<DnaComponent>(mob);
            EnsureComp<FingerprintComponent>(mob);

            EnsureComp<DetailExaminableComponent>(mob);
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

        // JAMBOREE Remeasures how far away the wired machine is so the console knows whether it can be used.

        public void RecheckConnections(EntityUid console, CentralCommandDnaClonerConsoleComponent? component = null)
        {
            if (!Resolve(console, ref component))
                return;

            if (component.DnaClonerVat is { } vat)
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
