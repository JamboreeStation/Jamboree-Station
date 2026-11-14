// SPDX-FileCopyrightText: 2026 Jamboree <JamboreeBot@proton.me>
//  SPDX-FileCopyrightText: 2026 PureBreadBagel
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Roles;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Jobs;

/// <summary>
/// Installs organs on a mob when its job is applied (Jamboree - CentComm / Deathsquad).
/// Each spawned organ replaces whatever currently occupies the slot it targets,
/// the same way a surgical replacement would. Organs are matched by their
/// <see cref="OrganComponent.SlotId"/>: if a compatible slot doesn't exist on the mob's
/// body template, the organ is voided.
/// Works with cybernetic organs, but accepts any organ prototype.
/// </summary>
public sealed partial class AddOrgan : JobSpecial
{
    /// <summary>
    /// Organ prototypes to install.
    /// </summary>
    [DataField(required: true)]
    public HashSet<EntProtoId> Organs { get; private set; } = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var bodySystem = entMan.System<SharedBodySystem>();
        var containerSystem = entMan.System<SharedContainerSystem>();

        if (!entMan.TryGetComponent<BodyComponent>(mob, out var body))
            return;

        foreach (var proto in Organs)
        {
            var organ = entMan.SpawnEntity(proto, entMan.GetComponent<TransformComponent>(mob).MapPosition);
            if (!entMan.TryGetComponent<OrganComponent>(organ, out var organComp))
            {
                entMan.DeleteEntity(organ);
                continue;
            }

            var slotId = organComp.SlotId;

            // Track down the body part owning the target slot, and the organ currently sitting in it.
            EntityUid? targetPart = null;
            EntityUid? replaced = null;
            OrganComponent? replacedComp = null;

            foreach (var (partId, part) in bodySystem.GetBodyChildren(mob, body))
            {
                if (!part.Organs.ContainsKey(slotId))
                    continue;

                targetPart = partId;

                if (containerSystem.TryGetContainer(partId, SharedBodySystem.GetOrganContainerId(slotId), out var organContainer))
                {
                    foreach (var held in organContainer.ContainedEntities)
                    {
                        if (entMan.TryGetComponent<OrganComponent>(held, out var heldOrgan))
                        {
                            replaced = held;
                            replacedComp = heldOrgan;
                            break;
                        }
                    }
                }

                break;
            }

            if (targetPart is not { } target)
            {
                entMan.DeleteEntity(organ);
                continue;
            }

            // Take the old organ out first, then the slot is free to receive the replacement.
            if (replaced is { } existing && replacedComp is not null)
            {
                bodySystem.RemoveOrgan(existing, replacedComp);
                entMan.QueueDeleteEntity(existing);
            }

            if (!bodySystem.InsertOrgan(target, organ, slotId))
            {
                entMan.DeleteEntity(organ);
            }
        }
    }
}
