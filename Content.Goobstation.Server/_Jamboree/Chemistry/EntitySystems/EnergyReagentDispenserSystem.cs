// SPDX-FileCopyrightText: 2021 20kdc <asdd2808@gmail.com>
// SPDX-FileCopyrightText: 2021 Clyybber <darkmine956@gmail.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <gradientvera@outlook.com>
// SPDX-FileCopyrightText: 2021 Ygg01 <y.laughing.man.y@gmail.com>
// SPDX-FileCopyrightText: 2022 Rane <60792108+Elijahrane@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 ElectroJr <leonsfriedrich@gmail.com>
// SPDX-FileCopyrightText: 2023 Emisse <99158783+Emisse@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 deltanedas <deltanedas@laptop>
// SPDX-FileCopyrightText: 2023 deltanedas <user@zenith>
// SPDX-FileCopyrightText: 2024 0x6273 <0x40@keemail.me>
// SPDX-FileCopyrightText: 2024 AWF <you@example.com>
// SPDX-FileCopyrightText: 2024 Brandon Li <48413902+aspiringLich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Cojoke <83733158+Cojoke-dot@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 GitHubUser53123 <110841413+GitHubUser53123@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Jake Huxell <JakeHuxell@pm.me>
// SPDX-FileCopyrightText: 2024 Kevin Zheng <kevinz5000@gmail.com>
// SPDX-FileCopyrightText: 2024 Kira Bridgeton <161087999+Verbalase@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 Tayrtahn <tayrtahn@gmail.com>
// SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 IrisTheAmped <iristheamped@gmail.com>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2026 PureBreadBagel <purebreadbagel@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later


using Content.Goobstation.Server._Jamboree.Chemistry.Components;
using Content.Goobstation.Shared._Jamboree.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell.Components;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Popups;
using Robust.Server.Player;


namespace Content.Goobstation.Server._Jamboree.Chemistry.EntitySystems
{
    /// <summary>
    /// Contains all the server-side logic for reagent dispensers.
    /// <seealso cref="EnergyReagentDispenserComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed partial class EnergyReagentDispenserSystem : EntitySystem
    {
        [Dependency] private AudioSystem _audioSystem = default!;
        [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private IPrototypeManager _prototypeManager = default!;
        [Dependency] private BatterySystem _battery = default!;
        [Dependency] private PowerCellSystem _powerCell = default!;
        [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;

        [Dependency] private IAdminManager _adminManager = default!;
        [Dependency] private SharedPopupSystem _popup = default!;
        [Dependency] private IPlayerManager _playerManager = default!;

        // A lot of dependencies, but this system touches power, cells, APC, containers and chat, so it earns them.

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EnergyReagentDispenserComponent, EntRemovedFromContainerMessage>(OnBeakerRemoved);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, EntInsertedIntoContainerMessage>(OnBeakerInserted);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmountMessage);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserDispenseReagentMessage>(OnDispenseReagentMessage);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolutionMessage);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, PowerCellChangedEvent>(OnPowerCellChanged);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserEditReagentMessage>(OnEditReagentMessage);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, MapInitEvent>(OnMapInit, before: [typeof(ItemSlotsSystem)]);
        }
        // Every one of those subscriptions funnels back here, so the state can't drift out of sync with the machine.
        private void SubscribeUpdateUiState<T>(Entity<EnergyReagentDispenserComponent> ent, ref T ev) => UpdateUiState(ent);

        private void UpdateUiState(Entity<EnergyReagentDispenserComponent> reagentDispenser)
        {
            // Runs on almost every interaction with the dispenser, so keep it cheap.

            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedEnergyReagentDispenser.OutputSlotName);
            var outputContainerInfo = BuildOutputContainerInfo(outputContainer);
            var inventory = GetInventory(reagentDispenser.Comp);
            var batteryCharge = 0f;
            var batteryMaxCharge = 0f;
            var currentReceivingEnergy = 0f;
            var usingBattery = false;
            var idleUse = 0f;
            var hasPower = false;

            // JAMBOREE Portable dispensers are powered by the cell in their cell slot, stationary ones by their internal battery.
            var usingCell = false;
            if (_powerCell.TryGetBatteryFromSlot(reagentDispenser.Owner, out var cellBattery))
            {
                usingCell = true;
                batteryCharge = _battery.GetCharge(cellBattery.Value.AsNullable());
                batteryMaxCharge = cellBattery.Value.Comp.MaxCharge;
                hasPower = _battery.GetCharge(cellBattery.Value.AsNullable()) > 0f;
                // If this is a portable dispenser, we get the battery from the cell slot and use that for the UI state!
            }
            else if (TryComp<BatteryComponent>(reagentDispenser, out var battery))
            {
                batteryCharge = _battery.GetCharge((reagentDispenser.Owner, battery));
                batteryMaxCharge = battery.MaxCharge;
            }

