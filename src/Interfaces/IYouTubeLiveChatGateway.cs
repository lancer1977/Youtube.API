using PolyhydraGames.APi.Youtube.Models;

namespace PolyhydraGames.APi.Youtube.Interfaces;

public interface IYouTubeLiveChatGateway : IYouTubeLiveChatService
{
}

public interface IYouTubeLiveChatService
{
    Task<string?> ResolveLiveChatIdAsync(CancellationToken ct = default);

    Task<YouTubeLiveChatContext?> ResolveBroadcastContextAsync(
        string broadcastId,
        CancellationToken ct = default);

    Task<YouTubeLiveChatContext?> ResolveActiveBroadcastContextAsync(
        string channelId,
        CancellationToken ct = default);

    Task<YouTubeLiveChatMessageBatch> ListMessagesAsync(
        string liveChatId,
        string? pageToken = null,
        CancellationToken ct = default);

    Task<string?> SendMessageAsync(
        string liveChatId,
        string text,
        CancellationToken ct = default);
}
