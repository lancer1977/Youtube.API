using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using PolyhydraGames.APi.Youtube.Models;
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

    public void Publish(ChannelKey channelKey, YouTubeLiveChatMessage message)
    {
        var snippet = message.RawPayload.Snippet;
        var author = message.Author;
        var role = ToChannelRole(author);

        var meta = new Dictionary<string, string>
        {
            ["youtubeMessageId"] = message.MessageId,
            ["youtubeLiveChatId"] = message.LiveChatId ?? string.Empty,
            ["youtubeBroadcastId"] = message.BroadcastId ?? string.Empty,
            ["youtubeVideoId"] = message.VideoId ?? string.Empty,
            ["youtubeMessageType"] = snippet?.Type ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(author?.ChannelId))
        {
            meta["sourceChannelId"] = author.ChannelId;
        }

        Emit(new InboundEnvelope(
            channelKey,
            InboundEnvelope.Sources.YouTubeChat,
            author?.ChannelId,
            author?.DisplayName,
            null,
            role,
            snippet?.DisplayMessage ?? snippet?.TextMessageDetails?.MessageText ?? message.Text,
            message.Timestamp,
            meta));
    }

    public void Publish(ChannelKey channelKey, LiveChatMessage message)
    {
        var author = message.AuthorDetails;
        var model = new YouTubeLiveChatMessage(
            message.Id ?? string.Empty,
            message.Snippet?.LiveChatId,
            null,
            null,
            message.Snippet?.TextMessageDetails?.MessageText ?? message.Snippet?.DisplayMessage ?? string.Empty,
            message.Snippet?.PublishedAtDateTimeOffset ?? DateTimeOffset.UtcNow,
            author is null
                ? null
                : new YouTubeLiveChatAuthor(
                    author.ChannelId,
                    author.DisplayName,
                    author.IsChatOwner == true,
                    author.IsChatModerator == true,
                    author.IsChatSponsor == true,
                    author.IsVerified == true),
            message);

        Publish(channelKey, model);
    }

    private static ChannelRole ToChannelRole(YouTubeLiveChatAuthor? author)
    {
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

        return viewerRole;
    }
}
