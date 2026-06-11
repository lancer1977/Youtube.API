#if NET10_0_OR_GREATER
using Microsoft.Extensions.Logging;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.PostOffice.Abstractions;
using PolyhydraGames.PostOffice.Core;
using PolyhydraGames.Platforms.Abstractions;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class YouTubeOutboundSink : OutboundSink<YouTubeOutboundSink>
{
    private const string LiveChatIdMetaKey = "liveChatId";
    private readonly IYouTubeLiveChatGateway _gateway;

    public YouTubeOutboundSink(IYouTubeLiveChatGateway gateway, ILogger<YouTubeOutboundSink> logger) : base(logger)
    {
        _gateway = gateway;
    }

    protected override string Name => nameof(YouTubeOutboundSink);

    public override bool CanHandle(ChannelKey channel) => channel.Platform == Platform.Youtube;

    public override async Task SendAsync(OutboundEnvelope message, CancellationToken ct = default)
    {
        try
        {
            if (!CanHandle(message.Channel))
            {
                Log.LogWarning("YouTube outbound sink received unsupported platform {Platform}", message.Channel.Platform);
                return;
            }

            var liveChatId = ResolveLiveChatId(message);
            if (string.IsNullOrWhiteSpace(liveChatId))
            {
                liveChatId = await _gateway.ResolveLiveChatIdAsync(ct);
            }

            if (string.IsNullOrWhiteSpace(liveChatId))
            {
                Log.LogWarning("YouTube outbound message ignored because no live chat id could be resolved.");
                return;
            }

            await _gateway.SendMessageAsync(liveChatId, message.Text, ct);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error sending outbound YouTube message");
        }
    }

    private static string? ResolveLiveChatId(OutboundEnvelope message)
        => message.Meta != null && message.Meta.TryGetValue(LiveChatIdMetaKey, out var liveChatId) ? liveChatId : null;
}
#endif
