namespace PolyhydraGames.APi.Youtube.Models;

public enum YouTubeLiveChatPollerPhase
{
    Started,
    NoBroadcast,
    Resolved,
    Rebinding,
    Backoff,
    Stopped
}

public sealed record YouTubeLiveChatPollerState(
    string? BroadcastId,
    string? LiveChatId,
    string? ChannelId,
    string? VideoId,
    string? PageToken,
    IReadOnlyList<string> SeenMessageIds,
    YouTubeLiveChatPollerPhase Phase,
    DateTimeOffset UpdatedUtc);
