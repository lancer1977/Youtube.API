using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
        services.AddLogging();

        var section = configuration.GetSection("YouTube");
        services
            .AddOptions<YouTubeApiOptions>()
            .Bind(section)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<YouTubeApiOptions>, YouTubeApiOptionsValidator>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<YouTubeApiOptions>>().Value);

        if (section is null || !section.Exists())
        {
            return services;
        }

        var enabled = section.GetValue<bool>("Enabled");

        if (respectEnabledFlag && !enabled)
        {
            return services;
        }

        services.AddSingleton<IYoutubeConfig>(new YoutubeConfig(
            section.GetValue<string?>("ApiKey") ?? string.Empty,
            section.GetValue<string?>("ApplicationName") ?? "PolyhydraGames",
            section.GetValue<string?>("ClientId"),
            section.GetValue<string?>("ClientSecret"),
            section.GetValue<string?>("RefreshToken")));

        services.AddSingleton<IYouTubeApiClientFactory, YouTubeApiClientFactory>();
        services.TryAddSingleton<IYouTubeAuthStateStore, YouTubeAuthStateStore>();
        services.AddSingleton<IYoutubeQuery, YoutubeQueryService>();

        return services;
    }

    public static IServiceCollection AddYouTubeLiveStreaming(
        this IServiceCollection services,
        IConfiguration configuration,
        bool respectEnabledFlag = true)
    {
        var section = configuration.GetSection("YouTube");
        if (section is null || !section.Exists())
        {
            return services;
        }

        services.AddYouTubeApi(configuration, respectEnabledFlag);

        if (respectEnabledFlag && !section.GetValue<bool>("Live:Enabled"))
        {
            return services;
        }

        if (string.IsNullOrWhiteSpace(section.GetValue<string?>("ClientId")) ||
            string.IsNullOrWhiteSpace(section.GetValue<string?>("ClientSecret")) ||
            string.IsNullOrWhiteSpace(section.GetValue<string?>("RefreshToken")))
        {
            return services;
        }

        services.AddSingleton(s => s.GetRequiredService<YouTubeApiOptions>().Live);

        services.AddSingleton<IYouTubeLiveChatGateway, GoogleYouTubeLiveChatGateway>();
        services.AddSingleton<IYouTubeLiveChatService>(sp => sp.GetRequiredService<IYouTubeLiveChatGateway>());
        services.AddSingleton<IYouTubeLiveBroadcastResolver, YouTubeLiveBroadcastResolver>();
        services.AddSingleton<IYouTubeStreamContextResolver, YouTubeStreamContextResolver>();
        services.TryAddSingleton<IYouTubeLiveChatCheckpointStore, YouTubeLiveChatCheckpointStore>();
        services.AddSingleton<IYouTubeLiveChatPoller, YouTubeLiveChatPoller>();
        services.AddSingleton<YouTubeInboundSource>();
        services.AddSingleton<IYouTubeLiveClient, YouTubeLiveClient>();
        services.AddHostedService(sp =>
            (YouTubeLiveChatPoller)sp.GetRequiredService<IYouTubeLiveChatPoller>());

        if (section.GetValue<bool>("Live:EnableOutboundChat"))
        {
            services.AddSingleton<IOutboundSink, YouTubeOutboundSink>();
        }

        return services;
    }
}
