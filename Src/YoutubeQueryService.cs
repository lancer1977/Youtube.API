using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace APi.Youtube;

public class YoutubeQueryService : IYoutubeQuery
{
    private YouTubeService _youtubeService;
    private readonly IYoutubeConfig _config;

    public YoutubeQueryService(IYoutubeConfig config)
    {
        _config = config;
        _youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = _config.ApiKey,
            ApplicationName = _config.ApplicationName
        });
    }
    public async Task<Channel?> GetUser(string username)
    {
        var request = _youtubeService.Channels.List(username);
        var searchResponse = await request.ExecuteAsync();
        return searchResponse.Items.FirstOrDefault();
    }
    public async Task<List<string>> GetUserVideos(string username)
    {
        var user = await GetUser(username);
        var uploadsPlaylistId = user.ContentDetails.RelatedPlaylists.Uploads;
         

        Console.WriteLine($"Uploads Playlist ID: {uploadsPlaylistId}");

        // Step 2: Get videos from the Uploads Playlist
        var playlistRequest = _youtubeService.PlaylistItems.List("snippet");
        playlistRequest.PlaylistId = uploadsPlaylistId;
        //playlistRequest.MaxResults = 10; // Limit results

        var playlistResponse = await playlistRequest.ExecuteAsync();

        foreach (var item in playlistResponse.Items)
        {
            Console.WriteLine($"Title: {item.Snippet.Title}, Video ID: {item.Snippet.ResourceId.VideoId}");
        }
        return playlistResponse.Items.Select(x => x.Snippet.Title).ToList();
    }

    public async Task<List<string>> GetUserPlaylists(string username)
    {
        var user = await GetUser(username);
        var request = _youtubeService.Playlists.List("snippet");
        request.ChannelId = user.Id;
        var searchResponse = await request.ExecuteAsync();
        return searchResponse.Items.Select(x => x.Snippet.Title).ToList();
    }

    public async Task<int> GetVideosOfGameCount(string username,string gameName, string system)
    {
        var user = await GetUser(username);
        var uploadsPlaylistId = user.ContentDetails.RelatedPlaylists.Uploads;


        Console.WriteLine($"Uploads Playlist ID: {uploadsPlaylistId}");

        // Step 2: Get videos from the Uploads Playlist
        var playlistRequest = _youtubeService.PlaylistItems.List("snippet");
        playlistRequest.PlaylistId = uploadsPlaylistId;
        //playlistRequest.MaxResults = 10; // Limit results

        var playlistResponse = await playlistRequest.ExecuteAsync();

        foreach (var item in playlistResponse.Items)
        {
            Console.WriteLine($"Title: {item.Snippet.Title}, Video ID: {item.Snippet.ResourceId.VideoId}");
        }
        return playlistResponse.Items.Select(x => x.Snippet.Title).Count();
    }


    async Task SearchVideosAsync(YouTubeService youtubeService, string query)
    {
        var searchRequest = youtubeService.Search.List("snippet");
        searchRequest.Q = query;
        searchRequest.MaxResults = 5;

        var searchResponse = await searchRequest.ExecuteAsync();

        foreach (var result in searchResponse.Items)
        {
            Console.WriteLine($"Title: {result.Snippet.Title}, Channel: {result.Snippet.ChannelTitle}");
        }
    }
}