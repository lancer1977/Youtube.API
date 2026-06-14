namespace PolyhydraGames.APi.Youtube.Interfaces;

public interface IYouTubeLiveBroadcastResolver
{
    Task<PolyhydraGames.APi.Youtube.Models.YouTubeLiveChatContext?> ResolveAsync(
        string? broadcastId = null,
        string? channelId = null,
        CancellationToken ct = default);
}
