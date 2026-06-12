using Google.Apis.YouTube.v3.Data;
using PolyhydraGames.APi.Youtube.Models;

namespace PolyhydraGames.APi.Youtube.Interfaces;

public interface IYouTubeLiveChatGateway
{
    Task<string?> ResolveLiveChatIdAsync(CancellationToken ct = default);
    Task<string?> SendMessageAsync(string liveChatId, string text, CancellationToken ct = default);
    Task<YouTubeLiveChatPage> ListMessagesAsync(string liveChatId, string? pageToken = null, CancellationToken ct = default);
}
