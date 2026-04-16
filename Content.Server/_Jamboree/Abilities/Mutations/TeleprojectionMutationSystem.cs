using Content.Shared._Jamboree.Mutations;
using Content.Shared._Jamboree.Actions.Events;
using Content.Shared._Jamboree.Abilities.Mutations;

namespace Content.Server._Jamboree.Abilities.Mutations
{
    public sealed class TeleprojectionMutationSystem : EntitySystem
    {
        [Dependency] private readonly MindSwapMutationSystem _mindSwap = default!;
        [Dependency] private readonly PotentialMutantSystem _mutant = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TeleprojectionMutationComponent, TeleprojectionMutationActionEvent>(OnPowerUsed);
        }

        private void OnPowerUsed(EntityUid uid, TeleprojectionMutationComponent component, TeleprojectionMutationActionEvent args)
        {
            if (!_mutant.OnAttemptMutantAbilityUse(args.Performer, "teleprojection", true))
                return;

            var projection = Spawn(component.Prototype, Transform(uid).Coordinates);
            Transform(projection).AttachToGridOrMap();
            _mindSwap.Swap(uid, projection);

            _mutant.LogAbilityUsed(uid, "teleprojection");
            args.Handled = true;
        }
    }
}
