using Google.Apis.YouTube.v3.Data;

namespace APi.Youtube
{
    public interface IYoutubeQuery
    {
        public Task<List<string>> GetUserVideos(string username, string query ="");
        public Task<List<PlaylistSnippet>> GetUserPlaylists(string username); 
        Task<Video?> GetVideoDetails(string videoId);
        Task<string?> GetUserID(string username);
    }

    public interface IYoutubeUserQuery
    {
        public Task<List<string>> GetVideos();
        public Task<List<string>> GetPlaylists();
        public Task<int> GetVideosOfGameCount(string gameName, string system);
    }
}