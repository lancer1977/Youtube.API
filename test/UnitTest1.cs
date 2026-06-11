using System.Reactive.Linq;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PolyhydraGames.APi.Youtube;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Extensions;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.APi.Youtube.Services;
using PolyhydraGames.Core.Test;
using Microsoft.Extensions.Logging.Abstractions;
using PolyhydraGames.PostOffice.Abstractions;
using PolyhydraGames.Platforms.Abstractions;

namespace Test;

[TestFixture]
public class ApiTests
{
    [TestCase("PT1H1M1S", ExpectedResult = 3661)]
    public int ToSeconds(string duration) => duration.ToSeconds();

    [TestCase("PT1H1M1S", ExpectedResult = 61)]
    public int ToMinutes(string duration) => duration.ToMinutes();

    [Test]
    public async Task AsUserQuery_DelegatesToUnderlyingQuery()
    {
        var query = new FakeYoutubeQuery();
        var userQuery = query.AsUserQuery("@DreadBreadcrumb-w4q");

        var videos = await userQuery.GetVideos();
        var playlists = await userQuery.GetUserPlaylists();
        var count = await userQuery.GetVideosOfGameCount("Bomberman", "tg16");

        Assert.That(query.LastUsername, Is.EqualTo("@DreadBreadcrumb-w4q"));
        Assert.That(videos, Has.Count.EqualTo(1));
        Assert.That(playlists, Has.Count.EqualTo(1));
        Assert.That(count, Is.EqualTo(7));
        Assert.That(query.LastQuery, Is.EqualTo("Bomberman:tg16"));
    }

