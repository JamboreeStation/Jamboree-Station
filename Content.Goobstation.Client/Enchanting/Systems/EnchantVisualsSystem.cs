// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Enchanting.Components;
using Content.Goobstation.Shared.Enchanting.Systems;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Goobstation.Client.Enchanting.Systems;

/// <summary>
/// Gives enchanted items a cool shader
/// </summary>
public sealed class EnchantVisualsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public readonly ProtoId<ShaderPrototype> Shader = "Enchant";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnchantedComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<EnchantedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<EnchantedComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<EnchantedComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
    }

    private void OnStartup(Entity<EnchantedComponent> ent, ref ComponentStartup args)
    {
        SetShader(ent);
    }

    private void OnHandleState(Entity<EnchantedComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // Initial network state arrives before Sprite initializes its post-shaders.
        if (!ent.Comp.Running)
            return;

        SetShader(ent);
    }

    private void SetShader(Entity<EnchantedComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        sprite.PostShader = _proto.Index(Shader).InstanceUnique();
    }

    private void OnHeldVisualsUpdated(Entity<EnchantedComponent> ent, ref HeldVisualsUpdatedEvent args)
    {
        SetLayers(args.User, args.RevealedLayers);
    }

    private void OnEquipmentVisualsUpdated(Entity<EnchantedComponent> ent, ref EquipmentVisualsUpdatedEvent args)
    {
        SetLayers(args.Equipee, args.RevealedLayers);
    }

    private void SetLayers(EntityUid uid, HashSet<string> keys)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        foreach (var key in keys)
        {
            if (sprite.LayerMapTryGet(key, out var index))
                sprite.LayerSetShader(index, Shader);
        }
    }
}
