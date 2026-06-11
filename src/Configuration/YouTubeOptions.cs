namespace PolyhydraGames.APi.Youtube.Configuration;

public sealed class YouTubeApiOptions
{
    public bool Enabled { get; init; }
    public string? ApiKey { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? RefreshToken { get; init; }
    public string? ApplicationName { get; init; } = "PolyhydraGames";
    public YouTubeLiveOptions Live { get; init; } = new();
}

public sealed class YouTubeLiveOptions
{
    public bool Enabled { get; init; }
    public string? OwnerUserId { get; init; }
    public string? ChannelId { get; init; }
    public string? BroadcastId { get; init; }
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);
    public bool AutoDiscoverActiveBroadcast { get; init; } = true;
    public bool EnableOutboundChat { get; init; } = true;
}