    [Test]
    public void AddYouTubeApi_BindsApiKeyConfigurationAndRegistersQueryService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["YouTube:Enabled"] = "true",
                ["YouTube:ApiKey"] = "test-api-key",
                ["YouTube:ApplicationName"] = "TestApp"
            })
            .Build();

        services.AddYouTubeApi(configuration);

        using var provider = services.BuildServiceProvider();
        var youtubeConfig = provider.GetRequiredService<IYoutubeConfig>();

        Assert.Multiple(() =>
        {
            Assert.That(youtubeConfig, Is.TypeOf<YoutubeConfig>());
            Assert.That(youtubeConfig.ApiKey, Is.EqualTo("test-api-key"));
            Assert.That(youtubeConfig.ApplicationName, Is.EqualTo("TestApp"));
            Assert.That(provider.GetRequiredService<IYoutubeQuery>(), Is.Not.Null);
        });
    }

    [Test]
    public void AddYouTubeLiveStreaming_RegistersLiveAdaptersWhenOAuthAndLiveAreEnabled()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["YouTube:Enabled"] = "true",
                ["YouTube:ApiKey"] = "api-key",
                ["YouTube:ClientId"] = "client-id",
                ["YouTube:ClientSecret"] = "client-secret",
                ["YouTube:RefreshToken"] = "refresh-token",
                ["YouTube:Live:Enabled"] = "true",
                ["YouTube:Live:BroadcastId"] = "broadcast-123",
                ["YouTube:Live:EnableOutboundChat"] = "true"
            })
            .Build();

        services.AddYouTubeLiveStreaming(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IYouTubeLiveChatGateway>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<YouTubeInboundSource>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IOutboundSink>(), Is.Not.Null);
        });
    }

    [Test]
    public void YouTubeInboundSource_Publish_EmitsNormalizedEnvelope()
    {
        var source = new YouTubeInboundSource(NullLogger<YouTubeInboundSource>.Instance);
        var channelKey = new ChannelKey(new UserId(Guid.Parse("00000000-0000-0000-0000-000000000010")), Platform.Youtube, "channel-123");
        var message = new LiveChatMessage
        {
            Id = "message-1",
            Snippet = new LiveChatMessageSnippet
            {
                LiveChatId = "livechat-1",
                DisplayMessage = "hello from youtube",
                PublishedAtDateTimeOffset = DateTimeOffset.Parse("2026-06-11T00:00:00Z")
            },
            AuthorDetails = new LiveChatMessageAuthorDetails
            {
                ChannelId = "viewer-42",
                DisplayName = "Watcher",
                IsChatModerator = true
            }
        };

        InboundEnvelope? captured = null;
        using var subscription = source.Messages.Subscribe(envelope => captured = envelope);

        source.Publish(channelKey, message);

        Assert.That(captured, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(captured!.Channel.Platform, Is.EqualTo(Platform.Youtube));
            Assert.That(captured.Source, Is.EqualTo(InboundEnvelope.Sources.YouTubeChat));
            Assert.That(captured.Text, Is.EqualTo("hello from youtube"));
            Assert.That(captured.ViewerName, Is.EqualTo("Watcher"));
            Assert.That(captured.PlatformRawViewerId, Is.EqualTo("viewer-42"));
            Assert.That(captured.Meta!["youtubeLiveChatId"], Is.EqualTo("livechat-1"));
            Assert.That(captured.UserRole.HasFlag(ChannelRole.Admin), Is.True);
        });
    }

    [Test]
    public async Task YouTubeOutboundSink_UsesExplicitLiveChatIdWhenProvided()
    {
        var gateway = new FakeYouTubeLiveChatGateway();
        var sink = new YouTubeOutboundSink(gateway, NullLogger<YouTubeOutboundSink>.Instance);
        var channel = new ChannelKey(new UserId(Guid.Parse("00000000-0000-0000-0000-000000000011")), Platform.Youtube, "channel-123");
        var envelope = new OutboundEnvelope(channel, OutboundEnvelopeKind.ChannelMessage, "outbound hello", string.Empty, true,
            new Dictionary<string, string> { ["liveChatId"] = "livechat-xyz" });

        await sink.SendAsync(envelope);

        Assert.Multiple(() =>
        {
            Assert.That(gateway.ResolvedLiveChatIdCalls, Is.EqualTo(0));
            Assert.That(gateway.LastLiveChatId, Is.EqualTo("livechat-xyz"));
            Assert.That(gateway.LastMessage, Is.EqualTo("outbound hello"));
        });
    }

    private sealed class FakeYoutubeQuery : IYoutubeQuery
    {
        public string? LastUsername { get; private set; }
        public string LastQuery { get; private set; } = string.Empty;

        public Task<IList<SearchResult>> GetUserVideos(string username, string query = "")
        {
            LastUsername = username;
            LastQuery = query;
            return Task.FromResult<IList<SearchResult>>(new List<SearchResult>
            {
                new()
                {
                    Id = new ResourceId { VideoId = "video-1" },
                    Snippet = new SearchResultSnippet { Title = "Sample video" }
                }
            });
        }

        public Task<IList<PlaylistSnippet>> GetUserPlaylists(string username)
        {
            LastUsername = username;
            return Task.FromResult<IList<PlaylistSnippet>>(new List<PlaylistSnippet>
            {
                new() { Title = "Sample playlist" }
            });
        }

        public Task<Video?> GetVideoDetails(string videoId)
            => Task.FromResult<Video?>(new Video());

        public Task<string?> GetUserID(string username)
        {
            LastUsername = username;
            return Task.FromResult<string?>("channel-123");
        }

        public Task<int> GetVideosOfGameCount(string username, string gameName, string system)
        {
            LastUsername = username;
            LastQuery = $"{gameName}:{system}";
            return Task.FromResult(7);
        }
    }

    private sealed class FakeYouTubeLiveChatGateway : IYouTubeLiveChatGateway
    {
        public int ResolvedLiveChatIdCalls { get; private set; }
        public string? LastLiveChatId { get; private set; }
        public string? LastMessage { get; private set; }

        public Task<string?> ResolveLiveChatIdAsync(CancellationToken ct = default)
        {
            ResolvedLiveChatIdCalls++;
            return Task.FromResult<string?>("resolved-livechat-id");
        }

        public Task<string?> SendMessageAsync(string liveChatId, string text, CancellationToken ct = default)
        {
            LastLiveChatId = liveChatId;
            LastMessage = text;
            return Task.FromResult<string?>("sent-message-id");
        }

        public Task<IReadOnlyList<LiveChatMessage>> ListMessagesAsync(string liveChatId, string? pageToken = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<LiveChatMessage>>(Array.Empty<LiveChatMessage>());
    }
}

public class ExtensionTests
{
    [TestCase("PT1H1M1S", ExpectedResult = 3661)]
    public int ToSeconds(string duration) => duration.ToSeconds();

    [TestCase("PT1H1M1S", ExpectedResult = 61)]
    public int ToMinutes(string duration) => duration.ToMinutes();
}
