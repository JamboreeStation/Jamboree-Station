using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.LinkAccount;

[Serializable, NetSerializable]
public sealed record SharedRMCPatronFull(
    SharedRMCPatronTier? Tier,
    bool Linked,
    Color? GhostColor,
    string? OocIcon, // Goob - ooc icon selection
    SharedRMCLobbyMessage? LobbyMessage,
    SharedRMCRoundEndShoutouts? RoundEndShoutout
);
