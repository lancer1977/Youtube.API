#if NET10_0_OR_GREATER
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using PolyhydraGames.PostOffice.Abstractions;
using PolyhydraGames.PostOffice.Core;
using PolyhydraGames.Platforms.Abstractions;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class YouTubeInboundSource : InboundSource<YouTubeInboundSource>
{
    public YouTubeInboundSource(ILogger<YouTubeInboundSource> logger) : base(logger)
    {
    }

    protected override string Name => nameof(YouTubeInboundSource);

    protected override Task OnStart(CancellationToken ct) => Task.CompletedTask;

    public void Publish(ChannelKey channelKey, LiveChatMessage message)
    {
        var snippet = message.Snippet;
        var author = message.AuthorDetails;

        var meta = new Dictionary<string, string>
        {
            ["youtubeMessageId"] = message.Id ?? string.Empty,
            ["youtubeLiveChatId"] = snippet?.LiveChatId ?? string.Empty,
            ["youtubeMessageType"] = snippet?.Type ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(author?.ChannelId))
        {
            meta["sourceChannelId"] = author.ChannelId;
        }

        var viewerRole = ChannelRole.Everyone;
        if (author?.IsChatOwner == true)
        {
            viewerRole |= ChannelRole.Streamer;
        }

        if (author?.IsChatModerator == true)
        {
            viewerRole |= ChannelRole.Admin;
        }

        if (author?.IsChatSponsor == true)
        {
            viewerRole |= ChannelRole.Subscriber;
        }

        if (author?.IsVerified == true)
        {
            viewerRole |= ChannelRole.Vip;
        }

        Emit(new InboundEnvelope(
            channelKey,
            InboundEnvelope.Sources.YouTubeChat,
            author?.ChannelId,
            author?.DisplayName,
            null,
            viewerRole,
            snippet?.DisplayMessage ?? snippet?.TextMessageDetails?.MessageText ?? string.Empty,
            snippet?.PublishedAtDateTimeOffset ?? DateTimeOffset.UtcNow,
            meta));
    }
}
#endif
