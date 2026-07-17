using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared._Jamboree.Mutations;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Jittering;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Jamboree.Mutations;

public sealed class GreaterMutantSystem : EntitySystem
{
    /// <summary>
    /// Container on the greater mutant that physically holds the victim's brain organ.
    /// </summary>
    public const string BrainContainerId = "greater_mutant_brain";

    /// <summary>
    /// Active mutations needed to collapse into a greater mutant.
    /// </summary>
    public const int MutationThreshold = 8;

    /// <summary>
    /// Status effect key used by <see cref="SharedJitteringSystem.DoJitter"/>.
    /// </summary>
    private const string JitterKey = "Jitter";

    private static readonly TimeSpan TransformDelay = TimeSpan.FromSeconds(10);

    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MutantComponent, MutationAddedEvent>(OnMutationAdded);
        SubscribeLocalEvent<PendingGreaterMutantComponent, MutationRemovedEvent>(OnPendingMutationRemoved);
        SubscribeLocalEvent<PendingGreaterMutantComponent, GreaterMutantTransformDoAfterEvent>(OnTransformDoAfter);
        SubscribeLocalEvent<GreaterMutantComponent, MutationRemovedEvent>(OnMutationRemoved);
        SubscribeLocalEvent<GreaterMutantComponent, DestructionEventArgs>(OnGreaterMutantDestruction);
        SubscribeLocalEvent<GreaterMutantComponent, BeingGibbedEvent>(OnGreaterMutantGibbed);
        SubscribeLocalEvent<GreaterMutantComponent, PolymorphedEvent>(OnGreaterMutantPolymorphed);
    }

    private void OnMutationAdded(Entity<MutantComponent> ent, ref MutationAddedEvent args)
    {
        if (ent.Comp.ActiveMutations.Count < MutationThreshold)
            return;

        if (HasComp<GreaterMutantComponent>(args.Entity) || HasComp<PendingGreaterMutantComponent>(args.Entity))
            return;

        TryStartTransform(args.Entity);
    }

    /// <summary>
    /// Begins the drawn-out collapse into a greater mutant. The victim jitters for the duration and
    /// the transform only lands if they are still over the threshold when it elapses.
    /// </summary>
    private void TryStartTransform(EntityUid uid)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, uid, TransformDelay, new GreaterMutantTransformDoAfterEvent(), uid)
        {
            // This is happening *to* them, not something they are doing. It should survive being
            // shoved in a scanner, cuffed, knocked down or beaten unconscious.
            RequireCanInteract = false,
            BreakOnMove = false,
            BreakOnWeightlessMove = false,
            BreakOnDamage = false,
            BreakOnHandChange = false,
            BreakOnDropItem = false,
            NeedHand = false,
            // Nothing gets to speed up or slow down the mutation, and it keeps the delay in step
            // with the jitter duration below.
            MultiplyDelay = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs, out var doAfterId))
            return;

        var pending = EnsureComp<PendingGreaterMutantComponent>(uid);
        pending.DoAfter = doAfterId;

        _jitter.DoJitter(uid, TransformDelay, true, 80, 8, true);

        _popup.PopupEntity(Loc.GetString("greater-mutant-transform-start-self"), uid, uid, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("greater-mutant-transform-start-others", ("target", Identity.Entity(uid, EntityManager))),
            uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);
    }

    /// <summary>
    /// Losing mutations part-way through calls the whole thing off.
    /// </summary>
    private void OnPendingMutationRemoved(Entity<PendingGreaterMutantComponent> ent, ref MutationRemovedEvent args)
    {
        if (TryComp<MutantComponent>(ent.Owner, out var mutant) && mutant.ActiveMutations.Count >= MutationThreshold)
            return;

        var target = Identity.Entity(ent.Owner, EntityManager);

        // Cancel raises the DoAfter event synchronously, so OnTransformDoAfter does the cleanup.
        _doAfter.Cancel(ent.Comp.DoAfter);

        _popup.PopupEntity(Loc.GetString("greater-mutant-transform-abort-self"), ent.Owner, ent.Owner);
        _popup.PopupEntity(Loc.GetString("greater-mutant-transform-abort-others", ("target", target)),
            ent.Owner, Filter.PvsExcept(ent.Owner), true);
    }

    private void OnTransformDoAfter(Entity<PendingGreaterMutantComponent> ent, ref GreaterMutantTransformDoAfterEvent args)
    {
        // Single exit point for the transform however it ended -- including cancels the DoAfter
        // system raises on its own. Leaving the marker behind would block them ever transforming.
        _statusEffects.TryRemoveStatusEffect(ent.Owner, JitterKey);
        RemComp<PendingGreaterMutantComponent>(ent);

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        // Re-check rather than trust the component: the threshold could have been crossed back and
        // forth, or the mutations stripped by something that does not raise MutationRemovedEvent.
        if (!TryComp<MutantComponent>(ent.Owner, out var mutant) || mutant.ActiveMutations.Count < MutationThreshold)
            return;

        var name = Identity.Entity(ent.Owner, EntityManager);

        // Popup goes on the mutant, not the victim: by now the victim has been banished to the
        // paused map and nobody has it in PVS.
        if (TransformToGreaterMutant(ent.Owner) is not { } mutantUid)
            return;

        _popup.PopupEntity(Loc.GetString("greater-mutant-transform-end-others", ("target", name)),
            mutantUid, Filter.Pvs(mutantUid), true, PopupType.LargeCaution);
    }

    private void OnMutationRemoved(Entity<GreaterMutantComponent> ent, ref MutationRemovedEvent args)
    {
        if (TryComp<MutantComponent>(args.Entity, out var mutant) && mutant.ActiveMutations.Count > 0)
            return;

        // The brain restore rides on PolymorphedEvent rather than happening here, so that any
        // other route back (admin revert, a future gene therapy, etc.) restores the player too.
        if (TryComp<PolymorphedEntityComponent>(args.Entity, out var polymorphed))
            _polymorph.Revert((args.Entity, polymorphed));
    }

    /// <returns>The spawned greater mutant, or null if the transform did not happen.</returns>
    public EntityUid? TransformToGreaterMutant(EntityUid uid)
    {
        if (!TryComp<MutantComponent>(uid, out var mutant) || mutant.ActiveMutations.Count < MutationThreshold)
            return null;

        if (HasComp<GreaterMutantComponent>(uid))
            return null;

        var potentialMutantSystem = EntityManager.System<PotentialMutantSystem>();
        var mutations = mutant.ActiveMutations.ToList();

        // The brain has to come out *before* the polymorph. BrainSystem moves the mind into the
        // organ when it leaves the body, so pulling it first lands the player in the brain and
        // leaves the humanoid mindless -- which in turn makes PolymorphEntity find no mind to
        // hand to the mutant, so the mutant stays under HTN control.
        EntityUid? brainUid = null;
        EntityUid? brainPart = null;
        var brainSlot = string.Empty;

        var brains = _body.GetBodyOrganEntityComps<BrainComponent>(uid);
        if (brains.Count > 0)
        {
            var brain = brains[0];

            // Remember where it came from so a revert can put it back in the right slot. Prefer the
            // container's own id over OrganComponent.SlotId, which is only populated if a species
            // prototype bothered to set it.
            if (_container.TryGetContainingContainer((brain.Owner, null, null), out var organContainer))
            {
                brainPart = organContainer.Owner;
                string containerId = organContainer.ID;
                brainSlot = containerId.StartsWith(SharedBodySystem.OrganSlotContainerIdPrefix)
                    ? containerId[SharedBodySystem.OrganSlotContainerIdPrefix.Length..]
                    : brain.Comp2.SlotId;
            }

            if (_body.RemoveOrgan(brain.Owner, brain.Comp2))
                brainUid = brain.Owner;
        }

        var polymorphedUid = _polymorph.PolymorphEntity(uid, new PolymorphConfiguration
        {
            Entity = "MobGreaterMutant",
            TransferDamage = true,
            // The victim is unrecognizable as a mutant, so it keeps the prototype's own name and
            // sprite rather than inheriting theirs.
            TransferName = false,
            TransferHumanoidAppearance = false,
            // Gear is left on the floor where they transformed, so it stays lootable/recoverable
            // even if the mutant is gibbed. The victim reverts naked, by design.
            Inventory = PolymorphInventoryChange.Drop,
            RevertOnCrit = false,
            RevertOnDeath = false,
            AllowRepeatedMorphs = false,
            ShowPopup = false,
            AttachToGridOrMap = true,
        });

        if (polymorphedUid is not { } greaterMutantUid)
        {
            // Put the brain back rather than stranding the player in a loose organ.
            if (brainUid is { } orphanedBrain && brainPart is { } part)
                _body.InsertOrgan(part, orphanedBrain, brainSlot);

            return null;
        }

        var greaterMutant = EnsureComp<GreaterMutantComponent>(greaterMutantUid);
        greaterMutant.StoredHumanoid = uid;

        if (brainUid is { } storedBrain)
        {
            var brainContainer = _container.EnsureContainer<ContainerSlot>(greaterMutantUid, BrainContainerId);
            _container.Insert(storedBrain, brainContainer);

            greaterMutant.StoredBrain = storedBrain;
            greaterMutant.StoredBrainPart = brainPart;
            greaterMutant.StoredBrainSlot = brainSlot;
        }

        potentialMutantSystem.RemoveAllMutations((uid, mutant));

        EnsureComp<MutantComponent>(greaterMutantUid, out var greaterMutantMutant);
        foreach (var mutation in mutations)
            potentialMutantSystem.AddMutation((greaterMutantUid, greaterMutantMutant), mutation);

        return greaterMutantUid;
    }

    private void OnGreaterMutantDestruction(EntityUid uid, GreaterMutantComponent component, Content.Shared.Destructible.DestructionEventArgs args)
    {
        if (component.StoredHumanoid is { Valid: true } humanoid)
            QueueDel(humanoid);

        if (component.StoredBrain is { Valid: true } brain)
            QueueDel(brain);
    }

    private void OnGreaterMutantGibbed(EntityUid uid, GreaterMutantComponent component, ref BeingGibbedEvent args)
    {
        if (component.StoredHumanoid is { Valid: true } humanoid)
            QueueDel(humanoid);

        if (component.StoredBrain is { Valid: true } brain)
            QueueDel(brain);
    }

    /// <summary>
    /// Fires while reverting, after PolymorphSystem has pulled the humanoid back out of polymorph
    /// space and put it on the grid, but before the mutant is deleted. That window is the only safe
    /// point to get the brain out of the mutant and back into its socket.
    /// </summary>
    private void OnGreaterMutantPolymorphed(EntityUid uid, GreaterMutantComponent component, ref PolymorphedEvent args)
    {
        if (!args.IsRevert)
            return;

        var humanoid = args.NewEntity;

        if (component.StoredBrain is not { } brain || TerminatingOrDeleted(brain))
            return;

        // Must leave the container before the mutant is deleted, or the brain (and the player
        // inside it) gets deleted along with it.
        _container.RemoveEntity(uid, brain, force: true);

        // Putting the organ back raises OrganAddedToBodyEvent, which is what BrainSystem uses to
        // clear DebrainedComponent and move the mind out of the brain and back into the body.
        var restored = component.StoredBrainPart is { } part
            && !TerminatingOrDeleted(part)
            && _body.InsertOrgan(part, brain, component.StoredBrainSlot);

        if (!restored)
        {
            // Body part is gone, so there's no socket to return to. Drop the brain next to the
            // humanoid rather than deleting it -- a player is still in there.
            _transform.SetCoordinates(brain, Transform(humanoid).Coordinates);
            _transform.AttachToGridOrMap(brain);
            Log.Warning($"Could not restore brain {ToPrettyString(brain)} into {ToPrettyString(humanoid)}; dropped it instead.");
        }

        component.StoredBrain = null;
        component.StoredBrainPart = null;
        component.StoredBrainSlot = string.Empty;
        component.StoredHumanoid = null;
    }
}
