// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server._Jamboree.DnaCloner.Systems;
using Content.Server.Ghost;
using Content.Shared._Jamboree.DnaCloner;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Server.GameObjects;

namespace Content.IntegrationTests.Tests.Cloning;

[TestFixture]
public sealed class CentralCommandDnaClonerTest
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task CandidateListLabelsObserverRolesWithoutChangingEligibility(bool observer)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Dirty = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var mind = server.System<SharedMindSystem>().CreateMind(pair.Player!.UserId, "Cloner Label Test");
            var ghost = server.System<GhostSystem>().SpawnGhost(mind, map.GridCoords);
            Assert.That(ghost, Is.Not.Null);
            if (observer)
                server.System<SharedRoleSystem>().MindAddRole(mind, "MindRoleObserver");

            // Roles live on contained role entities, not on the mind itself.
            Assert.That(entMan.HasComponent<ObserverRoleComponent>(mind), Is.False);
            var console = entMan.SpawnEntity("CentralCommandDnaClonerComputer", map.GridCoords);
            server.System<CentralCommandDnaClonerSystem>().UpdateUserInterface(console);
            Assert.That(server.System<UserInterfaceSystem>()
                .TryGetUiState<CentralCommandDnaClonerConsoleBoundUserInterfaceState>(console,
                    CentralCommandDnaClonerUiKey.Key, out var state), Is.True);

            var row = state.MindNames.Single(candidate => candidate.Id == entMan.GetNetEntity(mind));
            Assert.That(row.Name, Is.EqualTo("Cloner Label Test"));
            Assert.That(row.IsObserver, Is.EqualTo(observer));
        });

        await pair.CleanReturnAsync();
    }
}
