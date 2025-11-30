using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using PolyhydraGames.APi.Youtube.Interfaces;

namespace PolyhydraGames.APi.Youtube;

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

    public async Task<string?> GetUserID(string channelName)
    {
        var searchRequest = _youtubeService.Search.List("snippet");
        searchRequest.Q = channelName;
        searchRequest.Type = "channel";
        searchRequest.MaxResults = 5; // Adjust as needed
        var result = await searchRequest.ExecuteAsync();
        return result.Items.Select(x => x.Snippet.ChannelId).FirstOrDefault();
    }


    public async Task<Channel?> GetChannel(string channelName)
    {
        var searchRequest = _youtubeService.Channels.List("snippet,contentDetails, statistics");
        searchRequest.ForHandle = channelName;
        searchRequest.MaxResults = 5; // Adjust as needed
        var result = await searchRequest.ExecuteAsync();
        return result.Items.Select(x => x).FirstOrDefault();
    }

    public Task<IList<SearchResult>> GetUserVideos(string username, string query)
    {
        return GetUserUploads(username, query, null);
    }

    public async Task<IList<SearchResult>> GetUserUploads(string username, string query, string? paginationToken)
    {
        var user = await GetChannel(username);
        var searchRequest = _youtubeService.Search.List("snippet");
        searchRequest.ChannelId = user.Id; // Filter by Channel ID
        searchRequest.Type = "video"; // Only return videos
        searchRequest.Order = SearchResource.ListRequest.OrderEnum.Date; // Newest videos first 
        searchRequest.Q = query;
        searchRequest.MaxResults = 50; // Limit results
        searchRequest.PageToken = paginationToken;
        //searchRequest.
        //searchRequest.PublishedAfter = DateTime.Now.AddYears(-1); // Only return videos published in the last year

        var response = await searchRequest.ExecuteAsync();
        Console.WriteLine("Count ::" + response.Items.Count);
        foreach (var item in response.Items)
        {
            Console.WriteLine($"Title: {item.Snippet.Title}, Video ID: {item.Id.VideoId}");
        }
        return response.Items;
    }

    public async Task<IList<PlaylistSnippet>> GetUserPlaylists(string username)
    {
        var user = await GetChannel(username);
        var request = _youtubeService.Playlists.List("snippet");
        request.ChannelId = user.Id;
        var searchResponse = await request.ExecuteAsync();
        return searchResponse.Items.Select(x => x.Snippet).ToList();
    }

    public async Task<Video?> GetVideoDetails(string videoId)
    {

        var videoRequest = _youtubeService.Videos.List("snippet,statistics,contentDetails");
        videoRequest.Id = videoId;

        var videoResponse = await videoRequest.ExecuteAsync();



        return videoResponse.Items.First();
    }

    public async Task<int> GetVideosOfGameCount(string username, string gameName, string system)
    {
        var user = await GetChannel(username);
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


    async private Task SearchVideosAsync(YouTubeService youtubeService, string query)
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