            if (TryComp<ApcPowerReceiverBatteryComponent>(reagentDispenser, out var apcPower))
            {
                currentReceivingEnergy = apcPower.BatteryRechargeRate;
                usingBattery = apcPower.Enabled;
                idleUse = apcPower.IdleLoad;
            }

            // Cell-powered portables aren't tied to the APC net, so don't let an inherited receiver override their power state.
            // JAMBOREE A stationary dispenser on APC lets the APC decide whether it's powered.
            if (!usingCell && TryComp<ApcPowerReceiverComponent>(reagentDispenser, out var apc))
                hasPower = apc.Powered;

            var state = new EnergyReagentDispenserBoundUserInterfaceState(
                outputContainerInfo,
                GetNetEntity(outputContainer),
                inventory,
                reagentDispenser.Comp.DispenseAmount,
                batteryCharge,
                batteryMaxCharge,
                currentReceivingEnergy,
                idleUse,
                usingBattery,
                hasPower
            );
            // Broadcasts to every client with this dispenser open, so anyone watching sees the same numbers.
            _userInterfaceSystem.SetUiState(reagentDispenser.Owner, EnergyReagentDispenserUiKey.Key, state);

        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null; // if theres no beaker then return null lol.

            if (_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out _, out var solution))
            {
                // if there is a beaker, then show its max capacity, current capacity and what reagents are in it.
                return new ContainerInfo(Name(container.Value), solution.Volume, solution.MaxVolume)
                {
                    Reagents = solution.Contents,
                };
            }

            return null;
        }

        private List<EnergyReagentInventoryItem> GetInventory(EnergyReagentDispenserComponent comp)
        {
            var inventory = new List<EnergyReagentInventoryItem>();

            foreach (var (reagentId, cost) in comp.Reagents)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagentId, out var reagentProto))
                    continue;

                inventory.Add(new EnergyReagentInventoryItem(
                    reagentId,
                    reagentProto.LocalizedName,
                    cost,
                    reagentProto.SubstanceColor
                ));
                // Any reagent added to the component shows up here automatically, with its localised name and colour.
                // The client sorts by label, so dispense order is stable rather than dictionary order.
            }

            inventory.Sort((a, b) => string.Compare(a.ReagentLabel, b.ReagentLabel, StringComparison.Ordinal));  // sort it by name.
            return inventory;
        }


        // Anything the client can do to this dispenser funnels back through a handler, and each one
        // re-sends state so the UI can't drift out of step with the machine.
        private void OnSetDispenseAmountMessage(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref EnergyReagentDispenserSetDispenseAmountMessage message)
        {
            reagentDispenser.Comp.DispenseAmount = message.EnergyReagentDispenserDispenseAmount;
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnPowerChanged(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref PowerChangedEvent args)
        {
            UpdatePowerAppearance(reagentDispenser);
            UpdateUiState(reagentDispenser);

        }
        private void OnPowerCellChanged(EntityUid uid, EnergyReagentDispenserComponent component, PowerCellChangedEvent args)
        {
            var ent = new Entity<EnergyReagentDispenserComponent>(uid, component);
            UpdatePowerAppearance(ent);
            UpdateUiState(ent);
        }

        // Asks "is this dispenser powered?" and tells the appearance system the answer,
        // so the client's visualizer can flip the sprite between powered / no-power.
        private void UpdatePowerAppearance(Entity<EnergyReagentDispenserComponent> ent)
        {
            var hasPower = false;

            // JAMBOREE Portable dispenser? If a power cell is in the slot, power comes from it.
            if (_powerCell.TryGetBatteryFromSlot(ent.Owner, out var cellBattery))
                hasPower = cellBattery?.Comp.LastCharge > 0f;

            //JAMBOREE A portable with a cell in the slot draws from that, anything else is stationary and follows the APC.
            else if (TryComp<ApcPowerReceiverComponent>(ent, out var apc))
                hasPower = apc.Powered;

            _appearanceSystem.SetData(ent.Owner, EnergyReagentDispenserVisuals.Powered, hasPower);
        }

        private void OnDispenseReagentMessage(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref EnergyReagentDispenserDispenseReagentMessage message)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedEnergyReagentDispenser.OutputSlotName);
            if (outputContainer is not { Valid: true }
                || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
                return;

            var amount = (int) reagentDispenser.Comp.DispenseAmount;
            var powerRequired = GetPowerCostForReagent(message.ReagentId, amount, reagentDispenser.Comp);

            // JAMBOREE Portable dispensers draw from the cell in their cell slot, stationary ones from their internal battery.
            var usingCell = _powerCell.TryGetBatteryFromSlot(reagentDispenser.Owner, out var cellBatteryEnt);
            BatteryComponent? battery = usingCell ? cellBatteryEnt?.Comp : null;
            if (!usingCell)
                TryComp(reagentDispenser, out battery);

            if (battery is null || battery.LastCharge < powerRequired)
            {
                _audioSystem.PlayPvs(reagentDispenser.Comp.PowerSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));
                return;
            }


            var sol = new Solution(message.ReagentId, amount);
            if (!_solutionContainerSystem.TryAddSolution(solution.Value, sol))
                return;

            _battery.TryUseCharge(cellBatteryEnt?.Owner ?? reagentDispenser.Owner, powerRequired);
            reagentDispenser.Comp.StoredEnergySpent += powerRequired;
            ClickSound(reagentDispenser);
            UpdateUiState(reagentDispenser); // JAMBOREE Replaced this to track the energy spent.
        }

        // JAMBOREE - These explain why the reagent edit attempt was denied. Like typos of a reagent or they arent an admin.
        // + this is also a Jamboree exclusive new system, its not in the original Goobstation e-Chem.
        private void OnEditReagentMessage(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref EnergyReagentDispenserEditReagentMessage message)
        {
            if (!_playerManager.TryGetSessionByEntity(message.Actor, out var session))
            {
                Reply(message.Actor, "edit: sender has no session (server)");
                return;
            }

            if (_adminManager.GetAdminData(session)?.HasFlag(AdminFlags.Admin) != true)
            {
                Reply(message.Actor, "edit: rejected, not an admin (server)");
                return;
            }

            // -1 is the deletion sentinel, filtered here so it never becomes data.
            if (message.NewCost < 0f)
            {
                reagentDispenser.Comp.Reagents.Remove(message.ReagentId);
                UpdateUiState(reagentDispenser);
                Reply(message.Actor, $"edit: removed {message.ReagentId} (server)");
                return;
            }

            // JAMBOREE Reject ids that don't resolve to a real reagent, so an admin typo can't poison the inventory.
            if (!_prototypeManager.TryIndex<ReagentPrototype>(message.ReagentId, out _))
            {
                Reply(message.Actor, $"edit: '{message.ReagentId}' is not a reagent id (server)");
                return;
            }

            reagentDispenser.Comp.Reagents[message.ReagentId] = message.NewCost;
            UpdateUiState(reagentDispenser);
            Reply(message.Actor, $"edit: set {message.ReagentId} to {message.NewCost} (server)");
        }


        private void Reply(EntityUid target, string text)
        {
            _popup.PopupEntity(text, target, target);
        }

        private void OnClearContainerSolutionMessage(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref EnergyReagentDispenserClearContainerSolutionMessage message)
        {
            var outputContainerNullable = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedEnergyReagentDispenser.OutputSlotName);
            if (outputContainerNullable is not { Valid: true } outputContainer
                || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer, out var solution, out _))
                return;

            if (reagentDispenser.Comp.StoredEnergySpent > 0f)
            {
                // Refund the energy to the cell in the slot for portable dispensers, or the internal battery for stationary ones.
                if (_powerCell.TryGetBatteryFromSlotOrEntity(reagentDispenser.Owner, out var battery))
                    _battery.ChangeCharge(battery.Value.AsNullable(), reagentDispenser.Comp.StoredEnergySpent);

                reagentDispenser.Comp.StoredEnergySpent = 0f; // JAMBOREE - refund exactly what was tracked, never a fixed amount
            }

            _solutionContainerSystem.RemoveAllSolution(solution.Value); // the machine eats the contents
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnBeakerInserted(Entity<EnergyReagentDispenserComponent> ent, ref EntInsertedIntoContainerMessage args)
        {
            UpdateBeakerAppearance(ent);
            UpdateUiState(ent);
        }

        private void OnBeakerRemoved(Entity<EnergyReagentDispenserComponent> ent, ref EntRemovedFromContainerMessage args)
        {
            // JAMBOREE - Zero the tracked spend on removal. This is so the player can't swap beakers with a cheap reagent
            // like water to abuse the recharge-back feature. Which is unfixed in Goobstations.
            ent.Comp.StoredEnergySpent = 0f;
            UpdateBeakerAppearance(ent);
            UpdateUiState(ent);
        }

        private void UpdateBeakerAppearance(Entity<EnergyReagentDispenserComponent> ent)
        {
            var beaker = _itemSlotsSystem.GetItemOrNull(ent, SharedEnergyReagentDispenser.OutputSlotName);
            _appearanceSystem.SetData(ent.Owner, EnergyReagentDispenserVisuals.Beaker, beaker != null);
        }

        private void ClickSound(Entity<EnergyReagentDispenserComponent> reagentDispenser) =>
            _audioSystem.PlayPvs(reagentDispenser.Comp.ClickSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));

        private static float GetPowerCostForReagent(string reagentId, int amount, EnergyReagentDispenserComponent comp)
        {
            return comp.Reagents.TryGetValue(reagentId, out var cost)
                ? cost * amount
                : float.MaxValue; // unknown reagents cost a number too big for a normal battery, so a bad lookup can never be free
        }
        private void OnMapInit(Entity<EnergyReagentDispenserComponent> entity, ref MapInitEvent args)
        {
            _itemSlotsSystem.AddItemSlot(entity.Owner, SharedEnergyReagentDispenser.OutputSlotName, entity.Comp.EnergyBeakerSlot);
            UpdatePowerAppearance(entity);
        }
    }
}
