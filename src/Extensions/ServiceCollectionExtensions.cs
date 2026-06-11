using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.APi.Youtube.Services;
using PolyhydraGames.PostOffice.Abstractions;

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
        services.AddSingleton<IYoutubeConfig>(new YoutubeConfig(
            options.ApiKey ?? string.Empty,
            options.ApplicationName ?? "PolyhydraGames",
            options.ClientId,
            options.ClientSecret,
            options.RefreshToken));
        services.AddSingleton<IYoutubeQuery, YoutubeQueryService>();

        return services;
    }

    public static IServiceCollection AddYouTubeLiveStreaming(
        this IServiceCollection services,
        IConfiguration configuration,
        bool respectEnabledFlag = true)
    {
        var section = configuration.GetSection("YouTube");
        var options = new YouTubeApiOptions();
        section.Bind(options);

        services.AddYouTubeApi(configuration, respectEnabledFlag);

#if NET10_0_OR_GREATER
        if (respectEnabledFlag && !options.Live.Enabled)
        {
            return services;
        }

        if (!options.HasOAuthCredentials)
        {
            return services;
        }

        services.AddSingleton(_ => options.Live);
        services.AddSingleton<IYouTubeLiveChatGateway, GoogleYouTubeLiveChatGateway>();
        services.AddSingleton<YouTubeInboundSource>();
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<YouTubeInboundSource>>(Microsoft.Extensions.Logging.Abstractions.NullLogger<YouTubeInboundSource>.Instance);
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<YouTubeOutboundSink>>(Microsoft.Extensions.Logging.Abstractions.NullLogger<YouTubeOutboundSink>.Instance);

        if (options.Live.EnableOutboundChat)
        {
            services.AddSingleton<IOutboundSink, YouTubeOutboundSink>();
        }
#endif

        return services;
    }
}
