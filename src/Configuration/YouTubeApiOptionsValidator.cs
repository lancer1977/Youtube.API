using Microsoft.Extensions.Options;

namespace PolyhydraGames.APi.Youtube.Configuration;

public sealed class YouTubeApiOptionsValidator : IValidateOptions<YouTubeApiOptions>
{
    public ValidateOptionsResult Validate(string? name, YouTubeApiOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ResolvedApplicationName))
        {
            failures.Add("ApplicationName must not be empty.");
        }

        if (!options.Live.Enabled)
        {
            return ValidateIfConfiguredForReadOperations(options, failures);
       }

        if (string.IsNullOrWhiteSpace(options.ApiKey) && !options.HasOAuthCredentials)
        {
            failures.Add(
                "YouTube API requires either ApiKey or OAuth credentials (ClientId/ClientSecret/RefreshToken).");
        }

        if (string.IsNullOrWhiteSpace(options.Live.ChannelId) && string.IsNullOrWhiteSpace(options.Live.BroadcastId))
        {
            failures.Add("YouTube live requires ChannelId or BroadcastId when Live.Enabled is true.");
        }

        if (string.IsNullOrWhiteSpace(options.Live.OwnerUserId))
        {
            failures.Add("YouTube live OwnerUserId is required so the poller can build a ChannelKey.");
        }

        if (options.Live.PollInterval < TimeSpan.FromSeconds(5))
        {
            failures.Add("YouTube live PollInterval must be at least 00:00:05.");
        }

        if (options.Live.EnableOutboundChat && !options.HasOAuthCredentials)
        {
            failures.Add("YouTube live outbound chat requires OAuth credentials.");
        }

        return ValidateIfConfiguredForReadOperations(options, failures);
    }

    private static ValidateOptionsResult ValidateIfConfiguredForReadOperations(
        YouTubeApiOptions options,
        List<string> failures)
    {
        if (!options.Enabled && failures.Count == 0)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.Enabled && string.IsNullOrWhiteSpace(options.ApiKey) && !options.HasOAuthCredentials)
        {
            failures.Add("YouTube API queries require ApiKey or OAuth credentials.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
