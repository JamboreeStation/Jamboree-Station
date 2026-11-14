// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Lavaland.Procedural.Systems;
using Content.Server.Administration;
using Content.Shared._Lavaland.Procedural.Prototypes;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Lavaland.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class LavalandMappingCommand : IConsoleCommand
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public string Command => "mappinglavaland";

    public string Description => Loc.GetString("cmd-mappinglavaland-desc");

    public string Help => Loc.GetString("cmd-mappinglavaland-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        LavalandMapPrototype? lavalandProto;
        int? lavalandSeed = null;

        switch (args.Length)
        {
            case 0:
                shell.WriteLine(Loc.GetString("cmd-mappinglavaland-missing-prototype"));
                shell.WriteLine(Help);
                return;
            case 1:
                if (!_proto.TryIndex(args[0], out lavalandProto))
                {
                    shell.WriteLine(Loc.GetString("cmd-mappinglavaland-invalid-prototype", ("prototype", args[0])));
                    return;
                }
                break;
            case 2:
                if (!_proto.TryIndex(args[0], out lavalandProto))
                {
                    shell.WriteLine(Loc.GetString("cmd-mappinglavaland-invalid-prototype", ("prototype", args[0])));
                    return;
                }

                if (!ushort.TryParse(args[1], out var targetId))
                {
                    shell.WriteLine(Loc.GetString("cmd-mappinglavaland-invalid-seed", ("seed", args[1])));
                    return;
                }
                lavalandSeed = targetId;
                break;
            default:
                shell.WriteLine(Loc.GetString("shell-need-between-arguments", ("lower", 1), ("upper", 2)));
                shell.WriteLine(Help);
                return;
        }
        var lavalandSys = _entityManager.System<LavalandSystem>();

        if (lavalandSys.GetPreloaderEntity() == null)
            lavalandSys.EnsurePreloaderMap();

        if (!lavalandSys.SetupLavalandPlanet(lavalandProto, out var lavaland, lavalandSeed))
            shell.WriteLine(Loc.GetString("cmd-mappinglavaland-load-failed"));
        else
            shell.WriteLine(Loc.GetString("cmd-mappinglavaland-success", ("map", $"{_entityManager.ToPrettyString(lavaland)}")));
    }
}
