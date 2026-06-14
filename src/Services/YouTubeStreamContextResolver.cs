using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.APi.Youtube.Models;
using PolyhydraGames.Platforms.Abstractions;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class YouTubeStreamContextResolver : IYouTubeStreamContextResolver
{
    private readonly YouTubeApiOptions _options;
    private readonly IYouTubeLiveBroadcastResolver _broadcastResolver;

    public YouTubeStreamContextResolver(
        YouTubeApiOptions options,
        IYouTubeLiveBroadcastResolver broadcastResolver)
    {
        _options = options;
        _broadcastResolver = broadcastResolver;
    }

    public async Task<YouTubeStreamContext?> ResolveAsync(CancellationToken ct = default)
    {
        var resolvedContext = await _broadcastResolver.ResolveAsync(ct: ct);
        if (resolvedContext is null)
        {
            return null;
        }

        var channelKey = BuildChannelKey(resolvedContext);
        return new YouTubeStreamContext(
            resolvedContext.BroadcastId,
            resolvedContext.LiveChatId,
            resolvedContext.ChannelId,
            resolvedContext.VideoId,
            channelKey);
    }

    private ChannelKey? BuildChannelKey(YouTubeLiveChatContext context)
    {
        if (!Guid.TryParse(_options.Live.OwnerUserId, out var ownerId))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(context.ChannelId))
        {
            return null;
        }

        return new ChannelKey(new UserId(ownerId), Platform.Youtube, context.ChannelId);
    }
}
