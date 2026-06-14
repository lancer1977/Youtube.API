using Google;
using Google.Apis.Requests;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.APi.Youtube.Models;
using PolyhydraGames.Platforms.Abstractions;
using System.Net;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class YouTubeLiveChatPoller(
    IYouTubeStreamContextResolver contextResolver,
    IYouTubeLiveChatService liveChatService,
    YouTubeInboundSource inboundSource,
    IYouTubeLiveChatCheckpointStore checkpointStore,
    YouTubeApiOptions options,
    ILogger<YouTubeLiveChatPoller> logger) : BackgroundService, IYouTubeLiveChatPoller
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromSeconds(2);

    private readonly HashSet<string> _seenMessages = [];
    private readonly Queue<string> _seenMessageOrder = new();
    private readonly TimeSpan _minPollInterval = options.Live.PollInterval > DefaultPollInterval
        ? options.Live.PollInterval
        : DefaultPollInterval;
    private readonly int _maxBackoffSeconds = Math.Clamp(options.Live.MaxBackoffSeconds ?? 30, 2, 120);
    private readonly int _maxSeenWindowSize = Math.Clamp(options.Live.StateWindowSize, 16, 1024);

    private ChannelKey? _loadedChannelKey;
    private string? _pageToken;
    private YouTubeStreamContext? _currentContext;
    private YouTubeLiveChatPollerPhase _phase = YouTubeLiveChatPollerPhase.Started;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Live.Enabled)
        {
            return;
        }

        var retries = 0;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_currentContext?.ChannelKey is null || _currentContext.LiveChatId is null)
                {
                    _currentContext = await TryResolveContextAsync(stoppingToken);
                    if (_currentContext?.ChannelKey is null)
                    {
                        _phase = YouTubeLiveChatPollerPhase.NoBroadcast;
                        await PersistCurrentStateAsync(stoppingToken);
                        var delay = GetRetryDelay(++retries);
                        LogState("No broadcast or channel context available; retrying.", _phase, _currentContext, delay);
                        await Task.Delay(delay, stoppingToken);
                        continue;
                    }

                    await EnsureCheckpointLoadedAsync(_currentContext, stoppingToken);

                    if (_currentContext.LiveChatId is null)
                    {
                        _phase = YouTubeLiveChatPollerPhase.NoBroadcast;
                        await PersistCurrentStateAsync(stoppingToken);
                        var delay = GetRetryDelay(++retries);
                        LogState("Resolved stream context without an active live chat; retrying.", _phase, _currentContext, delay);
                        await Task.Delay(delay, stoppingToken);
                        continue;
                    }

                    LogState("Resolved live chat context.", YouTubeLiveChatPollerPhase.Resolved, _currentContext);
                }

                var context = _currentContext;
                if (context?.ChannelKey is null || context.LiveChatId is null)
                {
                    _currentContext = null;
                    continue;
                }

                try
                {
                    var channelKey = context.ChannelKey ?? throw new InvalidOperationException("Resolved live chat context is missing a channel key.");
                    if (channelKey.Equals(default(ChannelKey)))
                    {
                        _currentContext = null;
                        continue;
                    }

                    _phase = YouTubeLiveChatPollerPhase.Resolved;
                    var batch = await liveChatService.ListMessagesAsync(context.LiveChatId, _pageToken, stoppingToken);
                    foreach (var message in batch.Messages)
                    {
                        if (!TryMarkSeen(message.MessageId))
                        {
                            continue;
                        }

                        var outboundMessage = new YouTubeLiveChatMessage(
                            message.MessageId,
                            context.LiveChatId,
                            context.BroadcastId,
                            context.VideoId,
                            message.Text,
                            message.Timestamp,
                            message.Author,
                            message.RawPayload);

                    inboundSource.Publish(channelKey!, outboundMessage);
                    }

                    _pageToken = batch.NextPageToken;
                    retries = 0;
                    await PersistCurrentStateAsync(stoppingToken);
                    var delay = GetPollingDelay(batch.PollingInterval);
                    LogState("Polled live chat successfully.", _phase, context, delay);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex) when (ex is GoogleApiException gae && IsExpiredLiveChat(gae))
                {
                    _phase = YouTubeLiveChatPollerPhase.Rebinding;
                    LogState(ex, "Live chat id expired or rotated; rebinding stream context.", _phase, context);
                    await PersistCurrentStateAsync(stoppingToken);
                    _currentContext = null;
                    _pageToken = null;
                    retries = Math.Min(retries + 1, 8);
                }
                catch (Exception ex) when (ex is GoogleApiException gae && IsRetryable(gae))
                {
                    _phase = YouTubeLiveChatPollerPhase.Backoff;
                    var delay = GetRetryDelay(++retries);
                    LogState(ex, "Transient YouTube API error while polling live chat; backing off.", _phase, context, delay);
                    await PersistCurrentStateAsync(stoppingToken);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex)
                {
                    _phase = YouTubeLiveChatPollerPhase.Backoff;
                    var delay = GetRetryDelay(++retries);
                    logger.LogError(
                        ex,
                        "Non-recoverable error in live chat poll loop for channel {ChannelId}, broadcast {BroadcastId}, liveChat {LiveChatId}. Retrying in {Delay}.",
                        context.ChannelId,
                        context.BroadcastId,
                        context.LiveChatId,
                        delay);
                    await PersistCurrentStateAsync(stoppingToken);
                    await Task.Delay(delay, stoppingToken);
                }
            }
        }
        finally
        {
            _phase = YouTubeLiveChatPollerPhase.Stopped;
            LogState("Live chat poller stopped.", _phase, _currentContext);
            await PersistCurrentStateAsync(CancellationToken.None);
        }
    }

    private async Task<YouTubeStreamContext?> TryResolveContextAsync(CancellationToken ct)
    {
        try
        {
            return await contextResolver.ResolveAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to resolve live stream context.");
            return null;
        }
    }

    private async Task EnsureCheckpointLoadedAsync(YouTubeStreamContext context, CancellationToken ct)
    {
        if (context.ChannelKey is null)
        {
            return;
        }

        var channelKey = context.ChannelKey.Value;
        if (_loadedChannelKey is not null && SameChannelKey(_loadedChannelKey.Value, channelKey))
        {
            return;
        }

        _loadedChannelKey = channelKey;
        var checkpoint = await checkpointStore.LoadAsync(channelKey, ct);
        if (checkpoint is null)
        {
            _pageToken = null;
            _seenMessages.Clear();
            _seenMessageOrder.Clear();
            return;
        }

        if (!string.Equals(checkpoint.LiveChatId, context.LiveChatId, StringComparison.Ordinal))
        {
            _pageToken = null;
            _seenMessages.Clear();
            _seenMessageOrder.Clear();
            return;
        }

        RestoreSeenWindow(checkpoint.SeenMessageIds);
        _pageToken = checkpoint.PageToken;
        logger.LogInformation(
            "Restored live chat checkpoint for channel {ChannelId}, broadcast {BroadcastId}, liveChat {LiveChatId} with {SeenCount} tracked message ids.",
            context.ChannelId,
            context.BroadcastId,
            context.LiveChatId,
            _seenMessages.Count);
    }

    private async Task PersistCurrentStateAsync(CancellationToken ct)
    {
        var context = _currentContext;
        if (context is null || context.ChannelKey is null)
        {
            return;
        }

        var channelKey = context.ChannelKey.Value;

        try
        {
            await checkpointStore.SaveAsync(
                channelKey,
                BuildState(context, _phase, _pageToken),
                ct);
        }
        catch (Exception ex)
        {
            var channelId = context.ChannelId;
            var broadcastId = context.BroadcastId;
            var liveChatId = context.LiveChatId;
            logger.LogWarning(
                ex,
                "Unable to persist live chat checkpoint for channel {ChannelId}, broadcast {BroadcastId}, liveChat {LiveChatId}.",
                channelId,
                broadcastId,
                liveChatId);
        }
    }

    private YouTubeLiveChatPollerState BuildState(
        YouTubeStreamContext context,
        YouTubeLiveChatPollerPhase phase,
        string? pageToken)
    {
        return new YouTubeLiveChatPollerState(
            context.BroadcastId,
            context.LiveChatId,
            context.ChannelId,
            context.VideoId,
            pageToken,
            _seenMessageOrder.ToArray(),
            phase,
            DateTimeOffset.UtcNow);
    }

    private void RestoreSeenWindow(IEnumerable<string> seenMessageIds)
    {
        _seenMessages.Clear();
        _seenMessageOrder.Clear();

        foreach (var messageId in seenMessageIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (_seenMessages.Add(messageId))
            {
                _seenMessageOrder.Enqueue(messageId);
            }
        }
    }

    private bool TryMarkSeen(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId) || _seenMessages.Contains(messageId))
        {
            return false;
        }

        _seenMessages.Add(messageId);
        _seenMessageOrder.Enqueue(messageId);

        while (_seenMessageOrder.Count > _maxSeenWindowSize)
        {
            var removed = _seenMessageOrder.Dequeue();
            _seenMessages.Remove(removed);
        }

        return true;
    }

    private TimeSpan GetPollingDelay(TimeSpan rawDelay)
    {
        var minimum = _minPollInterval;
        if (rawDelay < minimum)
        {
            return minimum;
        }

        return rawDelay > DefaultPollInterval * 6
            ? DefaultPollInterval * 6
            : rawDelay;
    }

    private TimeSpan GetRetryDelay(int attempt)
    {
        var power = Math.Pow(2, Math.Min(attempt, 8));
        var seconds = Math.Max(RetryBaseDelay.TotalSeconds, power);
        return TimeSpan.FromSeconds(Math.Min(seconds, _maxBackoffSeconds));
    }

    private static bool IsRetryable(GoogleApiException ex)
    {
        if (ex.HttpStatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
        {
            return true;
        }

        var reason = ex.Error?.Errors?.FirstOrDefault()?.Reason;
        return reason is "quotaExceeded" or "rateLimitExceeded" or "backendError" or "internalError";
    }

    private static bool IsExpiredLiveChat(GoogleApiException ex)
    {
        if (ex.HttpStatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            return true;
        }

        var reason = ex.Error?.Errors?.FirstOrDefault()?.Reason;
        return reason is "liveChatEnded" or "liveChatNotFound" or "notFound" or "gone";
    }

    private void LogState(string message, YouTubeLiveChatPollerPhase phase, YouTubeStreamContext? context, TimeSpan? delay = null)
    {
        if (delay is null)
        {
            logger.LogInformation(
                "{Message} State={State} channelId={ChannelId} broadcastId={BroadcastId} liveChatId={LiveChatId}",
                message,
                phase,
                context?.ChannelId,
                context?.BroadcastId,
                context?.LiveChatId);
            return;
        }

        logger.LogInformation(
            "{Message} State={State} channelId={ChannelId} broadcastId={BroadcastId} liveChatId={LiveChatId} delay={Delay}",
            message,
            phase,
            context?.ChannelId,
            context?.BroadcastId,
            context?.LiveChatId,
            delay.Value);
    }

    private void LogState(Exception ex, string message, YouTubeLiveChatPollerPhase phase, YouTubeStreamContext? context, TimeSpan? delay = null)
    {
        if (delay is null)
        {
            logger.LogWarning(
                ex,
                "{Message} State={State} channelId={ChannelId} broadcastId={BroadcastId} liveChatId={LiveChatId}",
                message,
                phase,
                context?.ChannelId,
                context?.BroadcastId,
                context?.LiveChatId);
            return;
        }

        logger.LogWarning(
            ex,
            "{Message} State={State} channelId={ChannelId} broadcastId={BroadcastId} liveChatId={LiveChatId} delay={Delay}",
            message,
            phase,
            context?.ChannelId,
            context?.BroadcastId,
            context?.LiveChatId,
            delay.Value);
    }

    private static bool SameChannelKey(ChannelKey left, ChannelKey right)
    {
        return Equals(left.OwnerUserId, right.OwnerUserId) &&
               left.Platform == right.Platform &&
               string.Equals(left.PlatformAccountId, right.PlatformAccountId, StringComparison.Ordinal);
    }
}
