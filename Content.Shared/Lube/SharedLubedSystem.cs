// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.NameModifier.EntitySystems;

namespace Content.Shared.Lube;

public abstract class SharedLubedSystem : EntitySystem
{
    [Dependency] private readonly NameModifierSystem _nameMod = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LubedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LubedComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
    }

    /// <summary>
    /// Removes the lubed condition and refreshes the target's name.
    /// </summary>
    public void RemoveLubed(EntityUid uid)
    {
        if (RemComp<LubedComponent>(uid))
            _nameMod.RefreshNameModifiers(uid);
    }

    private void OnInit(Entity<LubedComponent> ent, ref ComponentInit args)
    {
        _nameMod.RefreshNameModifiers(ent.Owner);
    }

    private void OnRefreshNameModifiers(Entity<LubedComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("lubed-name-prefix");
    }
}
