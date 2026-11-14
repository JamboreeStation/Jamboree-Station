// SPDX-License-Identifier: MIT

using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared._Funkystation.Stains.Components;
using Content.Shared._Funkystation.Stains.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Hands;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Client._Funkystation.Stains;

public sealed partial class StainSystem : SharedStainSystem
{
    [Dependency] private IPrototypeManager _prototypeManager = null!;
    [Dependency] private SharedSolutionContainerSystem _solution = null!;
    [Dependency] private SpriteSystem _sprite = null!;
    [Dependency] private ISerializationManager _serialization = null!;
    [Dependency] private ItemSystem _items = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StainableComponent, AppearanceChangeEvent>(OnAppearanceChanged);
        SubscribeLocalEvent<StainableComponent, GetEquipmentVisualsEvent>(OnEquipmentVisuals, after: [typeof(ClientClothingSystem)]);
        SubscribeLocalEvent<StainableComponent, GetInhandVisualsEvent>(OnInhandVisuals, after: [typeof(ItemSystem)]);
    }

    private void OnAppearanceChanged(Entity<StainableComponent> ent, ref AppearanceChangeEvent args)
    {
        _items.VisualsChanged(ent.Owner);

        if (args.Sprite == null)
            return;

        var spriteEnt = new Entity<SpriteComponent?>(ent.Owner, args.Sprite);

        var layers = new List<int>(ent.Comp.RevealedLayers);
        layers.Sort((a, b) => b.CompareTo(a));

        foreach (var layer in layers)
        {
            _sprite.RemoveLayer(spriteEnt, layer);
        }

        ent.Comp.RevealedLayers.Clear();

        foreach (var (_, layerData) in BuildVisuals(ent, ent.Comp.IconVisuals, "icon"))
        {
            ent.Comp.RevealedLayers.Add(_sprite.AddLayer(spriteEnt, layerData, null));
        }
    }

    private void OnEquipmentVisuals(Entity<StainableComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (ent.Comp.ClothingVisuals.TryGetValue(args.Slot, out var layers))
            args.Layers.AddRange(BuildVisuals(ent, layers, args.Slot));
    }

    private void OnInhandVisuals(Entity<StainableComponent> ent, ref GetInhandVisualsEvent args)
    {
        var location = args.Location.ToString().ToLowerInvariant();
        if (ent.Comp.ItemVisuals.TryGetValue(location, out var layers))
            args.Layers.AddRange(BuildVisuals(ent, layers, location));
    }

    private IEnumerable<(string, PrototypeLayerData)> BuildVisuals(Entity<StainableComponent> ent, List<PrototypeLayerData> templates, string prefix)
    {
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out _, out var sol) || sol.Volume <= FixedPoint2.Zero)
            yield break;

        var color = sol.GetColor(_prototypeManager);
        for (var i = 0; i < templates.Count; i++)
        {
            var layer = _serialization.CreateCopy(templates[i], notNullableOverride: true);
            layer.Color = color;
            yield return ($"stain-{prefix}-{i}", layer);
        }
    }
}
