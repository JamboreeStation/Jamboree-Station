using Content.Shared._Jamboree.Mutations;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._Jamboree.Abilities.Mutations;

[UsedImplicitly]
public sealed partial class AddMutantActions : MutationFunction
{
    [DataField]
    public List<EntProtoId> Actions = new();

    public override void OnMutate(EntityUid mob, IComponentFactory factory, IEntityManager entityManager, ISerializationManager serializationManager, ISharedPlayerManager playerManager, ILocalizationManager loc, MutantComponent mutantComponent, MutationPrototype proto)
    {
        var actions = entityManager.System<SharedActionsSystem>();
        foreach (var id in Actions)
        {
            EntityUid? actionId = null;
            if (actions.AddAction(mob, ref actionId, id))
            {
                actions.StartUseDelay(actionId);
                // Get or create the list
                if (!mutantComponent.Actions.TryGetValue(proto.ID, out List<EntityUid>? actionList))
                    mutantComponent.Actions.Add(proto.ID, actionList = new());
                actionList.Add(actionId.Value);
            }
        }
    }
}

[UsedImplicitly]
public sealed partial class RemoveMutantActions : MutationFunction
{
    public override void OnMutate(EntityUid mob, IComponentFactory factory, IEntityManager entityManager, ISerializationManager serializationManager, ISharedPlayerManager playerManager, ILocalizationManager loc, MutantComponent mutantComponent, MutationPrototype proto)
    {
        var actions = entityManager.System<SharedActionsSystem>();
        if (mutantComponent.Actions is null
            || !mutantComponent.Actions.TryGetValue(proto.ID, out List<EntityUid>? actionList))
            return;
        // Psionics used some fuckery with serialization, but this seems like the bad way to do it?
        foreach (var actionUid in actionList)
        {
            actions.RemoveAction(mob, actionUid);
        }
    }
}

[UsedImplicitly]
public sealed partial class AddMutantComponents : MutationFunction
{
    /// <summary>
    ///     The list of what Components this mutation adds.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();

    public override void OnMutate(EntityUid mob, IComponentFactory factory, IEntityManager entityManager, ISerializationManager serializationManager, ISharedPlayerManager playerManager, ILocalizationManager loc, MutantComponent mutantComponent, MutationPrototype proto)
    {
        foreach (var entry in Components.Values)
        {
            if (entityManager.HasComponent(mob, entry.Component.GetType()))
                continue;

            var comp = (Component) serializationManager.CreateCopy(entry.Component, notNullableOverride: true);
            comp.Owner = mob;
            entityManager.AddComponent(mob, comp);
        }
    }
}

[UsedImplicitly]
public sealed partial class RemoveMutantComponents : MutationFunction
{
    /// <summary>
    ///     The list of what Components this power removes.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();

    public override void OnMutate(EntityUid mob, IComponentFactory factory, IEntityManager entityManager, ISerializationManager serializationManager, ISharedPlayerManager playerManager, ILocalizationManager loc, MutantComponent mutantComponent, MutationPrototype proto)
    {
        foreach (var (name, _) in Components)
            entityManager.RemoveComponentDeferred(mob, factory.GetComponent(name).GetType());
    }
}