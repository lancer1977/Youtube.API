using System.Net;
using System.Reactive.Linq;
using Google;
using Google.Apis.Requests;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PolyhydraGames.APi.Youtube;
using PolyhydraGames.APi.Youtube.Configuration;
using PolyhydraGames.APi.Youtube.Extensions;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.APi.Youtube.Models;
using PolyhydraGames.APi.Youtube.Services;
using PolyhydraGames.Core.Test;
using PolyhydraGames.Platforms.Abstractions;
using PolyhydraGames.PostOffice.Abstractions;

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
        var options = provider.GetRequiredService<IOptions<YouTubeApiOptions>>().Value;
        var youtubeConfig = provider.GetRequiredService<IYoutubeConfig>();

        Assert.Multiple(() =>
        {
            Assert.That(options.ApiKey, Is.EqualTo("test-api-key"));
            Assert.That(options.ApplicationName, Is.EqualTo("TestApp"));
            Assert.That(youtubeConfig, Is.TypeOf<YoutubeConfig>());
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
                ["YouTube:Live:Enabled"] = "true",
                ["YouTube:ClientId"] = "client-id",
                ["YouTube:ClientSecret"] = "client-secret",
                ["YouTube:RefreshToken"] = "refresh-token",
                ["YouTube:Live:OwnerUserId"] = "00000000-0000-0000-0000-000000000099",
                ["YouTube:Live:BroadcastId"] = "broadcast-123",
                ["YouTube:Live:EnableOutboundChat"] = "true"
            })
            .Build();

        services.AddYouTubeLiveStreaming(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IYouTubeLiveClient>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IYouTubeLiveChatService>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IYouTubeLiveChatPoller>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IYouTubeStreamContextResolver>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<YouTubeInboundSource>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IOutboundSink>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IYouTubeAuthStateStore>(), Is.Not.Null);
        });
    }

    [Test]
    public void YouTubeAuthStateStore_SeparatesStreamerStateByKey()
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
                ["YouTube:Live:OwnerUserId"] = "streamer-a"
            })
            .Build();

        services.AddYouTubeApi(configuration);

        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IYouTubeAuthStateStore>();

        Assert.That(store.TryGet("streamer-a", out var streamerA), Is.True);
        var streamerAState = streamerA ?? throw new AssertionException("Expected streamer-a auth state.");
        Assert.That(streamerAState.RefreshToken, Is.EqualTo("refresh-token"));

        store.Set("streamer-b", new YouTubeAuthState("refresh-token-b"));

        Assert.Multiple(() =>
        {
            Assert.That(store.TryGet("streamer-a", out var stillStreamerA), Is.True);
            var stillStreamerAState = stillStreamerA ?? throw new AssertionException("Expected streamer-a auth state.");
            Assert.That(stillStreamerAState.RefreshToken, Is.EqualTo("refresh-token"));
            Assert.That(store.TryGet("streamer-b", out var streamerB), Is.True);
            var streamerBState = streamerB ?? throw new AssertionException("Expected streamer-b auth state.");
            Assert.That(streamerBState.RefreshToken, Is.EqualTo("refresh-token-b"));
        });
    }

    [Test]
    public async Task YouTubeLiveClient_EmitsNormalizedMessagesAndUsesGatewayForSend()
    {
        var channelKey = new ChannelKey(new UserId(Guid.Parse("00000000-0000-0000-0000-000000000014")), Platform.Youtube, "channel-123");
        var inboundSource = new YouTubeInboundSource(NullLogger<YouTubeInboundSource>.Instance);
        var gateway = new FakeYouTubeLiveChatGateway();
        var poller = new FakeYouTubeLiveChatPoller();
        var client = new YouTubeLiveClient(gateway, poller, inboundSource);

        YouTubeChatMessageReceived? received = null;
        using var subscription = client.OnMessageReceived.Subscribe(message => received = message);

        inboundSource.Publish(channelKey, new YouTubeLiveChatMessage(
            "message-1",
            "livechat-1",
            "broadcast-1",
            "video-1",
            "hello from the facade",
            DateTimeOffset.Parse("2026-06-11T00:00:00Z"),
            new YouTubeLiveChatAuthor("author-1", "Watcher"),
            new LiveChatMessage()));

        await client.ConnectAsync();
        await client.SendMessageAsync("outbound from facade");
        await client.DisconnectAsync();

        Assert.That(received, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(received!.MessageId, Is.EqualTo("message-1"));
            Assert.That(received.Text, Is.EqualTo("hello from the facade"));
            Assert.That(received.LiveChatId, Is.EqualTo("livechat-1"));
            Assert.That(poller.StartCount, Is.EqualTo(1));
            Assert.That(poller.StopCount, Is.EqualTo(1));
            Assert.That(gateway.ResolveCalls, Is.GreaterThanOrEqualTo(1));
            Assert.That(gateway.SentMessages, Has.Count.EqualTo(1));
            Assert.That(gateway.SentMessages[0], Is.EqualTo(("resolved-livechat-id", "outbound from facade")));
        });
    }

    [Test]
    public void AddYouTubeApi_ValidationFailsWhenLiveEnabledWithoutBroadcastOrChannel()
    {
        var validator = new YouTubeApiOptionsValidator();
        var result = validator.Validate("youTube", new YouTubeApiOptions
        {
            Enabled = true,
            Live = new YouTubeLiveOptions
            {
                Enabled = true,
                OwnerUserId = "00000000-0000-0000-0000-000000000099"
            }
        });

        Assert.That(result.Failed, Is.True);
    }

    [Test]
    public void AddYouTubeApi_ValidationFailsWhenPollIntervalIsTooSmall()
    {
        var validator = new YouTubeApiOptionsValidator();
        var result = validator.Validate("youTube", new YouTubeApiOptions
        {
            Enabled = true,
            ApiKey = "api-key",
            Live = new YouTubeLiveOptions
            {
                Enabled = true,
                OwnerUserId = "00000000-0000-0000-0000-000000000099",
                ChannelId = "channel-123",
                PollInterval = TimeSpan.FromSeconds(4)
            }
        });

        Assert.That(result.Failed, Is.True);
        Assert.That(result.Failures, Has.Some.Contains("at least 00:00:05"));
    }

    [Test]
    public async Task YouTubeLiveChatCheckpointStore_RoundsTripCheckpointState()
    {
        var options = new YouTubeApiOptions
        {
            Live = new YouTubeLiveOptions
            {
                CheckpointTtl = TimeSpan.FromMinutes(10)
            }
        };
        var store = new YouTubeLiveChatCheckpointStore(options);
        var channelKey = new ChannelKey(new UserId(Guid.Parse("00000000-0000-0000-0000-000000000012")), Platform.Youtube, "channel-123");
        var state = new YouTubeLiveChatPollerState(
            "broadcast-1",
            "livechat-1",
            "channel-123",
            "video-1",
            "page-token",
            new[] { "message-1", "message-2" },
            YouTubeLiveChatPollerPhase.Resolved,
            DateTimeOffset.UtcNow);

        await store.SaveAsync(channelKey, state);
        var restored = await store.LoadAsync(channelKey);

        Assert.That(restored, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(restored!.LiveChatId, Is.EqualTo("livechat-1"));
            Assert.That(restored.PageToken, Is.EqualTo("page-token"));
            Assert.That(restored.SeenMessageIds, Is.EqualTo(new[] { "message-1", "message-2" }));
            Assert.That(restored.Phase, Is.EqualTo(YouTubeLiveChatPollerPhase.Resolved));
        });
    }

    [Test]
    public void YouTubeInboundSource_Publish_EmitsNormalizedEnvelope()
    {
        var source = new YouTubeInboundSource(NullLogger<YouTubeInboundSource>.Instance);
        var channelKey = new ChannelKey(new UserId(Guid.Parse("00000000-0000-0000-0000-000000000010")), Platform.Youtube, "channel-123");
        var message = new YouTubeLiveChatMessage(
            "message-1",
            "livechat-1",
            "broadcast-1",
            "video-1",
            "hello from youtube",
            DateTimeOffset.Parse("2026-06-11T00:00:00Z"),
            new YouTubeLiveChatAuthor("author-1", "Watcher", IsChatModerator: true),
            new LiveChatMessage());

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
            Assert.That(captured.PlatformRawViewerId, Is.EqualTo("author-1"));
            Assert.That(captured.Meta!["youtubeBroadcastId"], Is.EqualTo("broadcast-1"));
            Assert.That(captured.UserRole.HasFlag(ChannelRole.Admin), Is.True);
        });
    }

    [Test]
    public async Task YouTubeOutboundSink_UsesExplicitLiveChatIdWhenProvided()
    {
        var chatService = new FakeYouTubeLiveChatService();
        var contextResolver = new FakeYouTubeStreamContextResolver();
        var options = new YouTubeApiOptions
        {
            Enabled = true,
            Live = new YouTubeLiveOptions
            {
                Enabled = true,
                OwnerUserId = "00000000-0000-0000-0000-000000000099",
                EnableOutboundChat = true
            },
            ClientId = "client-id",
            ClientSecret = "client-secret",
            RefreshToken = "refresh-token"
        };
        var sink = new YouTubeOutboundSink(chatService, contextResolver, options, NullLogger<YouTubeOutboundSink>.Instance);
        var channel = new ChannelKey(new UserId(Guid.Parse("00000000-0000-0000-0000-000000000011")), Platform.Youtube, "channel-123");
        var envelope = new OutboundEnvelope(channel, OutboundEnvelopeKind.ChannelMessage, "outbound hello", string.Empty, true,
            new Dictionary<string, string> { ["liveChatId"] = "livechat-xyz" });

        await sink.SendAsync(envelope);

        Assert.Multiple(() =>
        {
            Assert.That(chatService.ExplicitLiveChatIdUsed, Is.True);
            Assert.That(chatService.LastLiveChatId, Is.EqualTo("livechat-xyz"));
            Assert.That(chatService.LastMessage, Is.EqualTo("outbound hello"));
        });
    }

    [Test]
    public async Task YouTubeOutboundSink_UsesResolvedContextWhenEnvelopeOmitsLiveChatId()
    {
        var chatService = new FakeYouTubeLiveChatService();
        var contextResolver = new FakeYouTubeStreamContextResolver();
        var options = new YouTubeApiOptions
        {
            Enabled = true,
            Live = new YouTubeLiveOptions
            {
                Enabled = true,
                OwnerUserId = "00000000-0000-0000-0000-000000000099",
                EnableOutboundChat = true
            },
            ClientId = "client-id",
            ClientSecret = "client-secret",
            RefreshToken = "refresh-token"
        };
        var sink = new YouTubeOutboundSink(chatService, contextResolver, options, NullLogger<YouTubeOutboundSink>.Instance);
        var channel = new ChannelKey(new UserId(Guid.Parse("00000000-0000-0000-0000-000000000011")), Platform.Youtube, "channel-123");
        var envelope = new OutboundEnvelope(channel, OutboundEnvelopeKind.ChannelMessage, "outbound hello", string.Empty, true);

        await sink.SendAsync(envelope);

        Assert.That(chatService.LastLiveChatId, Is.EqualTo("resolved-livechat-id"));
    }

    [Test]
    public async Task YouTubeLiveChatPoller_RebindsExpiredChatAndAvoidsDuplicateReplay()
    {
        var channelKey = new ChannelKey(new UserId(Guid.Parse("00000000-0000-0000-0000-000000000013")), Platform.Youtube, "channel-123");
        var firstContext = new YouTubeStreamContext("broadcast-1", "old-chat", "channel-123", "video-1", channelKey);
        var secondContext = new YouTubeStreamContext("broadcast-1", "new-chat", "channel-123", "video-1", channelKey);
        var resolver = new ScriptedStreamContextResolver(firstContext, secondContext);
        var chatService = new ScriptedLiveChatService(
            expiredLiveChatId: "old-chat",
            replacementBatch: new YouTubeLiveChatMessageBatch(
                [
                    new YouTubeLiveChatMessage(
                        "message-2",
                        "new-chat",
                        "broadcast-1",
                        "video-1",
                        "hello from new chat",
                        DateTimeOffset.Parse("2026-06-11T00:00:00Z"),
                        new YouTubeLiveChatAuthor("author-2", "Watcher 2"),
                        new LiveChatMessage())
                ],
                null,
                TimeSpan.FromSeconds(5)));
        var checkpointStore = new YouTubeLiveChatCheckpointStore(new YouTubeApiOptions
        {
            Live = new YouTubeLiveOptions
            {
                CheckpointTtl = TimeSpan.FromMinutes(10)
            }
        });
        await checkpointStore.SaveAsync(channelKey, new YouTubeLiveChatPollerState(
            "broadcast-1",
            "old-chat",
            "channel-123",
            "video-1",
            "page-token",
            new[] { "message-1" },
            YouTubeLiveChatPollerPhase.Resolved,
            DateTimeOffset.UtcNow));

        var source = new YouTubeInboundSource(NullLogger<YouTubeInboundSource>.Instance);
        var poller = new YouTubeLiveChatPoller(
            resolver,
            chatService,
            source,
            checkpointStore,
            new YouTubeApiOptions
            {
                Enabled = true,
                Live = new YouTubeLiveOptions
                {
                    Enabled = true,
                    OwnerUserId = "00000000-0000-0000-0000-000000000013",
                    ChannelId = "channel-123",
                    AutoDiscoverActiveBroadcast = false,
                    PollInterval = TimeSpan.FromSeconds(5),
                    StateWindowSize = 16
                }
            },
            NullLogger<YouTubeLiveChatPoller>.Instance);

        var received = 0;
        using var subscription = source.Messages.Subscribe(_ => Interlocked.Increment(ref received));

        await poller.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => received > 0, TimeSpan.FromSeconds(5));
        await poller.StopAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(chatService.ListRequests, Has.Count.EqualTo(2));
            Assert.That(chatService.ListRequests[0].liveChatId, Is.EqualTo("old-chat"));
            Assert.That(chatService.ListRequests[1].liveChatId, Is.EqualTo("new-chat"));
            Assert.That(received, Is.EqualTo(1));
            Assert.That(resolver.CallCount, Is.GreaterThanOrEqualTo(2));
        });
    }

    [Test]
    public void AddYouTubeApi_DoesNotRegisterLiveStreamingServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["YouTube:Enabled"] = "true",
                ["YouTube:ApiKey"] = "test-api-key"
            })
            .Build();

        services.AddYouTubeApi(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetService<IYouTubeLiveClient>(), Is.Null);
            Assert.That(provider.GetService<IYouTubeLiveChatService>(), Is.Null);
            Assert.That(provider.GetService<IYouTubeLiveChatPoller>(), Is.Null);
        });
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var started = DateTimeOffset.UtcNow;
        while (!condition())
        {
            if (DateTimeOffset.UtcNow - started > timeout)
            {
                Assert.Fail("Timed out waiting for condition.");
            }

            await Task.Delay(25);
        }
    }

    private sealed class FakeYoutubeQuery : IYoutubeQuery
    {
        public string? LastUsername { get; private set; }
        public string LastQuery { get; private set; } = string.Empty;

        public Task<IReadOnlyList<SearchResult>> GetUserVideos(string username, string query = "", CancellationToken ct = default)
        {
            LastUsername = username;
            LastQuery = query;
            return Task.FromResult<IReadOnlyList<SearchResult>>(new List<SearchResult>
            {
                new()
                {
                    Id = new ResourceId { VideoId = "video-1" },
                    Snippet = new SearchResultSnippet { Title = "Sample video" }
                }
            });
        }

        public Task<IReadOnlyList<PlaylistSnippet>> GetUserPlaylists(string username, CancellationToken ct = default)
        {
            LastUsername = username;
            return Task.FromResult<IReadOnlyList<PlaylistSnippet>>(new List<PlaylistSnippet>
            {
                new() { Title = "Sample playlist" }
            });
        }

        public Task<Video?> GetVideoDetails(string videoId, CancellationToken ct = default)
            => Task.FromResult<Video?>(new Video());

        public Task<string?> GetUserID(string username, CancellationToken ct = default)
        {
            LastUsername = username;
            return Task.FromResult<string?>("channel-123");
        }

        public Task<int> GetVideosOfGameCount(string username, string gameName, string system, CancellationToken ct = default)
        {
            LastUsername = username;
            LastQuery = $"{gameName}:{system}";
            return Task.FromResult(7);
        }
    }

    private sealed class FakeYouTubeLiveChatService : IYouTubeLiveChatService
    {
        public bool ExplicitLiveChatIdUsed { get; private set; }
        public string? LastLiveChatId { get; private set; }
        public string? LastMessage { get; private set; }

        public Task<string?> ResolveLiveChatIdAsync(CancellationToken ct = default)
            => Task.FromResult<string?>("resolved-livechat-id");

        public Task<YouTubeLiveChatContext?> ResolveBroadcastContextAsync(string broadcastId, CancellationToken ct = default)
            => Task.FromResult<YouTubeLiveChatContext?>(new YouTubeLiveChatContext(broadcastId, "resolved-livechat-id", "channel", "video-1"));

        public Task<YouTubeLiveChatContext?> ResolveActiveBroadcastContextAsync(string channelId, CancellationToken ct = default)
            => Task.FromResult<YouTubeLiveChatContext?>(new YouTubeLiveChatContext("broadcast-1", "resolved-livechat-id", channelId, "video-1"));

        public Task<YouTubeLiveChatMessageBatch> ListMessagesAsync(
            string liveChatId,
            string? pageToken = null,
            CancellationToken ct = default)
            => Task.FromResult(new YouTubeLiveChatMessageBatch(Array.Empty<YouTubeLiveChatMessage>(), null, TimeSpan.FromSeconds(5)));

        public Task<string?> SendMessageAsync(string liveChatId, string text, CancellationToken ct = default)
        {
            ExplicitLiveChatIdUsed = true;
            LastLiveChatId = liveChatId;
            LastMessage = text;
            return Task.FromResult<string?>("sent-message-id");
        }
    }

    private sealed class FakeYouTubeStreamContextResolver : IYouTubeStreamContextResolver
    {
        public Task<YouTubeStreamContext?> ResolveAsync(CancellationToken ct = default)
        {
            return Task.FromResult<YouTubeStreamContext?>(new YouTubeStreamContext(
                "broadcast-1",
                "resolved-livechat-id",
                "channel-1",
                "video-1",
                new ChannelKey(new UserId(Guid.Parse("00000000-0000-0000-0000-000000000011")), Platform.Youtube, "channel-1")));
        }
    }

    private sealed class ScriptedStreamContextResolver : IYouTubeStreamContextResolver
    {
        private readonly Queue<YouTubeStreamContext?> _contexts;

        public ScriptedStreamContextResolver(params YouTubeStreamContext?[] contexts)
        {
            _contexts = new Queue<YouTubeStreamContext?>(contexts);
        }

        public int CallCount { get; private set; }

        public Task<YouTubeStreamContext?> ResolveAsync(CancellationToken ct = default)
        {
            CallCount++;
            if (_contexts.Count > 1)
            {
                return Task.FromResult(_contexts.Dequeue());
            }

            return Task.FromResult(_contexts.Count == 1 ? _contexts.Peek() : null);
        }
    }

    private sealed class ScriptedLiveChatService : IYouTubeLiveChatService
    {
        private readonly string _expiredLiveChatId;
        private readonly YouTubeLiveChatMessageBatch _replacementBatch;
        private int _listCallCount;

        public ScriptedLiveChatService(string expiredLiveChatId, YouTubeLiveChatMessageBatch replacementBatch)
        {
            _expiredLiveChatId = expiredLiveChatId;
            _replacementBatch = replacementBatch;
        }

        public List<(string liveChatId, string? pageToken)> ListRequests { get; } = [];

        public Task<string?> ResolveLiveChatIdAsync(CancellationToken ct = default)
            => Task.FromResult<string?>(null);

        public Task<YouTubeLiveChatContext?> ResolveBroadcastContextAsync(string broadcastId, CancellationToken ct = default)
            => Task.FromResult<YouTubeLiveChatContext?>(null);

        public Task<YouTubeLiveChatContext?> ResolveActiveBroadcastContextAsync(string channelId, CancellationToken ct = default)
            => Task.FromResult<YouTubeLiveChatContext?>(null);

        public Task<YouTubeLiveChatMessageBatch> ListMessagesAsync(string liveChatId, string? pageToken = null, CancellationToken ct = default)
        {
            ListRequests.Add((liveChatId, pageToken));
            _listCallCount++;

            if (_listCallCount == 1 && liveChatId == _expiredLiveChatId)
            {
                throw CreateExpiredChatException();
            }

            return Task.FromResult(_replacementBatch);
        }

        public Task<string?> SendMessageAsync(string liveChatId, string text, CancellationToken ct = default)
            => Task.FromResult<string?>("sent-message-id");

        private static GoogleApiException CreateExpiredChatException()
        {
            var exception = new GoogleApiException("YouTube", "live chat expired");
            exception.HttpStatusCode = HttpStatusCode.Gone;
            exception.Error = new RequestError
            {
                Code = (int)HttpStatusCode.Gone,
                Message = "live chat expired",
                Errors = new List<SingleError>
                {
                    new()
                    {
                        Reason = "liveChatEnded",
                        Message = "live chat expired"
                    }
                }
            };

            return exception;
        }
    }

    private sealed class FakeYouTubeLiveChatGateway : IYouTubeLiveChatGateway
    {
        public int ResolveCalls { get; private set; }
        public List<(string liveChatId, string message)> SentMessages { get; } = [];

        public Task<string?> ResolveLiveChatIdAsync(CancellationToken ct = default)
        {
            ResolveCalls++;
            return Task.FromResult<string?>("resolved-livechat-id");
        }

        public Task<YouTubeLiveChatContext?> ResolveBroadcastContextAsync(string broadcastId, CancellationToken ct = default)
            => Task.FromResult<YouTubeLiveChatContext?>(new YouTubeLiveChatContext(broadcastId, "resolved-livechat-id", "channel-123", "video-1"));

        public Task<YouTubeLiveChatContext?> ResolveActiveBroadcastContextAsync(string channelId, CancellationToken ct = default)
            => Task.FromResult<YouTubeLiveChatContext?>(new YouTubeLiveChatContext("broadcast-1", "resolved-livechat-id", channelId, "video-1"));

        public Task<YouTubeLiveChatMessageBatch> ListMessagesAsync(string liveChatId, string? pageToken = null, CancellationToken ct = default)
            => Task.FromResult(new YouTubeLiveChatMessageBatch(Array.Empty<YouTubeLiveChatMessage>(), null, TimeSpan.FromSeconds(5)));

        public Task<string?> SendMessageAsync(string liveChatId, string text, CancellationToken ct = default)
        {
            SentMessages.Add((liveChatId, text));
            return Task.FromResult<string?>("sent-message-id");
        }
    }

    private sealed class FakeYouTubeLiveChatPoller : IYouTubeLiveChatPoller
    {
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartCount++;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCount++;
            return Task.CompletedTask;
        }
    }
}

public class ExtensionTests
{
    [TestCase("PT1H1M1S", ExpectedResult = 3661)]
    public int ToSeconds(string duration) => duration.ToSeconds();

    [TestCase("PT1H1M1S", ExpectedResult = 61)]
    public int ToMinutes(string duration) => duration.ToMinutes();
}
