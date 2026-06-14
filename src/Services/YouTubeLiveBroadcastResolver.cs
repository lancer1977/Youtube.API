using Google.Apis.Requests;
using Microsoft.Extensions.Logging;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.APi.Youtube.Models;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class YouTubeLiveBroadcastResolver : IYouTubeLiveBroadcastResolver
{
    private readonly IYouTubeLiveChatService _chatService;
    private readonly YouTubeApiOptions _options;
    private readonly ILogger<YouTubeLiveBroadcastResolver> _logger;

    public YouTubeLiveBroadcastResolver(
        IYouTubeLiveChatService chatService,
        YouTubeApiOptions options,
        ILogger<YouTubeLiveBroadcastResolver> logger)
    {
        _chatService = chatService;
        _options = options;
        _logger = logger;
    }

    public Task<YouTubeLiveChatContext?> ResolveAsync(
        string? broadcastId = null,
        string? channelId = null,
        CancellationToken ct = default)
    {
        var resolvedBroadcastId = !string.IsNullOrWhiteSpace(broadcastId)
            ? broadcastId
            : _options.Live.BroadcastId;

        var resolvedChannelId = !string.IsNullOrWhiteSpace(channelId)
            ? channelId
            : _options.Live.ChannelId;

        return ResolveContextInternalAsync(resolvedBroadcastId, resolvedChannelId, ct);
    }

    private async Task<YouTubeLiveChatContext?> ResolveContextInternalAsync(
        string? broadcastId,
        string? channelId,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(broadcastId))
        {
            return await _chatService.ResolveBroadcastContextAsync(broadcastId, ct);
        }

        if (!string.IsNullOrWhiteSpace(channelId) && _options.Live.AutoDiscoverActiveBroadcast)
        {
            return await _chatService.ResolveActiveBroadcastContextAsync(channelId, ct);
        }

        _logger.LogDebug("No broadcast/channel configured for live stream context resolution.");
        return null;
    }
}
