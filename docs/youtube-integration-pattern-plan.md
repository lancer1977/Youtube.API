# YouTube Integration Pattern Plan

## Purpose

Turn `PolyhydraGames.APi.Youtube` into the supporting package pattern for YouTube integrations across Polyhydra Games and ChannelCheevos, including regular YouTube Data API features and live streaming/chat integrations.

The goal is not just to wrap Google APIs. The package should become the YouTube equivalent of the Twitch and Discord platform libraries: a small, reusable integration layer that can plug into shared messaging, OAuth, command routing, overlays, and stream automation workflows.

## Current repository baseline

The repo is currently positioned as a .NET 10 library for the YouTube Data API v3 with a core library under `Src/`, NUnit tests under `Test/`, and detailed documentation under `docs/`.

The solution currently contains one core library project and one test project:

- `PolyhydraGames.APi.Youtube`
- `Test`

This plan keeps that structure but proposes a clearer internal shape so the package can support both YouTube API access and YouTube streaming/live-chat workflows.

## Guiding design

The package should support three lanes:

1. **YouTube Data API lane**
   - Channel lookup
   - Video lookup
   - Playlist lookup
   - Search
   - Upload metadata helpers if needed later
   - Quota-aware API calling

2. **YouTube Live Streaming lane**
   - Live broadcast discovery
   - Active stream lookup
   - Live chat ID resolution
   - Live chat polling
   - Live chat outbound messaging
   - Stream lifecycle awareness

3. **Polyhydra platform integration lane**
   - Convert YouTube chat messages into shared inbound envelopes
   - Send outbound messages through a YouTube outbound sink
   - Resolve `ChannelKey` for YouTube channels and broadcasts
   - Provide DI extension methods consistent with Twitch and Discord packages
   - Provide safe background services for polling and reconnect behavior

## Target package responsibilities

### In scope

- Wrap Google YouTube Data API v3 client setup.
- Encapsulate OAuth/API key configuration.
- Provide strongly typed service interfaces.
- Provide quota-safe request helpers.
- Provide live chat polling abstraction.
- Provide outbound chat sending abstraction.
- Provide Post Office compatible inbound/outbound adapters.
- Provide testable interfaces and fake implementations.
- Provide documentation for ChannelCheevos integration.

### Out of scope for first version

- Full YouTube Studio management.
- Video uploading pipeline.
- Moderation automation beyond basic chat messages.
- Analytics dashboards.
- Multi-account account switching UI.
- OBS integration.

Those can be added later after the core streaming/chat pattern is stable.

## Proposed project structure

```text
Src/
  PolyhydraGames.APi.Youtube.csproj
  Configuration/
    YouTubeApiOptions.cs
    YouTubeLiveOptions.cs
  Extensions/
    ServiceCollectionExtensions.cs
  Interfaces/
    IYouTubeApiClientFactory.cs
    IYouTubeChannelService.cs
    IYouTubeVideoService.cs
    IYouTubePlaylistService.cs
    IYouTubeLiveBroadcastService.cs
    IYouTubeLiveChatService.cs
    IYouTubeLiveChatPoller.cs
    IYouTubeChannelKeyResolver.cs
  Models/
    YouTubeChannelSummary.cs
    YouTubeVideoSummary.cs
    YouTubePlaylistSummary.cs
    YouTubeLiveBroadcastSummary.cs
    YouTubeLiveChatMessage.cs
    YouTubeOutboundMessage.cs
    YouTubeQuotaResult.cs
  Services/
    YouTubeApiClientFactory.cs
    YouTubeChannelService.cs
    YouTubeVideoService.cs
    YouTubePlaylistService.cs
    YouTubeLiveBroadcastService.cs
    YouTubeLiveChatService.cs
    YouTubeLiveChatPoller.cs
    YouTubeChannelKeyResolver.cs
  PostOffice/
    YouTubeInboundSource.cs
    YouTubeOutboundSink.cs
  Diagnostics/
    YouTubeQuotaTracker.cs
    YouTubeApiHealthCheck.cs
Test/
  ...
docs/
  youtube-integration-pattern-plan.md
  configuration.md
  live-chat.md
  post-office-integration.md
```

