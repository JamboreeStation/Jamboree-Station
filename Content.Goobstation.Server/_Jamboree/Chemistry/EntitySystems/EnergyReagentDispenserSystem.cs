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

using System.Linq;
using Content.Goobstation.Server._Jamboree.Chemistry.Components;
using Content.Goobstation.Shared._Jamboree.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Labels.Components;
using Content.Server.Power.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Server.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell.Components;

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

        // Ergh...Alot of dependencies. But this is a complex system, so it needs literally all these classes.

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

            SubscribeLocalEvent<EnergyReagentDispenserComponent, MapInitEvent>(OnMapInit, before: [typeof(ItemSlotsSystem)]);
        }
        // Initialize() means "uh what do i listen for??". So this whole bit is subscribing to other classes methods so we can use them.
        private void SubscribeUpdateUiState<T>(Entity<EnergyReagentDispenserComponent> ent, ref T ev) => UpdateUiState(ent);

        private void UpdateUiState(Entity<EnergyReagentDispenserComponent> reagentDispenser)
        {
            // This is called almost every single time you use it...SOB.


            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedEnergyReagentDispenser.OutputSlotName);
            var outputContainerInfo = BuildOutputContainerInfo(outputContainer);
            var inventory = GetInventory(reagentDispenser.Comp);
            var batteryCharge = 0f;
            var batteryMaxCharge = 0f;
            var currentReceivingEnergy = 0f;
            var usingBattery = false;
            var idleUse = 0f;
            var hasPower = false;
            // all of these are self explanitory on what they are meant for.

            // Portable dispensers are powered by the cell in their cell slot, stationary ones by their internal battery.
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
                // Elsewise, this means its stationary dispenser. So we use its internal battery instead of a cell.
            }

            if (TryComp<ApcPowerReceiverBatteryComponent>(reagentDispenser, out var apcPower))
            {
                currentReceivingEnergy = apcPower.BatteryRechargeRate;
                usingBattery = apcPower.Enabled;
                idleUse = apcPower.IdleLoad;
            }

            // Cell-powered portables aren't tied to the APC net, so don't let an inherited receiver override their power state.
            // Plus if its a stationary dispenser and get power from APC, let the APC decide if powered lol.
            if (!usingCell && TryComp<ApcPowerReceiverComponent>(reagentDispenser, out var apc))
                hasPower = apc.Powered;

            // Then we put allat information into an object so the client can actually read the damn thing.
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
            // Basically send all this information to the client. Such as how much charge that current dispenser has.
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
                // This basically displays all the reagents in the dispenser that it can dispenseand their cost.
                // So if you add a new reagent to the dispenser, itll show up here.
            }

            inventory.Sort((a, b) => string.Compare(a.ReagentLabel, b.ReagentLabel, StringComparison.Ordinal));  // sort it by name.
            return inventory;
        }


        // This method below updates the UI when the client does literally anything to it. So the information it outputs to the client is always accurate.
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

            // Portable dispenser? If a power cell is in the slot, power comes from it.
            if (_powerCell.TryGetBatteryFromSlot(ent.Owner, out var cellBattery))
                hasPower = _battery.GetCharge(cellBattery.Value.AsNullable()) > 0f;

            // Stationary is if it doesnt have a powercell slot.
            else if (TryComp<ApcPowerReceiverComponent>(ent, out var apc))
                hasPower = apc.Powered;

            // Send this information to the clients visualiser.
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

            // Portable dispensers draw from the cell in their cell slot, stationary ones from their internal battery.
            if (!_powerCell.TryGetBatteryFromSlotOrEntity(reagentDispenser.Owner, out var battery)
                || _battery.GetCharge(battery.Value.AsNullable()) < powerRequired)
            {
                _audioSystem.PlayPvs(reagentDispenser.Comp.PowerSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));
                return;
            }


            var sol = new Solution(message.ReagentId, amount);
            if (!_solutionContainerSystem.TryAddSolution(solution.Value, sol))
                return;

            _battery.TryUseCharge(battery.Value.AsNullable(), powerRequired);
            reagentDispenser.Comp.StoredEnergySpent += powerRequired;
            ClickSound(reagentDispenser);
            UpdateUiState(reagentDispenser); // Replaced this to track the energy spent // JAMBOREE
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

                reagentDispenser.Comp.StoredEnergySpent = 0f; // Add restored energy back to the battery instead of a ridiculous amount. JAMBOREE
            }

            _solutionContainerSystem.RemoveAllSolution(solution.Value); // yeah the machine eats it. idk what else to tell you
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnBeakerInserted(Entity<EnergyReagentDispenserComponent> ent, ref EntInsertedIntoContainerMessage args)
        {
            UpdateBeakerAppearance(ent);
            UpdateUiState(ent);
            // this is for portable energy dispensers. Makes the beaker show on the sprite...Wow.
        }

        private void OnBeakerRemoved(Entity<EnergyReagentDispenserComponent> ent, ref EntRemovedFromContainerMessage args)
        {
            ent.Comp.StoredEnergySpent = 0f;
            UpdateBeakerAppearance(ent);
            UpdateUiState(ent); // Update the UI to reflect the reset energy when the beaker is removed. JAMBOREE

        } // Remove the stored energy spent when the containers removed to prevent the exploit of swapping containers to restore energy. JAMBOREE

        private void UpdateBeakerAppearance(Entity<EnergyReagentDispenserComponent> ent)
        {
            var beaker = _itemSlotsSystem.GetItemOrNull(ent, SharedEnergyReagentDispenser.OutputSlotName);
            _appearanceSystem.SetData(ent.Owner, EnergyReagentDispenserVisuals.Beaker, beaker != null);
            // Ditto. You have to cover EVERY ACTION for the sprite and UI.
        }

        private void ClickSound(Entity<EnergyReagentDispenserComponent> reagentDispenser) =>
            _audioSystem.PlayPvs(reagentDispenser.Comp.ClickSound, reagentDispenser, AudioParams.Default.WithVolume(-2f)); // its a play sound! Clicky click!

        private static float GetPowerCostForReagent(string reagentId, int amount, EnergyReagentDispenserComponent comp)
        {
            return comp.Reagents.TryGetValue(reagentId, out var cost)
                ? cost * amount
                : float.MaxValue; // We get to see the cost of the reagent. You get to set each individual one in YAML!
        }
        private void OnMapInit(Entity<EnergyReagentDispenserComponent> entity, ref MapInitEvent args)
        {
            _itemSlotsSystem.AddItemSlot(entity.Owner, SharedEnergyReagentDispenser.OutputSlotName, entity.Comp.EnergyBeakerSlot);
            UpdatePowerAppearance(entity);
        }
    }
}
