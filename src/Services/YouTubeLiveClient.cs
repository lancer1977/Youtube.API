using System.Reactive.Linq;
using Microsoft.Extensions.Hosting;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.APi.Youtube.Models;
using PolyhydraGames.PostOffice.Abstractions;
using PolyhydraGames.Platforms.Abstractions;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class YouTubeLiveClient(
    IYouTubeLiveChatGateway liveChatGateway,
    IYouTubeLiveChatPoller poller,
    YouTubeInboundSource inboundSource) : IYouTubeLiveClient
{
    public IObservable<YouTubeChatMessageReceived> OnMessageReceived =>
        inboundSource.Messages.Select(MapMessage);

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return poller.StartAsync(cancellationToken);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        return poller.StopAsync(cancellationToken);
    }

    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(message));
        }

        var liveChatId = await liveChatGateway.ResolveLiveChatIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(liveChatId))
        {
            throw new InvalidOperationException("Unable to resolve a live chat id for the current YouTube live context.");
        }

        await liveChatGateway.SendMessageAsync(liveChatId, message, cancellationToken);
    }

    private static YouTubeChatMessageReceived MapMessage(InboundEnvelope envelope)
    {
        var meta = envelope.Meta ?? new Dictionary<string, string>();
        meta.TryGetValue("youtubeMessageId", out var messageId);
        meta.TryGetValue("youtubeLiveChatId", out var liveChatId);
        meta.TryGetValue("youtubeBroadcastId", out var broadcastId);
        meta.TryGetValue("youtubeVideoId", out var videoId);

        return new YouTubeChatMessageReceived(
            messageId ?? string.Empty,
            string.IsNullOrWhiteSpace(liveChatId) ? null : liveChatId,
            string.IsNullOrWhiteSpace(broadcastId) ? null : broadcastId,
            string.IsNullOrWhiteSpace(videoId) ? null : videoId,
            envelope.Channel,
            envelope.PlatformRawViewerId,
            envelope.ViewerName,
            envelope.UserRole,
            envelope.Text,
            envelope.Timestamp,
            meta);
    }
}
