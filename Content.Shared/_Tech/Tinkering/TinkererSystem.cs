// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Construction;
using Content.Shared.DoAfter;
using Content.Shared.Tools.Systems;

namespace Content.Shared._Tech.Tinkering;

public sealed class TinkererSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TinkererComponent, GetWorkDelayMultiplierEvent>(OnGetWorkDelay);
    }

    private void OnGetWorkDelay(Entity<TinkererComponent> ent, ref GetWorkDelayMultiplierEvent args)
    {
        // need to watch this (construction menu)
#pragma warning disable CS0618
        if (args.Event is SharedToolSystem.ToolDoAfterEvent or ConstructionInteractDoAfterEvent or AwaitedDoAfterEvent)
            args.Multiplier *= ent.Comp.DelayMultiplier;
#pragma warning restore CS0618
    }
}

[ByRefEvent]
public record struct GetWorkDelayMultiplierEvent(DoAfterEvent Event, float Multiplier = 1f);