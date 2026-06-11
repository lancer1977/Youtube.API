using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PolyhydraGames.APi.Youtube.Configuration;

namespace PolyhydraGames.APi.Youtube.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddYouTubeApi(
        this IServiceCollection services,
        IConfiguration configuration,
        bool respectEnabledFlag = true)
    {
        var section = configuration.GetSection("YouTube");
        var options = new YouTubeApiOptions();
        section.Bind(options);

        if (respectEnabledFlag && !options.Enabled)
        {
            return services;
        }

        services.AddSingleton(options);

        return services;
    }

    public static IServiceCollection AddYouTubeLiveStreaming(
        this IServiceCollection services,
        IConfiguration configuration,
        bool respectEnabledFlag = true)
    {
        services.AddYouTubeApi(configuration, respectEnabledFlag);

        // Placeholder registrations for future live services
        return services;
    }
}
