# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
# SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

reagent-effect-guidebook-deal-stamina-damage =
    { $chance ->
        [1] { $deltasign ->
                [1] Deals
                *[-1] Heals
            }
        *[other]
            { $deltasign ->
                [1] deal
                *[-1] heal
            }
    } { $amount } { $immediate ->
                    [true] immediate
                    *[false] overtime
                  } stamina damage

reagent-effect-guidebook-stealth-entities = Camouflages living mobs nearby.

reagent-effect-guidebook-change-faction = Changes the mob's faction to {$faction}.

reagent-effect-guidebook-mutate-plants-nearby =
    { $chance ->
        [1] Increases
        *[other] increase
    } the mutation level of nearby plants

reagent-effect-guidebook-oxygenate-nearby =
    { $chance ->
        [1] Adjusts
        *[other] adjust
    } the respiratory saturation of nearby breathing entities

reagent-effect-guidebook-extinguish-nearby =
    { $chance ->
        [1] Extinguishes
        *[other] extinguish
    } nearby burning entities

reagent-effect-guidebook-ignite-nearby =
    { $chance ->
        [1] Adjusts
        *[other] adjust
    } fire stacks on nearby flammable entities, igniting those with positive stacks

reagent-effect-guidebook-scramble-nearby =
    { $chance ->
        [1] Scrambles
        *[other] scramble
    } the DNA of nearby humanoids

reagent-effect-guidebook-create-entity-reaction-effect =
    { $chance ->
        [1] Creates
        *[other] create
    } { $amount ->
        [1] {INDEFINITE($entname)} {$entname}
        *[other] between 1 and {$amount} {MAKEPLURAL($entname)}
    }

reagent-effect-guidebook-dnascramble =
    { $chance ->
        [1] Scrambles
        *[other] scramble
    } the target's DNA

reagent-effect-guidebook-change-species = Turns the target into a {$species}.

reagent-effect-guidebook-change-species-random = Turns the target into a completely random species.

reagent-effect-guidebook-sex-change = Swaps the person's gender

reagent-effect-guidebook-immunity-modifier =
    { $chance ->
        [1] Modifies
        *[other] modify
    } immunity gain rate by {NATURALFIXED($gainrate, 5)}, strength by {NATURALFIXED($strength, 5)} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)}

reagent-effect-guidebook-disease-progress-change =
    { $chance ->
        [1] Modifies
        *[other] modify
    } progress of {$type} diseases by {NATURALFIXED($amount, 5)}

reagent-effect-guidebook-disease-mutate = Mutates diseases by {NATURALFIXED($amount, 4)}
