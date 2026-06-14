using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Interfaces;

namespace PolyhydraGames.APi.Youtube.Services;

public sealed class YouTubeApiClientFactory : IYouTubeApiClientFactory
{
    private const string DefaultAuthStateKey = "__default__";

    private readonly IYoutubeConfig _config;
    private readonly IOptions<YouTubeApiOptions> _options;
    private readonly IYouTubeAuthStateStore _authStateStore;
    private readonly ILogger<YouTubeApiClientFactory> _logger;

    public YouTubeApiClientFactory(
        IYoutubeConfig config,
        IOptions<YouTubeApiOptions> options,
        IYouTubeAuthStateStore authStateStore,
        ILogger<YouTubeApiClientFactory> logger)
    {
        _config = config;
        _options = options;
        _authStateStore = authStateStore;
        _logger = logger;
    }

    public YouTubeService CreatePublicReadService()
    {
        if (!string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            return CreateApiKeyService(_config);
        }

        if (_authStateStore.TryGet(DefaultAuthStateKey, out var state) && state is not null)
        {
            return CreateOAuthService(state.RefreshToken);
        }

        throw new InvalidOperationException(
            "YouTube configuration requires an API key for public reads or a default OAuth auth-state entry.");
    }

    public YouTubeService CreateAuthorizedService(string streamerKey)
    {
        var options = _options.Value;
        if (string.IsNullOrWhiteSpace(streamerKey))
        {
            throw new ArgumentException("Streamer key is required.", nameof(streamerKey));
        }

        if (!options.HasOAuthCredentials)
        {
            throw new InvalidOperationException("OAuth client credentials are required for YouTube live-chat operations.");
        }

        if (!_authStateStore.TryGet(streamerKey, out var state) || state is null)
        {
            throw new InvalidOperationException(
                $"No YouTube auth-state entry was found for streamer key '{streamerKey}'.");
        }

        return CreateOAuthService(state.RefreshToken);
    }

    private YouTubeService CreateApiKeyService(IYoutubeConfig config)
    {
        _logger.LogDebug("Creating YouTube service with API key.");
        return new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = config.ApiKey,
            ApplicationName = config.ApplicationName
        });
    }

    private YouTubeService CreateOAuthService(string refreshToken)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _config.ClientId,
                ClientSecret = _config.ClientSecret
            },
            Scopes = new[]
            {
                YouTubeService.Scope.YoutubeReadonly,
                YouTubeService.Scope.YoutubeForceSsl
            }
        });

        var credential = new UserCredential(
            flow,
            _config.ApplicationName,
            new TokenResponse { RefreshToken = refreshToken });

        return new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = _config.ApplicationName
        });
    }
}
