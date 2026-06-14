using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Interfaces;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class YouTubeAuthStateStore : IYouTubeAuthStateStore
{
    private readonly ConcurrentDictionary<string, YouTubeAuthState> _states = new(StringComparer.Ordinal);

    public YouTubeAuthStateStore(IOptions<YouTubeApiOptions> options)
    {
        var resolved = options.Value;
        if (string.IsNullOrWhiteSpace(resolved.RefreshToken))
        {
            return;
        }

        var defaultState = new YouTubeAuthState(resolved.RefreshToken);
        _states[DefaultKey] = defaultState;

        if (!string.IsNullOrWhiteSpace(resolved.Live.OwnerUserId))
        {
            _states[resolved.Live.OwnerUserId] = defaultState;
        }
    }

    public static string DefaultKey => "__default__";

    public bool TryGet(string streamerKey, out YouTubeAuthState? state)
    {
        if (string.IsNullOrWhiteSpace(streamerKey))
        {
            state = null;
            return false;
        }

        return _states.TryGetValue(streamerKey, out state);
    }

    public void Set(string streamerKey, YouTubeAuthState state)
    {
        if (string.IsNullOrWhiteSpace(streamerKey))
        {
            throw new ArgumentException("Streamer key is required.", nameof(streamerKey));
        }

        if (string.IsNullOrWhiteSpace(state.RefreshToken))
        {
            throw new ArgumentException("Refresh token is required.", nameof(state));
        }

        _states[streamerKey] = state;
    }

    public bool Remove(string streamerKey)
    {
        if (string.IsNullOrWhiteSpace(streamerKey))
        {
            return false;
        }

        return _states.TryRemove(streamerKey, out _);
    }
}