## Configuration model

Create a single strongly typed configuration entry point with nested live options.

```csharp
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
```

Expected appsettings shape:

```json
{
  "YouTube": {
    "Enabled": true,
    "ApiKey": "...",
    "ClientId": "...",
    "ClientSecret": "...",
    "RefreshToken": "...",
    "ApplicationName": "PolyhydraGames.ChannelCheevos",
    "Live": {
      "Enabled": true,
      "OwnerUserId": "polyhydra-user-id",
      "ChannelId": "UC...",
      "BroadcastId": null,
      "PollInterval": "00:00:05",
      "AutoDiscoverActiveBroadcast": true,
      "EnableOutboundChat": true
    }
  }
}
```

## Dependency injection pattern

Add a single extension entry point with optional feature-specific methods.

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddYouTubeApi(
        this IServiceCollection services,
        IConfiguration configuration,
        bool respectEnabledFlag = true)
    {
        // bind options
        // register YouTubeService factory
        // register data API services
        return services;
    }

    public static IServiceCollection AddYouTubeLiveStreaming(
        this IServiceCollection services,
        IConfiguration configuration,
        bool respectEnabledFlag = true)
    {
        // call AddYouTubeApi
        // register live broadcast service
        // register live chat service
        // register poller
        // register inbound source and outbound sink
        return services;
    }
}
```

Recommended registrations:

```csharp
services.AddSingleton<IYouTubeApiClientFactory, YouTubeApiClientFactory>();
services.AddSingleton<IYouTubeChannelService, YouTubeChannelService>();
services.AddSingleton<IYouTubeVideoService, YouTubeVideoService>();
services.AddSingleton<IYouTubePlaylistService, YouTubePlaylistService>();
services.AddSingleton<IYouTubeLiveBroadcastService, YouTubeLiveBroadcastService>();
services.AddSingleton<IYouTubeLiveChatService, YouTubeLiveChatService>();
services.AddSingleton<IYouTubeChannelKeyResolver, YouTubeChannelKeyResolver>();
services.AddSingleton<YouTubeLiveChatPoller>();
services.AddSingleton<YouTubeInboundSource>();
services.AddSingleton<IOutboundSink, YouTubeOutboundSink>();
```

If the host uses `IHostedService` or the existing Polyhydra `IStart` pattern, add an adapter so live polling starts consistently with Twitch and Discord integrations.

## YouTube Data API services

### `IYouTubeChannelService`

Responsibilities:

- Get channel by ID.
- Get channel by handle/custom URL when possible.
- Get channel branding/title/thumbnail summary.
- Normalize YouTube channel IDs into reusable platform account IDs.

### `IYouTubeVideoService`

Responsibilities:

- Get video metadata.
- Get video status/privacy where permitted.
- Get current livestream video metadata when broadcast ID/video ID is known.

### `IYouTubePlaylistService`

Responsibilities:

- Get playlist metadata.
- List playlist items.
- Support uploads playlist discovery for a channel.

### `IYouTubeLiveBroadcastService`

Responsibilities:

- Discover active broadcast for configured channel.
- Resolve broadcast ID to video ID.
- Resolve broadcast to live chat ID.
- Expose stream state: upcoming, live, complete, unknown.

## YouTube Live Chat services

### `IYouTubeLiveChatService`

Responsibilities:

- Fetch live chat messages using `liveChatMessages.list`.
- Send live chat messages using `liveChatMessages.insert` when OAuth permits it.
- Track and pass `pageToken` between polling requests.
- Respect YouTube polling interval hints.
- Normalize YouTube chat items into `YouTubeLiveChatMessage`.

### `YouTubeLiveChatPoller`

Responsibilities:

- Start only when YouTube live integration is enabled.
- Discover active broadcast if no explicit broadcast ID is configured.
- Resolve live chat ID.
- Poll chat messages safely.
- Emit normalized chat messages.
- Back off on quota/rate errors.
- Recover when broadcasts end or change.
- Avoid duplicate message delivery by tracking message IDs.

Suggested behavior:

```text
Start
  -> Resolve configured channel
  -> Find active broadcast, unless BroadcastId provided
  -> Resolve liveChatId
  -> Poll liveChatMessages.list
  -> Normalize each new message
  -> Publish into YouTubeInboundSource
  -> Wait YouTube polling interval or configured minimum
  -> Repeat until stopped or broadcast ends
