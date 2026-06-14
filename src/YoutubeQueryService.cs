using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using PolyhydraGames.APi.Youtube.Interfaces;

namespace PolyhydraGames.APi.Youtube;

public class YoutubeQueryService : IYoutubeQuery
{
    private readonly YouTubeService _youtubeService;
    private readonly ILogger<YoutubeQueryService> _logger;

    public YoutubeQueryService(IYouTubeApiClientFactory clientFactory, ILogger<YoutubeQueryService> logger)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        _youtubeService = clientFactory.CreatePublicReadService();
        _logger = logger;
    }

    internal static YouTubeService CreateService(IYoutubeConfig config)
    {
        if (config.HasOAuthCredentials)
        {
            return CreateOAuthService(config);
        }

        if (!string.IsNullOrWhiteSpace(config.ApiKey))
        {
            return CreateApiKeyService(config);
        }

        throw new InvalidOperationException(
            "YouTube configuration requires an API key or OAuth client credentials.");
    }

    private static YouTubeService CreateApiKeyService(IYoutubeConfig config)
        => new(new BaseClientService.Initializer
        {
            ApiKey = config.ApiKey,
            ApplicationName = config.ApplicationName
        });

    private static YouTubeService CreateOAuthService(IYoutubeConfig config)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = config.ClientId,
                ClientSecret = config.ClientSecret
            },
            Scopes = new[]
            {
                YouTubeService.Scope.YoutubeReadonly,
                YouTubeService.Scope.YoutubeForceSsl
            }
        });

        var credential = new UserCredential(
            flow,
            config.ApplicationName,
            new TokenResponse { RefreshToken = config.RefreshToken });

        return new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = config.ApplicationName
        });
    }

    public async Task<string?> GetUserID(string channelName, CancellationToken ct = default)
    {
        var searchRequest = _youtubeService.Search.List("snippet");
        searchRequest.Q = channelName;
        searchRequest.Type = "channel";
        searchRequest.MaxResults = 5;
        var result = await searchRequest.ExecuteAsync(ct);

        return result.Items?.Select(x => x.Snippet?.ChannelId)
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
    }

    public async Task<Channel?> GetChannel(string channelName, CancellationToken ct = default)
    {
        var searchRequest = _youtubeService.Channels.List("snippet,contentDetails,statistics");
        searchRequest.ForHandle = channelName;
        searchRequest.MaxResults = 5;
        var result = await searchRequest.ExecuteAsync(ct);
        return result.Items?.FirstOrDefault();
    }

    public Task<IReadOnlyList<SearchResult>> GetUserVideos(string username, string query = "", CancellationToken ct = default)
    {
        return GetUserUploads(username, query, null, ct);
    }

    public async Task<IReadOnlyList<SearchResult>> GetUserUploads(
        string username,
        string query,
        string? paginationToken,
        CancellationToken ct)
    {
        var user = await GetChannel(username, ct);
        if (user?.Id is null)
        {
            _logger.LogWarning("Unable to resolve user channel for username {Username}.", username);
            return Array.Empty<SearchResult>();
        }

        var request = _youtubeService.Search.List("snippet");
        request.ChannelId = user.Id;
        request.Type = "video";
        request.Order = SearchResource.ListRequest.OrderEnum.Date;
        request.Q = query;
        request.MaxResults = 50;
        request.PageToken = paginationToken;

        var response = await request.ExecuteAsync(ct);
        return response.Items?.ToList() ?? new List<SearchResult>();
    }

    public async Task<IReadOnlyList<PlaylistSnippet>> GetUserPlaylists(string username, CancellationToken ct = default)
    {
        var user = await GetChannel(username, ct);
        if (user?.Id is null)
        {
            _logger.LogWarning("Unable to resolve user channel for playlist lookup {Username}.", username);
            return Array.Empty<PlaylistSnippet>();
        }

        var request = _youtubeService.Playlists.List("snippet");
        request.ChannelId = user.Id;
        var searchResponse = await request.ExecuteAsync(ct);
        return searchResponse.Items?.Select(x => x.Snippet).ToList() ?? new List<PlaylistSnippet>();
    }

    public async Task<Video?> GetVideoDetails(string videoId, CancellationToken ct = default)
    {
        var videoRequest = _youtubeService.Videos.List("snippet,statistics,contentDetails");
        videoRequest.Id = videoId;
        var videoResponse = await videoRequest.ExecuteAsync(ct);

        return videoResponse.Items?.FirstOrDefault();
    }

    public async Task<int> GetVideosOfGameCount(string username, string gameName, string system, CancellationToken ct = default)
    {
        var user = await GetChannel(username, ct);
        if (user?.ContentDetails?.RelatedPlaylists is null)
        {
            _logger.LogWarning("Unable to resolve uploads playlist for user {Username}.", username);
            return 0;
        }

        var uploadsPlaylistId = user.ContentDetails.RelatedPlaylists.Uploads;
        if (string.IsNullOrWhiteSpace(uploadsPlaylistId))
        {
            _logger.LogWarning("Uploads playlist missing for user {Username}.", username);
            return 0;
        }

        var playlistRequest = _youtubeService.PlaylistItems.List("snippet");
        playlistRequest.PlaylistId = uploadsPlaylistId;
        playlistRequest.MaxResults = 50;
        var playlistResponse = await playlistRequest.ExecuteAsync(ct);

        return playlistResponse.Items?.Count ?? 0;
    }
}
