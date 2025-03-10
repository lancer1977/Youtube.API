using Google.Apis.YouTube.v3.Data;
using PolyhydraGames.APi.Youtube.Interfaces;

namespace PolyhydraGames.APi.Youtube
{
    public class YoutubeQueryUserService(IYoutubeQuery Query, string Username) : IYoutubeUserQuery
    {
        public Task<IList<SearchResult>> GetVideos() => Query.GetUserVideos(Username);

        public Task<IList<PlaylistSnippet>> GetUserPlaylists()=> Query.GetUserPlaylists(Username);

        public Task<int> GetVideosOfGameCount(string gameName, string system)
        {
            throw new NotImplementedException();
        }
    }
}