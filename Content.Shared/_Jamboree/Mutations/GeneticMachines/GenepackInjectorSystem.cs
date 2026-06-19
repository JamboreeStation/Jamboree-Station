using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

public sealed class GenepackInjectorSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PotentialMutantSystem _potentialMutant = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GenepackInjectorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<GenepackInjectorComponent, GenepackInjectorDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<GenepackInjectorComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, GenepackInjectorComponent component, ExaminedEvent args)
    {
        if (component.Spent)
        {
            args.PushMarkup(Loc.GetString("genepack-injector-spent"));
            return;
        }
        var count = component.Mutations.Count;
        args.PushMarkup(Loc.GetString("genepack-injector-contains", ("count", count)));
    }

    private void OnAfterInteract(EntityUid uid, GenepackInjectorComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;
        if (component.Spent)
        {
            _popup.PopupClient(Loc.GetString("genepack-injector-spent"), args.User, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, component.InjectTime,
            new GenepackInjectorDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, GenepackInjectorComponent component, GenepackInjectorDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target || component.Spent)
            return;

        // Server-only mutation application — clients can't run the mutation init functions.
        if (_net.IsServer)
        {
            var mutant = EnsureComp<MutantComponent>(target);
            foreach (var id in component.Mutations)
            {
                if (!_proto.TryIndex<MutationPrototype>(id, out var proto))
                    continue;
                _potentialMutant.AddMutation((target, mutant), proto);
            }
        }
        component.Spent = true;
        Dirty(uid, component);
        args.Handled = true;
    }
}
