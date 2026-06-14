using Google.Apis.YouTube.v3.Data;
using PolyhydraGames.APi.Youtube.Interfaces;

namespace PolyhydraGames.APi.Youtube
{
    public class YoutubeQueryUserService(IYoutubeQuery Query, string Username) : IYoutubeUserQuery
    {
        public Task<IReadOnlyList<SearchResult>> GetVideos(CancellationToken ct = default)
            => Query.GetUserVideos(Username, ct: ct);

        public Task<IReadOnlyList<PlaylistSnippet>> GetUserPlaylists(CancellationToken ct = default)
            => Query.GetUserPlaylists(Username, ct);

        public Task<int> GetVideosOfGameCount(string gameName, string system, CancellationToken ct = default)
            => Query.GetVideosOfGameCount(Username, gameName, system, ct);
    }
}
