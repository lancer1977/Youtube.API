using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PolyhydraGames.APi.Youtube;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Extensions;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.APi.Youtube.Models;
using PolyhydraGames.APi.Youtube.Services;
using PolyhydraGames.Platforms.Abstractions;

namespace Test.Integration;

[TestFixture]
public sealed class YouTubeIntegrationTests
{
    [Test, Category("Integration")]
    public void AddYouTubeApi_ResolvesRealFactoryAndQuerySurface()
    {
        var configuration = YouTubeIntegrationTestSupport.LoadConfigurationOrSkip();
        if (configuration is null)
        {
            Assert.Ignore("Set YOUTUBE_INTEGRATION=1 with YouTube API credentials to run this integration harness.");
        }

        using var provider = BuildProvider(configuration);

        var options = provider.GetRequiredService<IOptions<YouTubeApiOptions>>().Value;
        var factory = provider.GetRequiredService<IYouTubeApiClientFactory>();
        var query = provider.GetRequiredService<IYoutubeQuery>();

        Assert.Multiple(() =>
        {
            Assert.That(options.Enabled, Is.True);
            Assert.That(options.Live.Enabled, Is.True);
            Assert.That(factory, Is.TypeOf<YouTubeApiClientFactory>());
            Assert.That(query, Is.TypeOf<YoutubeQueryService>());
        });

        var publicService = factory.CreatePublicReadService();
        Assert.That(publicService.ApplicationName, Is.EqualTo(options.ResolvedApplicationName));
    }

    [Test, Category("Integration")]
    public void AddYouTubeLiveStreaming_ResolvesLiveLaneAndRoutesInboundMessages()
    {
        var configuration = YouTubeIntegrationTestSupport.LoadConfigurationOrSkip();
        if (configuration is null)
        {
            Assert.Ignore("Set YOUTUBE_INTEGRATION=1 with YouTube live credentials to run this integration harness.");
        }

        using var provider = BuildLiveProvider(configuration);

        var liveClient = provider.GetRequiredService<IYouTubeLiveClient>();
        var inboundSource = provider.GetRequiredService<YouTubeInboundSource>();
        var received = new List<YouTubeChatMessageReceived>();
        using var subscription = liveClient.OnMessageReceived.Subscribe(received.Add);

        var channelKey = new ChannelKey(new UserId(Guid.Parse("00000000-0000-0000-0000-000000000099")), Platform.Youtube, "channel-123");
        inboundSource.Publish(channelKey, new YouTubeLiveChatMessage(
            "message-1",
            "livechat-1",
            "broadcast-1",
            "video-1",
            "integration hello",
            DateTimeOffset.Parse("2026-06-17T00:00:00Z"),
            new YouTubeLiveChatAuthor("author-1", "Integration Viewer"),
            new Google.Apis.YouTube.v3.Data.LiveChatMessage()));

        Assert.That(provider.GetRequiredService<IYouTubeLiveChatGateway>(), Is.TypeOf<FakeLiveChatGateway>());
        Assert.That(provider.GetRequiredService<IYouTubeLiveChatPoller>(), Is.TypeOf<FakeLiveChatPoller>());
        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(received[0].Text, Is.EqualTo("integration hello"));
    }

    private static ServiceProvider BuildProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddYouTubeApi(configuration, respectEnabledFlag: false);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildLiveProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddYouTubeLiveStreaming(configuration, respectEnabledFlag: false);
        services.AddSingleton<IYouTubeLiveChatGateway, FakeLiveChatGateway>();
        services.AddSingleton<IYouTubeLiveChatService>(sp => sp.GetRequiredService<IYouTubeLiveChatGateway>());
        services.AddSingleton<IYouTubeLiveChatPoller, FakeLiveChatPoller>();
        return services.BuildServiceProvider();
    }

    private sealed class FakeLiveChatGateway : IYouTubeLiveChatGateway
    {
        public Task<string?> ResolveLiveChatIdAsync(CancellationToken ct = default) => Task.FromResult<string?>("resolved-livechat-id");
        public Task<YouTubeLiveChatContext?> ResolveBroadcastContextAsync(string broadcastId, CancellationToken ct = default)
            => Task.FromResult<YouTubeLiveChatContext?>(new YouTubeLiveChatContext("broadcast-1", "resolved-livechat-id", "channel-123", "video-1"));
        public Task<YouTubeLiveChatContext?> ResolveActiveBroadcastContextAsync(string channelId, CancellationToken ct = default)
            => Task.FromResult<YouTubeLiveChatContext?>(new YouTubeLiveChatContext("broadcast-1", "resolved-livechat-id", channelId, "video-1"));
        public Task<YouTubeLiveChatMessageBatch> ListMessagesAsync(string liveChatId, string? pageToken = null, CancellationToken ct = default)
            => Task.FromResult(new YouTubeLiveChatMessageBatch(Array.Empty<YouTubeLiveChatMessage>(), pageToken, TimeSpan.Zero));
        public Task<string?> SendMessageAsync(string liveChatId, string text, CancellationToken ct = default) => Task.FromResult<string?>(text);
    }

    private sealed class FakeLiveChatPoller : IYouTubeLiveChatPoller
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
