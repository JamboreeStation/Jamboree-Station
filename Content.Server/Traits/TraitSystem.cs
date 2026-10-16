// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._EinsteinEngines.Language;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Preferences;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits;

public sealed class TraitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Check if player's job allows to apply traits
        if (args.JobId == null ||
            !_prototypeManager.Resolve<JobPrototype>(args.JobId, out var protoJob) ||
            !protoJob.ApplyTraits)
        {
            return;
        }

        ApplyTraits(args.Mob, args.Profile);
    }

  // ALL OF THIS IS FOR THE DNA MOLECULARSYNTHESIS MACHINERY! Jamboree
    public void ApplyTraits(EntityUid mob, HumanoidCharacterProfile profile)
    {
        foreach (var traitId in profile.TraitPreferences)
        {
            if (!_prototypeManager.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
            {
                Log.Error($"No trait found with ID {traitId}!");
                return;
            }

            if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, mob) ||
                _whitelistSystem.IsWhitelistPass(traitPrototype.Blacklist, mob))
                continue;


            if (traitPrototype.IncludedSpecies.Count > 0 && !traitPrototype.IncludedSpecies.Contains(profile.Species) ||
                traitPrototype.ExcludedSpecies.Contains(profile.Species))
                continue;


            // Add all components required by the prototype
            EntityManager.AddComponents(mob, traitPrototype.Components, false);


            var language = EntityManager.System<LanguageSystem>();

            if (traitPrototype.RemoveLanguagesSpoken is not null)
                foreach (var lang in traitPrototype.RemoveLanguagesSpoken)
                    language.RemoveLanguage(mob, lang, true, false);

            if (traitPrototype.RemoveLanguagesUnderstood is not null)
                foreach (var lang in traitPrototype.RemoveLanguagesUnderstood)
                    language.RemoveLanguage(mob, lang, false, true);

            if (traitPrototype.LanguagesSpoken is not null)
                foreach (var lang in traitPrototype.LanguagesSpoken)
                    language.AddLanguage(mob, lang, true, false);

            if (traitPrototype.LanguagesUnderstood is not null)
                foreach (var lang in traitPrototype.LanguagesUnderstood)
                    language.AddLanguage(mob, lang, false, true);


            // Add item required by the trait
            if (traitPrototype.TraitGear == null)
                continue;

            if (!TryComp(mob, out HandsComponent? handsComponent))
                continue;

            var coords = Transform(mob).Coordinates;
            var inhandEntity = Spawn(traitPrototype.TraitGear, coords);
            _sharedHandsSystem.TryPickup(mob,
                inhandEntity,
                checkActionBlocker: false,
                handsComp: handsComponent);
        }
    }
}
