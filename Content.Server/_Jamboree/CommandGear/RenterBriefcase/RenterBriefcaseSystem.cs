// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Jamboree.RenterBriefcase;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._Jamboree.RenterBriefcase;

public sealed class RenterBriefcaseSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RenterBriefcaseComponent, ComponentRemove>(OnRemoved);
        SubscribeLocalEvent<RenterBriefcaseComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);

        _sawmill = Logger.GetSawmill("pocket_dimension");
    }

    private void OnRemoved(EntityUid uid, RenterBriefcaseComponent comp, ComponentRemove args)
    {
        if (!Deleted(comp.PocketDimensionMap))
            QueueDel(comp.PocketDimensionMap.Value);
    }

    private void OnGetVerbs(EntityUid uid, RenterBriefcaseComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !HasComp<HandsComponent>(args.User))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("pocket-dimension-verb-text"),
            Act = () => HandleActivation(uid, comp, args.User)
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Handles toggling the portal to the pocket dimension.
    /// </summary>
    private void HandleActivation(EntityUid uid, RenterBriefcaseComponent comp, EntityUid user)
    {
        if (Deleted(comp.PocketDimensionMap))
        {
            if (!_mapLoader.TryLoadMap(comp.PocketDimensionPath, out var map, out var roots,
                options: new Robust.Shared.EntitySerialization.DeserializationOptions { InitializeMaps = true }))
            {
                _sawmill.Error($"Failed to load pocket dimension map {comp.PocketDimensionPath}");
                QueueDel(map);
                return;
            }

            comp.PocketDimensionMap = map;

            bool foundGrid = false;
            foreach (var root in roots)
            {
                if (!HasComp<MapGridComponent>(root))
                    continue;

                var pos = new EntityCoordinates(root, 5, -3);
                comp.ExitPortal = Spawn(comp.ExitPortalPrototype, pos);
                EnsureComp<PortalComponent>(comp.ExitPortal!.Value, out var portal);
                EnsureComp<LinkedEntityComponent>(uid);
                portal.CanTeleportToOtherMaps = true;

                _sawmill.Info($"Created pocket dimension on grid {root} of map {map}");

                _link.OneWayLink(comp.ExitPortal.Value, uid);
                foundGrid = true;
                break;
            }
            if (!foundGrid)
            {
                _sawmill.Error($"Pocket dimension {comp.PocketDimensionPath} had no grids!");
                QueueDel(comp.PocketDimensionMap);
                return;
            }
        }

        var dimension = comp.ExitPortal!.Value;
        if (comp.PortalEnabled)
        {
            comp.PortalEnabled = false;

            _appearance.SetData(uid, RenterBriefcaseComponent.BriefcaseVisuals.State, RenterBriefcaseComponent.BriefcaseVisualState.Closing);
            _audio.PlayPvs(comp.ClosePortalSound, uid);

            Timer.Spawn(TimeSpan.FromMilliseconds(500),
                () =>
                {
                    _appearance.SetData(uid,
                        RenterBriefcaseComponent.BriefcaseVisuals.State,
                        RenterBriefcaseComponent.BriefcaseVisualState.Icon);

                    _link.TryUnlink(dimension, uid);

                    _link.OneWayLink(dimension, uid);
                });
        }
        else
        {
            comp.PortalEnabled = true;

            _appearance.SetData(uid, RenterBriefcaseComponent.BriefcaseVisuals.State, RenterBriefcaseComponent.BriefcaseVisualState.Opening);
            _audio.PlayPvs(comp.OpenPortalSound, uid);

            Timer.Spawn(TimeSpan.FromMilliseconds(500),
                () =>
                {
                    _appearance.SetData(uid,
                        RenterBriefcaseComponent.BriefcaseVisuals.State,
                        RenterBriefcaseComponent.BriefcaseVisualState.Open);

                    _link.TryUnlink(dimension, uid);

                    _link.TryLink(dimension, uid);
                });
        }
    }
}
