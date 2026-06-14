using Google.Apis.YouTube.v3.Data;

namespace PolyhydraGames.APi.Youtube.Interfaces;

public interface IYoutubeQuery
{
    public Task<IReadOnlyList<SearchResult>> GetUserVideos(string username, string query = "", CancellationToken ct = default);
    public Task<IReadOnlyList<PlaylistSnippet>> GetUserPlaylists(string username, CancellationToken ct = default);
    Task<Video?> GetVideoDetails(string videoId, CancellationToken ct = default);
    Task<string?> GetUserID(string username, CancellationToken ct = default);
    Task<int> GetVideosOfGameCount(string username, string gameName, string system, CancellationToken ct = default);
}
