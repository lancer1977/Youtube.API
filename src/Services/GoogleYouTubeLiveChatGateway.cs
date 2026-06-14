#if NET10_0_OR_GREATER
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.APi.Youtube.Models;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class GoogleYouTubeLiveChatGateway : IYouTubeLiveChatGateway
{
    private readonly YouTubeService _service;
    private readonly YouTubeApiOptions _options;
    private readonly ILogger<GoogleYouTubeLiveChatGateway> _logger;

    public GoogleYouTubeLiveChatGateway(
        IYouTubeApiClientFactory clientFactory,
        YouTubeApiOptions options,
        ILogger<GoogleYouTubeLiveChatGateway> logger)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        var authStateKey = string.IsNullOrWhiteSpace(options.Live.OwnerUserId)
            ? throw new InvalidOperationException("YouTube live chat requires a configured OwnerUserId auth-state key.")
            : options.Live.OwnerUserId;

        _service = clientFactory.CreateAuthorizedService(authStateKey);
        _options = options;
        _logger = logger;
    }

    public async Task<string?> ResolveLiveChatIdAsync(CancellationToken ct = default)
    {
        if (_options.Live.Enabled)
        {
            var context = await ResolveContextCoreAsync(_options.Live.BroadcastId, _options.Live.ChannelId, ct);
            return context?.LiveChatId;
        }

        return null;
    }

    public async Task<YouTubeLiveChatContext?> ResolveBroadcastContextAsync(
        string broadcastId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(broadcastId))
        {
            throw new ArgumentException("BroadcastId is required.", nameof(broadcastId));
        }

        var request = _service.Videos.List("liveStreamingDetails,snippet");
        request.Id = broadcastId;

        var response = await request.ExecuteAsync(ct);
        var video = response.Items?.FirstOrDefault();
        if (video is null)
        {
            _logger.LogInformation("Broadcast {BroadcastId} was not found.", broadcastId);
            return null;
        }

        return new YouTubeLiveChatContext(
            broadcastId,
            video.LiveStreamingDetails?.ActiveLiveChatId,
            video.Snippet?.ChannelId,
            video.Id);
    }

    public async Task<YouTubeLiveChatContext?> ResolveActiveBroadcastContextAsync(
        string channelId,
        CancellationToken ct = default)
    {
        var request = _service.Search.List("snippet");
        request.Type = "video";
        request.ChannelId = channelId;
        request.EventType = SearchResource.ListRequest.EventTypeEnum.Live;
        request.MaxResults = 1;
        request.Order = SearchResource.ListRequest.OrderEnum.Date;

        var response = await request.ExecuteAsync(ct);
        var liveVideoId = response.Items?.Select(item => item.Id?.VideoId).FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
        if (liveVideoId is null)
        {
            _logger.LogInformation("No active live broadcast found for channel {ChannelId}.", channelId);
            return null;
        }

        return await ResolveBroadcastContextAsync(liveVideoId, ct);
    }

    public async Task<YouTubeLiveChatMessageBatch> ListMessagesAsync(
        string liveChatId,
        string? pageToken = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(liveChatId))
        {
            throw new ArgumentException("Live chat id is required.", nameof(liveChatId));
        }

        var request = _service.LiveChatMessages.List(liveChatId, "id,snippet,authorDetails");
        request.PageToken = pageToken;
        request.MaxResults = 200;

        var response = await request.ExecuteAsync(ct);
        var messages = response.Items?.Select(MapMessage).ToList() ?? [];
        return new YouTubeLiveChatMessageBatch(
            messages,
            response.NextPageToken,
            response.PollingIntervalMillis is > 0
                ? TimeSpan.FromMilliseconds(response.PollingIntervalMillis.Value)
                : TimeSpan.FromSeconds(5));
    }

    public async Task<string?> SendMessageAsync(string liveChatId, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(liveChatId))
        {
            throw new ArgumentException("liveChatId is required.", nameof(liveChatId));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Attempted to send an empty message to live chat.");
            return null;
        }

        var message = new LiveChatMessage
        {
            Snippet = new LiveChatMessageSnippet
            {
                LiveChatId = liveChatId,
                Type = "textMessageEvent",
                TextMessageDetails = new LiveChatTextMessageDetails
                {
                    MessageText = text
                }
            }
        };

        var request = _service.LiveChatMessages.Insert(message, "snippet");
        var response = await request.ExecuteAsync(ct);
        return response?.Id;
    }

    private YouTubeLiveChatMessage MapMessage(LiveChatMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Id))
        {
            throw new InvalidOperationException("Malformed live chat response payload: message id is missing.");
        }

        var author = message.AuthorDetails;
        var snippet = message.Snippet;

        return new YouTubeLiveChatMessage(
            message.Id,
            snippet?.LiveChatId,
            null,
            null,
            snippet?.TextMessageDetails?.MessageText ?? snippet?.DisplayMessage ?? string.Empty,
            snippet?.PublishedAtDateTimeOffset ?? DateTimeOffset.UtcNow,
            new YouTubeLiveChatAuthor(
                author?.ChannelId,
                author?.DisplayName,
                author?.IsChatOwner == true,
                author?.IsChatModerator == true,
                author?.IsChatSponsor == true,
                author?.IsVerified == true),
            message);
    }

    private async Task<YouTubeLiveChatContext?> ResolveContextCoreAsync(
        string? broadcastId,
        string? channelId,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(broadcastId))
        {
            var broadcast = await ResolveBroadcastContextAsync(broadcastId, ct);
            if (broadcast?.LiveChatId is not null)
            {
                return broadcast;
            }

            _logger.LogWarning("Configured broadcast {BroadcastId} has no active live chat id.", broadcastId);
            return null;
        }

        if (_options.Live.AutoDiscoverActiveBroadcast && !string.IsNullOrWhiteSpace(channelId))
        {
            return await ResolveActiveBroadcastContextAsync(channelId, ct);
        }

        return null;
    }
}
#endif
