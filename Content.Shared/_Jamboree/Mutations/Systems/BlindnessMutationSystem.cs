using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared._Jamboree.Mutations;

/// <summary>
/// Handles <see cref="BlindnessMutationComponent"/>. Mirrors TemporaryBlindnessSystem:
///  sight is gone while the component is live, and both startup and shutdown just ask BlindableSystem
///  to recompute, so gaining and losing the mutation are exact opposites.
/// </summary>
public sealed class BlindnessMutationSystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blindable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlindnessMutationComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlindnessMutationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BlindnessMutationComponent, CanSeeAttemptEvent>(OnTrySee);
    }

    private void OnStartup(EntityUid uid, BlindnessMutationComponent component, ComponentStartup args)
    {
        _blindable.UpdateIsBlind(uid);
    }

    private void OnShutdown(EntityUid uid, BlindnessMutationComponent component, ComponentShutdown args)
    {
        _blindable.UpdateIsBlind(uid);
    }

    private void OnTrySee(EntityUid uid, BlindnessMutationComponent component, CanSeeAttemptEvent args)
    {
        // Once the component is being torn down it must stop vetoing sight, otherwise the
        // UpdateIsBlind we run from OnShutdown would just blind them again.
        if (component.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }
}
