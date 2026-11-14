// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._EinsteinEngines.Language.Components.Translators;

namespace Content.Shared._EinsteinEngines.Language.Components;

/// <summary>
///     An implant that allows the implantee to speak and understand other languages.
/// </summary>
[RegisterComponent]
public sealed partial class TranslatorImplantComponent : BaseTranslatorComponent
{
    /// <summary>
    ///     Whether the implantee knows the languages necessary to speak using this implant.
    /// </summary>
    public bool SpokenRequirementSatisfied = false;

    /// <summary>
    ///     Whether the implantee knows the languages necessary to understand translations of this implant.
    /// </summary>
    public bool UnderstoodRequirementSatisfied = false;

    /// <summary>
    ///     If true, grants the implantee the <see cref="UniversalLanguageSpeakerComponent"/> ability,
    ///     letting them understand and speak any language.
    /// </summary>
    [DataField("universalLanguageSpeaker")]
    public bool AddUniversalLanguageSpeaker = false;

    /// <summary>
    ///     Whether this implant added the universal language speaker component, so it can be removed safely on deimplantation.
    /// </summary>
    public bool AddedUniversalLanguageSpeaker = false;
}
