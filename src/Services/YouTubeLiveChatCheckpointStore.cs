using System.Collections.Concurrent;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.APi.Youtube.Models;
using PolyhydraGames.Platforms.Abstractions;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class YouTubeLiveChatCheckpointStore(YouTubeApiOptions options) : IYouTubeLiveChatCheckpointStore
{
    private readonly ConcurrentDictionary<string, YouTubeLiveChatPollerState> _states = new();
    private readonly TimeSpan _ttl = options.Live.CheckpointTtl;

    public Task<YouTubeLiveChatPollerState?> LoadAsync(ChannelKey channelKey, CancellationToken cancellationToken = default)
    {
        if (_states.TryGetValue(GetKey(channelKey), out var state) && !IsExpired(state))
        {
            return Task.FromResult<YouTubeLiveChatPollerState?>(Clone(state));
        }

        return Task.FromResult<YouTubeLiveChatPollerState?>(null);
    }

    public Task SaveAsync(
        ChannelKey channelKey,
        YouTubeLiveChatPollerState state,
        CancellationToken cancellationToken = default)
    {
        _states[GetKey(channelKey)] = Clone(state);
        PruneExpired();
        return Task.CompletedTask;
    }

    private void PruneExpired()
    {
        if (_ttl <= TimeSpan.Zero)
        {
            return;
        }

        foreach (var entry in _states)
        {
            if (IsExpired(entry.Value))
            {
                _states.TryRemove(entry.Key, out _);
            }
        }
    }

    private bool IsExpired(YouTubeLiveChatPollerState state)
    {
        if (_ttl <= TimeSpan.Zero)
        {
            return false;
        }

        return DateTimeOffset.UtcNow - state.UpdatedUtc > _ttl;
    }

    private static YouTubeLiveChatPollerState Clone(YouTubeLiveChatPollerState state)
        => state with { SeenMessageIds = state.SeenMessageIds.ToArray() };

    private static string GetKey(ChannelKey channelKey)
        => $"{channelKey.OwnerUserId}:{channelKey.Platform}:{channelKey.PlatformAccountId}";
}
