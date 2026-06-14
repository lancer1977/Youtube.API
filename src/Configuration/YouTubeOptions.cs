namespace PolyhydraGames.APi.Youtube.Configuration;

public sealed class YouTubeApiOptions
{
    private const string DefaultApplicationName = "PolyhydraGames";

    public bool Enabled { get; init; }
    public string? ApiKey { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? RefreshToken { get; init; }
    public string? ApplicationName { get; init; } = DefaultApplicationName;
    public YouTubeLiveOptions Live { get; init; } = new();

    public bool HasOAuthCredentials =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        !string.IsNullOrWhiteSpace(RefreshToken);

    public string ResolvedApplicationName => string.IsNullOrWhiteSpace(ApplicationName) ? DefaultApplicationName : ApplicationName;
}

public sealed class YouTubeLiveOptions
{
    public bool Enabled { get; init; }
    public string? OwnerUserId { get; init; }
    public string? ChannelId { get; init; }
    public string? BroadcastId { get; init; }
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(6);
    public int StateWindowSize { get; init; } = 128;
    public TimeSpan CheckpointTtl { get; init; } = TimeSpan.FromHours(12);
    public bool AutoDiscoverActiveBroadcast { get; init; } = true;
    public bool EnableOutboundChat { get; init; } = true;
    public int? MaxBackoffSeconds { get; init; } = 64;
}
