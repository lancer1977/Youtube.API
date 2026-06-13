#if NET10_0_OR_GREATER
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolyhydraGames.APi.Youtube.Interfaces;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class YouTubeChatListenerHostedService : IHostedService
{
    private readonly IYouTubeChatListener _listener;
    private readonly ILogger<YouTubeChatListenerHostedService> _logger;

    public YouTubeChatListenerHostedService(
        IYouTubeChatListener listener,
        ILogger<YouTubeChatListenerHostedService> logger)
    {
        _listener = listener;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("YouTube chat listener hosted service starting");
        await _listener.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("YouTube chat listener hosted service stopping");
        await _listener.StopAsync(cancellationToken);
    }
}
#endif
