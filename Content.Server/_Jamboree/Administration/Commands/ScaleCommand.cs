// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Server._Jamboree.Administration.Commands;

[AdminCommand(AdminFlags.VarEdit)]
public sealed class ScaleCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override string Command => "scale";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity) || !EntityManager.TryGetEntity(netEntity, out var uid))
        {
            shell.WriteError(Loc.GetString("shell-could-not-find-entity-with-uid", ("uid", args[0])));
            return;
        }

        if (!Parse.TryFloat(args[1], out var factor) || factor <= 0f)
        {
            shell.WriteError(Loc.GetString("cmd-scale-invalid-factor", ("factor", args[1])));
            return;
        }

        EntityManager.EnsureComponent<ScaleVisualsComponent>(uid.Value);
        var appearance = EntityManager.EnsureComponent<AppearanceComponent>(uid.Value);

        if (!_appearance.TryGetData<Vector2>(uid.Value, ScaleVisuals.Scale, out var oldScale, appearance))
            oldScale = Vector2.One;

        _appearance.SetData(uid.Value, ScaleVisuals.Scale, oldScale * factor, appearance);
        _physics.ScaleFixtures(uid.Value, factor);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromOptions(CompletionHelper.NetEntities(args[0], EntityManager)),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-hint-float")),
            _ => CompletionResult.Empty,
        };
    }
}
