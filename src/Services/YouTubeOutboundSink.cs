using Microsoft.Extensions.Logging;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.APi.Youtube.Models;
using PolyhydraGames.PostOffice.Abstractions;
using PolyhydraGames.PostOffice.Core;
using PolyhydraGames.Platforms.Abstractions;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class YouTubeOutboundSink(
    IYouTubeLiveChatService chatService,
    IYouTubeStreamContextResolver contextResolver,
    YouTubeApiOptions options,
    ILogger<YouTubeOutboundSink> logger) : OutboundSink<YouTubeOutboundSink>(logger)
{
    private const string LiveChatIdMetaKey = "liveChatId";

    protected override string Name => nameof(YouTubeOutboundSink);

    public override bool CanHandle(ChannelKey channel) => channel.Platform == Platform.Youtube;

    public override async Task SendAsync(OutboundEnvelope message, CancellationToken ct = default)
    {
        if (!CanHandle(message.Channel))
        {
            Log.LogWarning("YouTube outbound sink ignoring message for unsupported platform {Platform}", message.Channel.Platform);
            return;
        }

        if (!options.Enabled)
        {
            Log.LogWarning("YouTube API is disabled; outbound message ignored.");
            return;
        }

        if (!options.Live.Enabled)
        {
            Log.LogWarning("YouTube live streaming is disabled; outbound message ignored.");
            return;
        }

        if (!options.Live.EnableOutboundChat)
        {
            Log.LogWarning("YouTube outbound chat is disabled; outbound message ignored.");
            return;
        }

        if (!options.HasOAuthCredentials)
        {
            Log.LogError("YouTube outbound send failed for {ChannelRawId}; OAuth credentials are missing.", message.Channel.PlatformAccountId);
            return;
        }

        var liveChatId = GetLiveChatIdFromEnvelope(message);
        if (string.IsNullOrWhiteSpace(liveChatId))
        {
            var context = await ResolveContextAsync(ct);
            liveChatId = context?.LiveChatId;
        }

        if (string.IsNullOrWhiteSpace(liveChatId))
        {
            Log.LogError(
                "YouTube outbound send failed for {ChannelRawId}; no live chat id could be resolved.",
                message.Channel.PlatformAccountId);
            return;
        }

        try
        {
            var resultId = await chatService.SendMessageAsync(liveChatId, message.Text, ct);
            if (string.IsNullOrWhiteSpace(resultId))
            {
                Log.LogWarning("YouTube outbound message was acknowledged but no message id was returned.");
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            Log.LogError(ex, "YouTube outbound request failed due to bad operation state for {ChannelRawId}.", message.Channel.PlatformAccountId);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "YouTube outbound send failed for {ChannelRawId}.", message.Channel.PlatformAccountId);
        }
    }

    private static string? GetLiveChatIdFromEnvelope(OutboundEnvelope envelope)
    {
        return envelope.Meta != null &&
               envelope.Meta.TryGetValue(LiveChatIdMetaKey, out var liveChatId) &&
               !string.IsNullOrWhiteSpace(liveChatId)
            ? liveChatId
            : null;
    }

    private async Task<YouTubeStreamContext?> ResolveContextAsync(CancellationToken ct = default)
    {
        try
        {
            return await contextResolver.ResolveAsync(ct);
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Could not resolve YouTube stream context for outbound send.");
            return null;
        }
    }
}
