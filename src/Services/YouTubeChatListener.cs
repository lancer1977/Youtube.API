#if NET10_0_OR_GREATER
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.APi.Youtube.Models;
using PolyhydraGames.PostOffice.Abstractions;
using PolyhydraGames.PostOffice.Core;
using PolyhydraGames.Platforms.Abstractions;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class YouTubeChatListener : IYouTubeChatListener
{
    private readonly IYouTubeLiveChatGateway _gateway;
    private readonly YouTubeInboundSource _inboundSource;
    private readonly YouTubeApiOptions _options;
    private readonly ILogger<YouTubeChatListener> _logger;
    private readonly HashSet<string> _recentMessageIds = new(StringComparer.Ordinal);
    private CancellationTokenSource? _lifecycleCts;
    private Task? _runTask;
    private System.Threading.Timer? _duplicateCleanupTimer;

    public YouTubeChatListener(
        IYouTubeLiveChatGateway gateway,
        YouTubeInboundSource inboundSource,
        YouTubeApiOptions options,
        ILogger<YouTubeChatListener> logger)
    {
        _gateway = gateway;
        _inboundSource = inboundSource;
        _options = options;
        _logger = logger;
    }

    public bool IsRunning { get; private set; }

    public Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        if (!_options.Enabled || !_options.Live.Enabled)
        {
            _logger.LogInformation("YouTube live chat is disabled by configuration.");
            return Task.CompletedTask;
        }

        if (!TryGetOwnerUserId(out _))
        {
            _logger.LogWarning("YouTube chat listener cannot start because Live.OwnerUserId is missing or invalid.");
            return Task.CompletedTask;
        }

        _lifecycleCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _duplicateCleanupTimer = new System.Threading.Timer(
            _ => _recentMessageIds.Clear(),
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));

        _runTask = RunAsync(_lifecycleCts.Token);
        IsRunning = true;
        _logger.LogInformation("YouTube chat listener started.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (!IsRunning)
        {
            return;
        }

        _lifecycleCts?.Cancel();

        if (_runTask is not null)
        {
            try
            {
                await _runTask.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "YouTube chat listener stopped with an error.");
            }
        }

        _duplicateCleanupTimer?.Dispose();
        _duplicateCleanupTimer = null;

        _lifecycleCts?.Dispose();
        _lifecycleCts = null;
        _runTask = null;
        _recentMessageIds.Clear();
        IsRunning = false;
        _logger.LogInformation("YouTube chat listener stopped.");
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var liveChatId = string.Empty;
        var pageToken = (string?)null;
        ChannelKey channelKey = default;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(liveChatId))
                {
                    liveChatId = await _gateway.ResolveLiveChatIdAsync(ct) ?? string.Empty;
                    pageToken = null;

                    if (string.IsNullOrWhiteSpace(liveChatId))
                    {
                        await DelayAsync(_options.Live.PollInterval, ct);
                        continue;
                    }

                    if (!TryBuildChannelKey(liveChatId, out channelKey))
                    {
                        liveChatId = string.Empty;
                        await DelayAsync(_options.Live.PollInterval, ct);
                        continue;
                    }

                    _logger.LogInformation("YouTube live chat resolved: {LiveChatId}", liveChatId);
                }

                var page = await _gateway.ListMessagesAsync(liveChatId, pageToken, ct);
                PublishMessages(page.Messages, channelKey);

                if (!string.IsNullOrWhiteSpace(page.NextPageToken))
                {
                    pageToken = page.NextPageToken;
                    continue;
                }

                pageToken = null;
                await DelayAsync(page.PollingInterval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "YouTube chat polling failed; will retry.");
                liveChatId = string.Empty;
                pageToken = null;
                await DelayAsync(_options.Live.PollInterval, ct);
            }
        }
    }

    private void PublishMessages(IReadOnlyList<YouTubeLiveChatMessage> messages, ChannelKey channelKey)
    {
        foreach (var message in messages)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(message.MessageId) && !_recentMessageIds.Add(message.MessageId))
                {
                    _logger.LogTrace("Duplicate YouTube chat message suppressed: {MessageId}", message.MessageId);
                    continue;
                }

                _inboundSource.Publish(channelKey, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process incoming YouTube chat message.");
            }
        }
    }

    private bool TryBuildChannelKey(string liveChatId, out ChannelKey channelKey)
    {
        channelKey = default;
        if (!TryGetOwnerUserId(out var ownerUserId))
        {
            return false;
        }

        var platformAccountId =
            !string.IsNullOrWhiteSpace(_options.Live.ChannelId) ? _options.Live.ChannelId :
            !string.IsNullOrWhiteSpace(_options.Live.BroadcastId) ? _options.Live.BroadcastId :
            liveChatId;

        if (string.IsNullOrWhiteSpace(platformAccountId))
        {
            _logger.LogWarning("YouTube chat message ignored because no platform account identifier could be resolved.");
            return false;
        }

        channelKey = new ChannelKey(ownerUserId, Platform.Youtube, platformAccountId);
        return true;
    }

    private bool TryGetOwnerUserId(out UserId ownerUserId)
    {
        ownerUserId = default;
        if (string.IsNullOrWhiteSpace(_options.Live.OwnerUserId))
        {
            return false;
        }

        if (!Guid.TryParse(_options.Live.OwnerUserId, out var ownerUserGuid) || ownerUserGuid == Guid.Empty)
        {
            return false;
        }

        ownerUserId = ownerUserGuid;
        return true;
    }

    private static async Task DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        await Task.Delay(delay, ct);
    }
}
#endif
