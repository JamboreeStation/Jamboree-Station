// SPDX-License-Identifier: MIT
// Adapted from Starlight PR #5898, originally from Funky Station and Moff Station.

using System.Linq;
using Content.Shared._Funkystation.Stains.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Glue;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Lube;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Random.Helpers;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Funkystation.WashingMachine;

public abstract partial class SharedWashingMachineSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = null!;
    [Dependency] private INetManager _net = null!;
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private SharedPowerReceiverSystem _power = null!;
    [Dependency] private SharedEntityStorageSystem _storage = null!;
    [Dependency] private SharedAppearanceSystem _appearance = null!;
    [Dependency] private SharedPopupSystem _popup = null!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private DamageableSystem _damageable = null!;
    [Dependency] private ReactiveSystem _reactive = null!;
    [Dependency] private SharedStainSystem _stains = default!;
    [Dependency] private SharedCreamPieSystem _creamPie = default!;
    [Dependency] private GlueSystem _glue = default!;
    [Dependency] private SharedLubedSystem _lube = default!;
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WashingMachineComponent, ActivateInWorldEvent>(OnActivate, before: new[] { typeof(SharedEntityStorageSystem) });
        SubscribeLocalEvent<WashingMachineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WashingMachineComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<WashingMachineComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WashingMachineComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<WashingMachineComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<WashingMachineComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Timed hazards and cleaning have no predicting user.
        if (!_net.IsServer)
            return;

        var query = EntityQueryEnumerator<WashingMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.State != WashingMachineState.Washing)
                continue;

            if (!_power.IsPowered(uid) || comp.WashFinishTime == null || _storage.IsOpen(uid))
            {
                AbortWash((uid, comp), WashingMachineState.Idle);
                continue;
            }

            if (_timing.CurTime >= comp.WashFinishTime)
            {
                FinishWash(uid, comp);
                continue;
            }

            ProcessWashingHazards(uid, comp, frameTime);
        }
    }

    private void OnMapInit(Entity<WashingMachineComponent> ent, ref MapInitEvent args)
    {
        _appearance.SetData(ent.Owner, WashingMachineVisuals.State, ent.Comp.State);
    }

    private void OnBreak(Entity<WashingMachineComponent> ent, ref BreakageEventArgs args)
    {
        if (_net.IsServer)
            AbortWash(ent, WashingMachineState.Broken);
    }

    private void OnShutdown(Entity<WashingMachineComponent> ent, ref ComponentShutdown args)
    {
        if (!_net.IsServer)
            return;

        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
        if (!TerminatingOrDeleted(ent.Owner) && ent.Comp.State == WashingMachineState.Washing)
            AbortWash(ent, WashingMachineState.Idle);
    }

    private void OnPowerChanged(Entity<WashingMachineComponent> ent, ref PowerChangedEvent args)
    {
        if (_net.IsServer && !args.Powered && ent.Comp.State == WashingMachineState.Washing)
            AbortWash(ent, WashingMachineState.Idle);
    }

    private void AbortWash(Entity<WashingMachineComponent> ent, WashingMachineState state)
    {
        ent.Comp.State = state;
        ent.Comp.WashFinishTime = null;
        ent.Comp.NextWashAllowed = _timing.CurTime + ent.Comp.Cooldown;
        ent.Comp.AccumulatedSelfDamage = 0;
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
        Dirty(ent.Owner, ent.Comp);
        _appearance.SetData(ent.Owner, WashingMachineVisuals.State, state);
        _storage.OpenStorage(ent.Owner);
    }

    private void OnStorageOpenAttempt(Entity<WashingMachineComponent> ent, ref StorageOpenAttemptEvent args)
    {
        if (ent.Comp.State == WashingMachineState.Washing)
            args.Cancelled = true;
    }

    private void OnGetVerbs(Entity<WashingMachineComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (ent.Comp.State != WashingMachineState.Idle || !_power.IsPowered(ent.Owner) || _storage.IsOpen(ent.Owner))
            return;

        if (!TryComp<EntityStorageComponent>(ent, out var storage) || storage.Contents.ContainedEntities.Count == 0)
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("washing-machine-start"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Act = () =>
            {
                if (_timing.CurTime < ent.Comp.NextWashAllowed)
                {
                    _popup.PopupEntity(Loc.GetString("washing-machine-cooldown"), ent.Owner, user);
                    return;
                }
                TryStartWash(ent);
            }
        });
    }

    private void OnActivate(Entity<WashingMachineComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (ent.Comp.State != WashingMachineState.Idle || !_power.IsPowered(ent.Owner) || _storage.IsOpen(ent.Owner))
            return;

        if (!TryComp<EntityStorageComponent>(ent, out var storage) || storage.Contents.ContainedEntities.Count == 0)
            return;

        if (_timing.CurTime < ent.Comp.NextWashAllowed)
        {
            _popup.PopupEntity(Loc.GetString("washing-machine-cooldown"), ent.Owner, args.User);
            args.Handled = true;
            return;
        }

        args.Handled = true;
        TryStartWash(ent);
    }

    private void ProcessWashingHazards(EntityUid uid, WashingMachineComponent comp, float frameTime)
    {
        if (!TryComp<EntityStorageComponent>(uid, out var storage) || storage.Contents.ContainedEntities.Count == 0)
            return;

        var damageProto = _proto.Index(comp.WashingDamageType);
        var damage = new DamageSpecifier(damageProto, comp.BluntDamagePerSecond * frameTime);

        var waterSpray = new Solution();
        waterSpray.AddReagent(comp.WaterSprayReagent, comp.WaterSprayAmount);

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(uid));

        var sprayWater = rand.Prob(comp.WaterSprayChance * frameTime);

        var hasHeavyItems = false;

        // Damage and touch reactions can destroy items and change the container.
        foreach (var item in storage.Contents.ContainedEntities.ToArray())
        {
            if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid) || comp.State != WashingMachineState.Washing)
                return;

            if (TerminatingOrDeleted(item) || !storage.Contents.Contains(item))
                continue;

            if (TryComp<DamageableComponent>(item, out var damageable))
                _damageable.TryChangeDamage((item, damageable), damage, ignoreResistances: true);

            if (sprayWater && !TerminatingOrDeleted(item))
                _reactive.DoEntityReaction(item, waterSpray, ReactionMethod.Touch);

            if (!hasHeavyItems && !HasComp<ClothingComponent>(item))
                hasHeavyItems = true;
        }

        if (hasHeavyItems && !TerminatingOrDeleted(uid) && comp.State == WashingMachineState.Washing)
        {
            if (rand.Prob(comp.ThumpSoundChance * frameTime))
                _audio.PlayPvs(comp.HitSound, uid);

            comp.AccumulatedSelfDamage += comp.SelfDamagePerSecond * frameTime;
        }
    }

    private void FinishWash(EntityUid uid, WashingMachineComponent comp)
    {
        comp.State = WashingMachineState.Idle;
        comp.WashFinishTime = null;
        comp.NextWashAllowed = _timing.CurTime + comp.Cooldown;

        comp.AudioStream = _audio.Stop(comp.AudioStream);
        _audio.PlayPvs(comp.WashFinishedSound, uid);
        _appearance.SetData(uid, WashingMachineVisuals.State, WashingMachineState.Idle);

        HashSet<EntityUid> items = new();
        if (TryComp<EntityStorageComponent>(uid, out var storage))
        {
            items = storage.Contents.ContainedEntities.ToHashSet();

            // Harvest evidence from everything the stain helpers will clean, before clearing any solutions.
            var evidenceItems = new HashSet<EntityUid>(items);
            foreach (var item in items)
            {
                var slots = _inventory.GetSlotEnumerator(item, SlotFlags.WITHOUT_POCKET);
                while (slots.NextItem(out var clothing))
                    evidenceItems.Add(clothing);
            }

            foreach (var item in evidenceItems.ToArray())
            {
                if (TryComp<ToggleableClothingComponent>(item, out var toggleable))
                    evidenceItems.UnionWith(toggleable.ClothingUids.Keys);
            }

            UpdateForensics((uid, comp), evidenceItems);

            foreach (var item in items)
            {
                if (TerminatingOrDeleted(item))
                    continue;

                if (TryComp<CreamPiedComponent>(item, out var creamPied))
                    _creamPie.SetCreamPied(item, creamPied, false);
                _lube.RemoveLubed(item);
                if (HasComp<GluedComponent>(item))
                    _glue.RemoveGlued(item);

                _stains.CleanStains(item);
                _stains.CleanEquippedClothing(item);
            }
        }

        var machineEv = new WashingMachineFinishedWashingEvent(items);
        RaiseLocalEvent(uid, ref machineEv);

        var itemEv = new WashingMachineWashedEvent(uid, items);
        foreach (var item in items)
        {
            RaiseLocalEvent(item, ref itemEv);
        }

        if (comp.AccumulatedSelfDamage > 0)
        {
            var damageProto = _proto.Index(comp.WashingDamageType);
            var selfDamage = new DamageSpecifier(damageProto, comp.AccumulatedSelfDamage);
            comp.AccumulatedSelfDamage = 0;
            _damageable.TryChangeDamage((uid, null), selfDamage, ignoreResistances: true);
        }

        if (TerminatingOrDeleted(uid))
            return;

        Dirty(uid, comp);
        _storage.OpenStorage(uid);
    }

    private void TryStartWash(Entity<WashingMachineComponent> ent)
    {
        if (!_net.IsServer)
            return;

        if (ent.Comp.State != WashingMachineState.Idle || !_power.IsPowered(ent.Owner) || _storage.IsOpen(ent.Owner))
            return;

        if (_timing.CurTime < ent.Comp.NextWashAllowed)
            return;

        if (!TryComp<EntityStorageComponent>(ent, out var storage) || storage.Contents.ContainedEntities.Count == 0)
            return;

        ent.Comp.State = WashingMachineState.Washing;
        ent.Comp.WashFinishTime = _timing.CurTime + ent.Comp.WashTime;
        ent.Comp.AudioStream ??= _audio.PlayPvs(ent.Comp.WashLoopSound, ent.Owner)?.Entity;

        Dirty(ent.Owner, ent.Comp);
        _appearance.SetData(ent.Owner, WashingMachineVisuals.State, WashingMachineState.Washing);

        var items = storage.Contents.ContainedEntities.ToHashSet();

        var machineEv = new WashingMachineStartedWashingEvent(items);
        RaiseLocalEvent(ent.Owner, ref machineEv);

        var itemEv = new WashingMachineIsBeingWashed(ent.Owner, items);
        foreach (var item in items)
        {
            RaiseLocalEvent(item, ref itemEv);
        }
    }

    protected virtual void UpdateForensics(Entity<WashingMachineComponent> ent, HashSet<EntityUid> items)
    {
    }
}
