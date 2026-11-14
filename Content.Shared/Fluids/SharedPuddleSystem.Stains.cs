// SPDX-License-Identifier: MIT

using Content.Shared._Funkystation.Fluids;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Standing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!; // Funky - Clothing stains
    [Dependency] private StandingStateSystem _standing = default!; // Moff - Clothing stains
    [Dependency] private SharedGravitySystem _gravity = default!; // Moff - Clothing Stains

    // Collision events also cover puddles whose step trigger is used for slipping.
    private void OnStepInPuddle(Entity<PuddleComponent> ent, ref StartCollideEvent args)
    {
        var stepper = args.OtherEntity;

        if (!_solutionContainerSystem.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return;

        if (solution.Volume <= FixedPoint2.Zero)
            return;

        if (TryComp<PhysicsComponent>(stepper, out var physicsComp)
            && (physicsComp.BodyStatus == BodyStatus.InAir || _gravity.IsWeightless(stepper)))
            return;

        EntityUid target;
        if (_standing.IsDown(stepper))
            target = stepper;
        else if (_inventory.TryGetSlotEntity(stepper, "shoes", out var shoes) && shoes is { } shoeUid)
            target = shoeUid;
        else
            return;

        var spilledEvent = new SpilledOnEvent(ent.Owner, solution);
        RaiseLocalEvent(target, spilledEvent);

        // Spill handlers can synchronously remove or replace the source solution.
        if (TerminatingOrDeleted(ent.Owner)
            || !_solutionContainerSystem.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var current, out var currentSolution)
            || TerminatingOrDeleted(current.Value.Owner)
            || !ReferenceEquals(currentSolution, solution))
            return;

        _solutionContainerSystem.UpdateChemicals(current.Value);
    }
}
