using Content.Client.Examine;
using Content.Shared._FarHorizons.Power.Generation.FissionGenerator;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._FarHorizons.Power.Generation.FissionGenerator;

// Adapted from Far Horizons' GasTurbineSystem by Jhrushbe:
// https://github.com/Far-Horizons-SS14/Far-Horizons-SS14/blob/ab400f143a7901d845819c003136e3253f39f8af/Content.Client/_FarHorizons/Power/Generation/FissionGenerator/GasTurbineSystem.cs
// This file used to have code ported from goonstation under CC-BY-NC-SA-3.0:
// https://github.com/goonstation/goonstation/blob/ff86b044/code/obj/nuclearreactor/turbine.dm
public sealed class TurbineSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animations = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprites = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const string AnimationKey = "turbine-speed";
    private static readonly EntProtoId SparkEffect = "GasTurbineSparkEffect";
    private static readonly EntProtoId SmokeEffect = "GasTurbineSmokeEffect";
    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurbineComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TurbineComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<TurbineComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<TurbineComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeLocalEvent<TurbineComponent, ClientExaminedEvent>(OnExamined);
    }

    private void OnStartup(EntityUid uid, TurbineComponent comp, ComponentStartup args) => UpdateAnimation(uid, comp);

    private void OnAfterHandleState(EntityUid uid, TurbineComponent comp, ref AfterAutoHandleStateEvent args) => UpdateAnimation(uid, comp);

    private void OnAppearanceChange(EntityUid uid, TurbineComponent comp, ref AppearanceChangeEvent args) => UpdateAnimation(uid, comp);

    private void OnExamined(EntityUid uid, TurbineComponent comp, ClientExaminedEvent args) => Spawn(comp.ArrowPrototype, new EntityCoordinates(uid, 0, 0));

    private void OnAnimationCompleted(EntityUid uid, TurbineComponent comp, AnimationCompletedEvent args)
    {
        // Stop also raises this event synchronously; only loop natural completions.
        if (args.Key == AnimationKey && args.Finished)
            UpdateAnimation(uid, comp);
    }

    public override void FrameUpdate(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator < 1f)
            return;

        _accumulator %= 1f;
        var query = EntityQueryEnumerator<TurbineComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var comp, out var metadata))
        {
            if ((metadata.Flags & MetaDataFlags.Detached) != 0)
                continue;

            // Also recover animations after PVS changes or long, low-RPM cycles.
            UpdateAnimation(uid, comp);
            UpdateParticles(uid);
        }
    }

    private void UpdateAnimation(EntityUid uid, TurbineComponent comp)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !_sprites.LayerMapTryGet((uid, sprite), TurbineVisualLayers.TurbineSpeed, out var layerId, false) ||
            !_sprites.TryGetLayer((uid, sprite), layerId, out var layer, false))
            return;

        if ((MetaData(uid).Flags & MetaDataFlags.Detached) != 0 ||
            comp.Ruined || comp.RPM <= 1 || !float.IsFinite(comp.RPM) ||
            !float.IsFinite(comp.BestRPM) || comp.BestRPM <= 0)
        {
            _animations.Stop(uid, null, AnimationKey);
            _sprites.LayerSetRsiState(layer, "turbine");
            comp.AnimRPM = 0;
            return;
        }

        // A BestRPM-only threshold leaves very slow cycles stale for minutes.
        var tolerance = Math.Min(comp.BestRPM, comp.AnimRPM) * 0.1f;
        if (Math.Abs(comp.RPM - comp.AnimRPM) > tolerance)
            _animations.Stop(uid, null, AnimationKey);

        if (_animations.HasRunningAnimation(uid, AnimationKey))
            return;

        comp.AnimRPM = comp.RPM;
        var duration = 0.5f * comp.BestRPM / comp.RPM;
        var track = new AnimationTrackSpriteFlick { LayerKey = TurbineVisualLayers.TurbineSpeed };
        for (var i = 0; i < 12; i++)
            track.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame($"turbinerun_{i:00}", i == 0 ? 0 : duration / 12));

        _animations.Play(uid, new Animation
        {
            Length = TimeSpan.FromSeconds(duration),
            AnimationTracks = { track },
        }, AnimationKey);
    }

    private void UpdateParticles(EntityUid uid)
    {
        // Damage flags already replicate through appearance data in this port.
        _appearance.TryGetData<bool>(uid, TurbineVisuals.DamageSpark, out var sparking);
        _appearance.TryGetData<bool>(uid, TurbineVisuals.DamageSmoke, out var smoking);
        if ((!sparking && !smoking) || Transform(uid).MapUid == null)
            return;

        var coordinates = _transform.GetMoverCoordinates(uid);
        if (sparking)
            Spawn(SparkEffect, coordinates.Offset(new(_random.NextFloat(-1, 1), _random.NextFloat(-1, 1))));
        if (smoking)
            Spawn(SmokeEffect, coordinates.Offset(new(_random.NextFloat(-1, 1), _random.NextFloat(-1, 1))));
    }
}
