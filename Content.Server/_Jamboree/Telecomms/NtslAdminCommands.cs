// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Jamboree.Telecomms;

[AdminCommand(AdminFlags.Admin)]
public sealed class NtslListCommand : LocalizedEntityCommands
{
    public override string Command => "ntsllist";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var any = false;
        var query = EntityManager.EntityQueryEnumerator<NtslServerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Code.Length == 0)
                continue;

            any = true;
            shell.WriteLine(Loc.GetString("cmd-ntsllist-entry",
                ("server", EntityManager.ToPrettyString(uid)),
                ("enabled", comp.Enabled ? "true" : "false"),
                ("length", comp.Code.Length),
                ("editor", comp.LastEditor ?? "?")));
        }

        if (!any)
            shell.WriteLine(Loc.GetString("cmd-ntsllist-none"));
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class NtslShowCommand : LocalizedEntityCommands
{
    public override string Command => "ntslshow";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 1),
                ("currentAmount", args.Length)));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity)
            || !EntityManager.TryGetEntity(netEntity, out var server)
            || !EntityManager.TryGetComponent<NtslServerComponent>(server, out var comp)
            || comp.Code.Length == 0)
        {
            shell.WriteError(Loc.GetString("cmd-ntsl-no-script", ("server", args[0])));
            return;
        }

        shell.WriteLine(comp.Code);
        foreach (var line in comp.Log)
        {
            shell.WriteLine(line);
        }
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class NtslWipeCommand : LocalizedEntityCommands
{
    [Dependency] private readonly NtslTelecomSystem _ntsl = default!;

    public override string Command => "ntslwipe";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 1),
                ("currentAmount", args.Length)));
            return;
        }

        var user = shell.Player?.AttachedEntity;
        if (args[0] == "all")
        {
            var count = 0;
            var query = EntityManager.EntityQueryEnumerator<NtslServerComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.Code.Length == 0)
                    continue;

                _ntsl.Clear(uid, user);
                count++;
            }

            shell.WriteLine(Loc.GetString("cmd-ntslwipe-all", ("count", count)));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity)
            || !EntityManager.TryGetEntity(netEntity, out var server)
            || !EntityManager.HasComponent<NtslServerComponent>(server))
        {
            shell.WriteError(Loc.GetString("cmd-ntsl-no-script", ("server", args[0])));
            return;
        }

        _ntsl.Clear(server.Value, user);
        shell.WriteLine(Loc.GetString("cmd-ntslwipe-one", ("server", EntityManager.ToPrettyString(server.Value))));
    }
}
