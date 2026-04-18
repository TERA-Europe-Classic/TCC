// Classic+ read-only fork: no-op compatibility shim. See StubInterface.cs
// for the rationale — every method here previously serialized a JSON-RPC call
// to tera-toolbox which in turn wrote a game packet. None of that happens
// here; these are pure sinks so existing call sites compile unchanged.

namespace TCC.Interop.Proxy;

public sealed class StubClient
{
    // LFG
    public void RegisterListing(string message, bool isRaid) { _ = message; _ = isRaid; }
    public void RequestListings(int minLevel, int maxLevel) { _ = minLevel; _ = maxLevel; }
    public void RequestListingCandidates() { }
    public void DeclineUserGroupApply(uint playerId, uint serverId) { _ = playerId; _ = serverId; }

    // Group / party
    public void DisbandGroup() { }
    public void LeaveGroup() { }
    public void ResetInstance() { }
    public void KickUser(uint serverId, uint playerId) { _ = serverId; _ = playerId; }
    public void DelegateLeader(uint serverId, uint playerId) { _ = serverId; _ = playerId; }
    public void SetInvitePower(uint serverId, uint playerId, bool canInvite) { _ = serverId; _ = playerId; _ = canInvite; }
    public void GroupInviteUser(string name, bool isRaid) { _ = name; _ = isRaid; }
    public void GuildInviteUser(string name) { _ = name; }

    // Player menu / social
    public void InspectUser(string name, uint serverId) { _ = name; _ = serverId; }
    public void InspectUser(uint gameId) { _ = gameId; }
    public void BlockUser(string name, uint serverId) { _ = name; _ = serverId; }
    public void UnblockUser(string name) { _ = name; }
    public void UnfriendUser(string name) { _ = name; }
    public void UnfriendUser(uint playerId) { _ = playerId; }
    public void AskInteractive(uint serverId, string name) { _ = serverId; _ = name; }

    // Broker chat hyperlinks
    public void AcceptBrokerOffer(uint playerId, uint listingId) { _ = playerId; _ = listingId; }
    public void DeclineBrokerOffer(uint playerId, uint listingId) { _ = playerId; _ = listingId; }

    // Chat
    public void ChatLinkAction(string linkAction) { _ = linkAction; }

    // Game-state write-through (return to lobby, FPS toggles via slash cmd)
    public void ReturnToLobby() { }
    public void InvokeCommand(string command) { _ = command; }

    // Settings sync — kept because UpdateSetting is invoked from a lot of
    // settings ViewModels. Treated as write-through state only the toolbox
    // side cared about; safe to drop silently.
    public void UpdateSetting(string key, bool value) { _ = key; _ = value; }
}
