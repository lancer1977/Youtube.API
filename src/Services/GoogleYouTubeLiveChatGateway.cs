#if NET10_0_OR_GREATER
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Interfaces;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class GoogleYouTubeLiveChatGateway : IYouTubeLiveChatGateway
{
    private readonly YouTubeService _service;
    private readonly YouTubeApiOptions _options;

    public GoogleYouTubeLiveChatGateway(IYoutubeConfig config, YouTubeApiOptions options)
    {
        _options = options;
        _service = YoutubeQueryService.CreateService(config);
    }

    public async Task<string?> ResolveLiveChatIdAsync(CancellationToken ct = default)
    {
        if (!_options.Live.Enabled)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(_options.Live.BroadcastId))
        {
            return await ResolveFromBroadcastAsync(_options.Live.BroadcastId, ct);
        }

        if (_options.Live.AutoDiscoverActiveBroadcast && !string.IsNullOrWhiteSpace(_options.Live.ChannelId))
        {
            return await ResolveFromActiveBroadcastAsync(_options.Live.ChannelId, ct);
        }

        return null;
    }

    public async Task<string?> SendMessageAsync(string liveChatId, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(liveChatId))
        {
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
        return response.Id;
    }

    public async Task<IReadOnlyList<LiveChatMessage>> ListMessagesAsync(string liveChatId, string? pageToken = null, CancellationToken ct = default)
    {
        var request = _service.LiveChatMessages.List(liveChatId, "id,snippet,authorDetails");
        request.MaxResults = 200;
        request.PageToken = pageToken;

        var response = await request.ExecuteAsync(ct);
        return response.Items is null ? Array.Empty<LiveChatMessage>() : response.Items.ToList();
    }

    private async Task<string?> ResolveFromBroadcastAsync(string broadcastId, CancellationToken ct)
    {
        var request = _service.Videos.List("liveStreamingDetails");
        request.Id = broadcastId;

        var response = await request.ExecuteAsync(ct);
        return response.Items?.FirstOrDefault()?.LiveStreamingDetails?.ActiveLiveChatId;
    }

    private async Task<string?> ResolveFromActiveBroadcastAsync(string channelId, CancellationToken ct)
    {
        var request = _service.Search.List("snippet");
        request.Type = "video";
        request.ChannelId = channelId;
        request.EventType = SearchResource.ListRequest.EventTypeEnum.Live;
        request.MaxResults = 1;

        var response = await request.ExecuteAsync(ct);
        var videoId = response.Items?.FirstOrDefault()?.Id?.VideoId;
        if (string.IsNullOrWhiteSpace(videoId))
        {
            return null;
        }

        return await ResolveFromBroadcastAsync(videoId, ct);
    }
}
#endif
