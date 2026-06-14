# Api.Youtube

YouTube API integration for Polyhydra Games.

## Contents

- [Docs home](./)
- [Setup](./setup.md)
- [POC](./poc.md)
- [Feature Index](./features/README.md)
- [Roadmap Index](./roadmaps/README.md)
- [api.youtube](./api-youtube.md)
- [decisions](./decisions/0000-template.md)

## Streaming architecture

`Query -> Stream context + chat service -> Poller -> Inbound source -> Outbound sink`

- `YouTubeQueryService` handles read API queries (cancellable, null-safe, options validated).
- `YouTubeAuthStateStore` keeps streamer-specific OAuth refresh tokens isolated from shared app credentials.
- `GoogleYouTubeLiveChatGateway` resolves and fetches live chat resources.
- `YouTubeLiveBroadcastResolver` and `YouTubeStreamContextResolver` normalize stream context.
- `YouTubeLiveChatPoller` runs as a hosted background service, persists checkpoint state, and emits `YouTubeLiveChatMessage` envelopes.
- `IYouTubeLiveClient` provides the direct connect/send/observe facade.
- `YouTubeOutboundSink` sends outbound messages with explicit/live-channel fallback context.
- `IYouTubeLiveChatCheckpointStore` keeps the live-chat resume seam isolated from the host.

## Decisions

- [Template](./decisions/0000-template.md)
