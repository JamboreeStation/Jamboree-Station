using Robust.Shared.GameStates;

namespace Content.Shared._Jamboree.Mutations;

[RegisterComponent, NetworkedComponent]
public sealed partial class GreaterMutantComponent : Component
{
    [DataField(serverOnly: true)]
    public EntityUid? StoredHumanoid;

    [DataField(serverOnly: true)]
    public EntityUid? StoredBrain;

    /// <summary>
    /// Body part the brain was taken from, so a revert can put it back where it belongs.
    /// </summary>
    [DataField(serverOnly: true)]
    public EntityUid? StoredBrainPart;

    /// <summary>
    /// Organ slot on <see cref="StoredBrainPart"/> the brain came out of.
    /// </summary>
    [DataField(serverOnly: true)]
    public string StoredBrainSlot = string.Empty;
}
