using Microsoft.Extensions.Configuration;

namespace Test.Integration;

internal static class YouTubeIntegrationTestSupport
{
    private const string EnvFlag = "YOUTUBE_INTEGRATION";

    internal static IConfiguration? LoadConfigurationOrSkip()
    {
        if (!IsEnabled())
        {
            return null;
        }

        var values = new Dictionary<string, string?>
        {
            ["YouTube:Enabled"] = "true",
            ["YouTube:ApiKey"] = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY"),
            ["YouTube:ApplicationName"] = Environment.GetEnvironmentVariable("YOUTUBE_APPLICATION_NAME") ?? "PolyhydraGames.API.Youtube.Tests",
            ["YouTube:ClientId"] = Environment.GetEnvironmentVariable("YOUTUBE_CLIENT_ID"),
            ["YouTube:ClientSecret"] = Environment.GetEnvironmentVariable("YOUTUBE_CLIENT_SECRET"),
            ["YouTube:RefreshToken"] = Environment.GetEnvironmentVariable("YOUTUBE_REFRESH_TOKEN"),
            ["YouTube:Live:Enabled"] = "true",
            ["YouTube:Live:OwnerUserId"] = Environment.GetEnvironmentVariable("YOUTUBE_OWNER_USER_ID"),
            ["YouTube:Live:ChannelId"] = Environment.GetEnvironmentVariable("YOUTUBE_CHANNEL_ID"),
            ["YouTube:Live:BroadcastId"] = Environment.GetEnvironmentVariable("YOUTUBE_BROADCAST_ID"),
            ["YouTube:Live:EnableOutboundChat"] = "true"
        };

        if (string.IsNullOrWhiteSpace(values["YouTube:ApiKey"]) &&
            (string.IsNullOrWhiteSpace(values["YouTube:ClientId"]) ||
             string.IsNullOrWhiteSpace(values["YouTube:ClientSecret"]) ||
             string.IsNullOrWhiteSpace(values["YouTube:RefreshToken"])))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(values["YouTube:Live:OwnerUserId"]))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(values["YouTube:Live:ChannelId"]) &&
            string.IsNullOrWhiteSpace(values["YouTube:Live:BroadcastId"]))
        {
            return null;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static bool IsEnabled()
        => string.Equals(Environment.GetEnvironmentVariable(EnvFlag), "1", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(Environment.GetEnvironmentVariable(EnvFlag), "true", StringComparison.OrdinalIgnoreCase);
}
