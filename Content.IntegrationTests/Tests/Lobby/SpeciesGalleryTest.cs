// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.UIKit.UserInterface.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Lobby;

[TestFixture]
public sealed class SpeciesGalleryTest
{
    [Test]
    public async Task SpeciesTilesShowTheirSpecies()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { InLobby = true });
        var client = pair.Client;
        var entities = client.ResolveDependency<IEntityManager>();
        var prototypes = client.ResolveDependency<IPrototypeManager>();

        await client.WaitAssertion(() => Assert.Multiple(() =>
        {
            foreach (var species in prototypes.EnumeratePrototypes<SpeciesPrototype>().Where(s => s.RoundStart))
            {
                using var gallery = new SpeciesGallery();
                gallery.SetSpeciesList(new[] { species });
                gallery.AddItem(species.ID, 0);

                var view = gallery.Children.Single().Children.Single().Children.Single()
                    .Children.OfType<SpriteView>().Single();
                Assert.That(view.Entity, Is.Not.Null, $"{species.ID} has no preview entity.");
                var doll = view.Entity.Value.Owner;
                var sprite = entities.GetComponent<SpriteComponent>(doll);
                var humanoid = entities.GetComponent<HumanoidAppearanceComponent>(doll);

                Assert.That(humanoid.Species, Is.EqualTo(species.ID), $"Wrong appearance for {species.ID}.");
                Assert.That(species.Sexes, Does.Contain(humanoid.Sex), $"Unsupported sex for {species.ID}.");
                Assert.That(sprite.Visible, Is.True, $"{species.ID} is invisible.");
                Assert.That(sprite.LayerData.Any(layer => layer.Visible && !layer.Blank), Is.True,
                    $"{species.ID} has no visible sprite layers.");
            }
        }));

        await pair.CleanReturnAsync();
    }
}
