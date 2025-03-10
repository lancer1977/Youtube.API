using Google.Apis.YouTube.v3.Data;

namespace PolyhydraGames.APi.Youtube.Interfaces
{
    public interface IYoutubeUserQuery
    {
        public Task<IList<SearchResult>> GetVideos();
        public Task<IList<PlaylistSnippet>> GetUserPlaylists();
    }
}