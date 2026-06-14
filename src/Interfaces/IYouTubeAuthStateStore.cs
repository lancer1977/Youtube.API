namespace PolyhydraGames.APi.Youtube.Interfaces;

public sealed record YouTubeAuthState(
    string RefreshToken,
    DateTimeOffset? UpdatedAt = null);

public interface IYouTubeAuthStateStore
{
    bool TryGet(string streamerKey, out YouTubeAuthState? state);

    void Set(string streamerKey, YouTubeAuthState state);

    bool Remove(string streamerKey);
}
