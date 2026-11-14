// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Content.Shared.Explosion.Components;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Activates <see cref="ExplosiveComponent"/> to explode.
/// </summary>
[RegisterComponent, Access(typeof(XAETriggerExplosivesSystem))]
public sealed partial class XAETriggerExplosivesComponent : Component;
