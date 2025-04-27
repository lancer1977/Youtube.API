using Google.Apis.YouTube.v3.Data;

namespace PolyhydraGames.APi.Youtube.Interfaces;

public interface IYoutubeQuery
{
    public Task<IList<SearchResult>> GetUserVideos(string username, string query = "");
    public Task<IList<PlaylistSnippet>> GetUserPlaylists(string username);
    Task<Video?> GetVideoDetails(string videoId);
    Task<string?> GetUserID(string username);
}