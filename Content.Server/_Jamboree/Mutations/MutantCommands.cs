// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Shared._Jamboree.Mutations;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Jamboree.Mutations;

[AdminCommand(AdminFlags.Logs)]
public sealed class ListMutantsCommand : IConsoleCommand
{
    public string Command => "lsmutants";
    public string Description => Loc.GetString("command-lsmutants-description");
    public string Help => Loc.GetString("command-lsmutants-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        foreach (var (actor, mutant, meta) in entMan.EntityQuery<ActorComponent, MutantComponent, MetaDataComponent>())
        {
            var mutationList = new List<string>();
            foreach (var power in mutant.ActiveMutations)
                mutationList.Add(power.Name);

            shell.WriteLine(meta.EntityName + " (" + meta.Owner + ") - " + actor.PlayerSession.Name + mutationList);
        }
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class AddMutationCommand : IConsoleCommand
{
    public string Command => "addmutation";
    public string Description => Loc.GetString("command-addmutation-description");
    public string Help => Loc.GetString("command-addmutation-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var mutantSys = entMan.System<PotentialMutantSystem>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid))
        {
            shell.WriteError(Loc.GetString("addmutation-args-one-error"));
            return;
        }

        if (!protoMan.TryIndex<MutationPrototype>(args[1], out var mutation))
        {
            shell.WriteError(Loc.GetString("addmutation-args-two-error"));
            return;
        }

        entMan.EnsureComponent<MutantComponent>(uid, out var mutant);
        mutantSys.AddMutation(new(uid, mutant), mutation);
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class AddRandomMutationCommand : IConsoleCommand
{
    public string Command => "addrandommutation";
    public string Description => Loc.GetString("command-addrandommutation-description");
    public string Help => Loc.GetString("command-addrandommutation-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var mutantSys = entMan.System<PotentialMutantSystem>();

        if (!EntityUid.TryParse(args[0], out var uid))
        {
            shell.WriteError(Loc.GetString("addrandommutation-args-one-error"));
            return;
        }

        if (entMan.TryGetComponent<PotentialMutantComponent>(uid, out var potentialMutant))
            mutantSys.TryGainRandomMutation(new(uid, potentialMutant));
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class RemoveMutationCommand : IConsoleCommand
{
    public string Command => "removemutation";
    public string Description => Loc.GetString("command-removemutation-description");
    public string Help => Loc.GetString("command-addmutation-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var mutantSys = entMan.System<PotentialMutantSystem>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid))
        {
            shell.WriteError(Loc.GetString("removemutation-args-one-error"));
            return;
        }

        if (!protoMan.TryIndex<MutationPrototype>(args[1], out var mutation))
        {
            shell.WriteError(Loc.GetString("removemutation-args-two-error"));
            return;
        }

        if (!entMan.TryGetComponent<MutantComponent>(uid, out var mutant))
        {
            shell.WriteError(Loc.GetString("removemutation-not-mutant-error"));
            return;
        }

        if (!mutant.ActiveMutations.Contains(mutation))
        {
            shell.WriteError(Loc.GetString("removemutation-not-contains-error"));
            return;
        }

        mutantSys.RemoveMutation(new(uid, mutant), mutation);
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class RemoveAllMutationsCommand : IConsoleCommand
{
    public string Command => "removeallmutations";
    public string Description => Loc.GetString("command-removeallmutations-description");
    public string Help => Loc.GetString("command-removeallpsremoveallmutationsionicpowers-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var mutantSys = entMan.System<PotentialMutantSystem>();

        if (!EntityUid.TryParse(args[0], out var uid))
        {
            shell.WriteError(Loc.GetString("removeallmutations-args-one-error"));
            return;
        }

        if (!entMan.TryGetComponent<MutantComponent>(uid, out var mutant))
        {
            shell.WriteError(Loc.GetString("removeallmutations-not-mutant-error"));
            return;
        }


        mutantSys.RemoveAllMutations(new(uid, mutant));
    }
}
