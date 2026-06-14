using Google.Apis.YouTube.v3.Data;

namespace PolyhydraGames.APi.Youtube.Interfaces
{
    public interface IYoutubeUserQuery
    {
        public Task<IReadOnlyList<SearchResult>> GetVideos(CancellationToken ct = default);
        public Task<IReadOnlyList<PlaylistSnippet>> GetUserPlaylists(CancellationToken ct = default);
        public Task<int> GetVideosOfGameCount(string gameName, string system, CancellationToken ct = default);
    }
}
