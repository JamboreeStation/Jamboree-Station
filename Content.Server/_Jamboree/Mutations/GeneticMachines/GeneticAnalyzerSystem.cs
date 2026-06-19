using System.Text;
using Content.Shared._Jamboree.Mutations;
using Content.Shared._Jamboree.Mutations.GeneticMachines;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Server._Jamboree.Mutations.GeneticMachines;

public sealed class GeneticAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeneticAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<GeneticAnalyzerComponent, GeneticAnalyzerDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, GeneticAnalyzerComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, component.ScanDelay,
            new GeneticAnalyzerDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, GeneticAnalyzerComponent component, GeneticAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        var sb = new StringBuilder();
        if (TryComp<MutantComponent>(target, out var mutant) && mutant.ActiveMutations.Count > 0)
        {
            sb.AppendLine(Loc.GetString("genetic-analyzer-result-header", ("target", target)));
            foreach (var m in mutant.ActiveMutations)
                sb.AppendLine("- " + m.Name);
        }
        else
        {
            sb.Append(Loc.GetString("genetic-analyzer-result-none"));
        }

        _popup.PopupEntity(sb.ToString(), args.User, args.User, PopupType.Medium);
        args.Handled = true;
    }
}