```

## Post Office integration

YouTube should follow the same high-level messaging model as Twitch and Discord.

### Inbound

`YouTubeInboundSource` should convert `YouTubeLiveChatMessage` into a shared inbound envelope.

Suggested mapping:

| YouTube value | Shared value |
|---|---|
| YouTube channel ID | `ChannelKey.PlatformAccountId` |
| Polyhydra owner user ID | `ChannelKey.OwnerUserId` |
| YouTube platform | `Platform.YouTube` |
| chat author channel ID | inbound sender platform ID |
| chat author display name | inbound viewer name |
| live chat message text | inbound text |
| live chat message ID | metadata `messageId` |
| live broadcast/video ID | metadata `broadcastId` / `videoId` |

Important behavior:

- Do not call outbound processors from inbound code.
- Inbound should emit or accept an inbound envelope only.
- Command handling should remain outside the YouTube package.
- YouTube package should be transport/platform glue, not command business logic.

### Outbound

`YouTubeOutboundSink` should implement the shared outbound sink abstraction.

Responsibilities:

- `CanHandle(ChannelKey channel)` returns true only for `Platform.YouTube`.
- Resolve the current live chat ID for the channel/broadcast.
- Send message text to live chat.
- Fail safely if no live broadcast/chat is active.
- Log quota/auth errors without crashing the host.

Avoid the Discord-style `_lastSocket` trap. Outbound messages should be routed from the target channel key and live chat resolver, not from the last inbound message.

## Platform enum requirement

The shared platform enum must include YouTube. If `Platform.YouTube` does not exist yet in `PolyhydraGames.Streaming.Interfaces.Enums`, add it there first. This package should not invent a parallel enum.

Expected shared values:

```csharp
public enum Platform
{
    Twitch,
    Discord,
    YouTube
}
```

## Authentication strategy

Support two authentication modes:

### API key mode

Use for public read-only operations:

- Channel lookup
- Video lookup
- Playlist lookup
- Public search
- Basic stream discovery when possible

### OAuth mode

Use for user/bot-authorized operations:

- Sending live chat messages
- Managing broadcast metadata later
- Reading private channel data later

The first milestone should support API key reads and OAuth outbound chat sends. Do not block read-only use on OAuth.

## Quota and reliability strategy

YouTube API quota can become the hidden cost center. Add explicit quota-aware patterns early.

Recommended components:

- `YouTubeQuotaTracker`
- request logging with operation names
- configurable polling interval
- respect YouTube `pollingIntervalMillis`
- exponential backoff for quota/rate errors
- duplicate message protection
- clear log events when stream discovery fails

Minimum log events:

- YouTube API disabled by config
- active broadcast not found
- live chat ID not found
- chat polling started
- chat polling stopped
- quota/rate limit hit
- outbound chat message failed
- OAuth credential missing for outbound send

## Testing plan

### Unit tests

Add tests for:

- options binding
- service registration with enabled/disabled flags
- channel key resolution
- live chat message normalization
- duplicate message suppression
- `YouTubeOutboundSink.CanHandle`
- outbound sink no-op behavior when no live chat is active
- API key vs OAuth credential selection

### Integration-style tests with fakes

Create fake service implementations for:

- `IYouTubeLiveBroadcastService`
- `IYouTubeLiveChatService`
- `IYouTubeChannelKeyResolver`

Use them to test:

- poller startup
- broadcast discovery
- live chat ID resolution
- message polling loop
- message emission to inbound source
- stop/cancellation behavior

### Manual smoke tests

Document a local smoke test:

1. Configure API key.
2. Configure OAuth refresh token for a test YouTube account.
3. Start an unlisted livestream.
4. Confirm active broadcast discovery.
5. Confirm incoming chat messages are logged/emitted.
6. Confirm outbound message send works.
7. End stream and confirm poller exits/backoff behavior.

## Documentation plan

Add or update:

- `docs/configuration.md`
- `docs/live-chat.md`
- `docs/post-office-integration.md`
- `docs/quota-and-reliability.md`
- README quick start

README should eventually include:

```csharp
services.AddYouTubeApi(Configuration);
services.AddYouTubeLiveStreaming(Configuration);
```

And a clear config example for both API key and OAuth modes.

## Implementation milestones

### Milestone 1: Stabilize package shape

- Verify project naming and casing.
- Confirm `.sln` builds on Linux and Windows.
- Add strongly typed options.
- Add DI extension methods.
- Add interfaces for data API services.
- Add basic README update.

Deliverable: package builds and exposes clean registration methods.

### Milestone 2: YouTube Data API wrapper

- Add client factory.
- Add channel/video/playlist services.
- Add models for summaries.
- Add tests with mocked/fake request layer.

Deliverable: ChannelCheevos can query YouTube channel/video/playlist metadata through this package.

### Milestone 3: Live broadcast discovery

- Add live broadcast service.
- Resolve active broadcast for configured channel.
- Resolve live chat ID.
- Add logs and tests.

Deliverable: package can detect the active stream and identify the live chat endpoint.

### Milestone 4: Live chat inbound

- Add live chat service.
- Add poller.
- Add message normalization.
- Add duplicate suppression.
- Add inbound source adapter.

Deliverable: YouTube live chat can enter the shared Polyhydra messaging pipeline.

### Milestone 5: Live chat outbound

- Add outbound send support with OAuth credentials.
- Add `YouTubeOutboundSink`.
- Add tests for platform routing and failure paths.

Deliverable: shared outbound dispatcher can send messages back to YouTube live chat.

### Milestone 6: ChannelCheevos integration

- Add package reference in ChannelCheevos host.
- Register YouTube API/live streaming services.
- Wire inbound YouTube chat to command handling.
- Wire outbound dispatcher to YouTube sink.
- Add dashboard/config visibility.

Deliverable: ChannelCheevos can use YouTube as a supported streaming/chat platform alongside Twitch/Discord.

## Acceptance criteria

The plan is complete when:

- `dotnet test APi.Youtube.sln` passes.
- YouTube API services can be registered independently from live streaming services.
- Live chat polling can be started and stopped cleanly.
- Incoming YouTube chat messages are converted to shared inbound envelopes.
- Outbound messages route through a YouTube outbound sink by `ChannelKey`.
- API key read mode works without OAuth.
- OAuth mode supports outbound chat send.
- README and docs explain configuration and host integration.

## Open questions

- Should this package be renamed from `APi` to `API`/`Api`, or preserved to avoid breaking project/package references?
- Does the shared `Platform` enum already include YouTube?
- Should YouTube live polling run as `IHostedService`, existing `IStart`, or both?
- Should stream discovery support multiple configured channels at once?
- Should outbound YouTube messages always target the active broadcast, or should callers specify broadcast/video/liveChat IDs explicitly?
- Where should OAuth refresh tokens live: existing OAuth.Core token storage, user secrets, or a ChannelCheevos database table?

## Recommended next card breakdown

1. `Youtube.API: normalize project/package naming and verify build`
2. `Youtube.API: add YouTubeApiOptions and DI registration`
3. `Youtube.API: add YouTube client factory`
4. `Youtube.API: add channel/video/playlist summary services`
5. `Youtube.API: add live broadcast discovery service`
6. `Youtube.API: add live chat polling service`
7. `Youtube.API: add Post Office inbound source`
8. `Youtube.API: add Post Office outbound sink`
9. `Youtube.API: add quota/backoff diagnostics`
10. `ChannelCheevos: wire YouTube live chat into messaging pipeline`
