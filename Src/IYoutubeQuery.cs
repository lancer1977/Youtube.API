namespace APi.Youtube
{
    public interface IYoutubeQuery
    {
        public Task<List<string>> GetUserVideos(string username);
        public Task<List<string>> GetUserPlaylists(string username); 
    }

    public interface IYoutubeUserQuery
    {
        public Task<List<string>> GetVideos();
        public Task<List<string>> GetPlaylists();
        public Task<int> GetVideosOfGameCount(string gameName, string system);
    }
}