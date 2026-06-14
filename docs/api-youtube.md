# Api.Youtube

**Location:** `~/code/APIs/Api.Youtube`

**Purpose:** YouTube Data API and live chat integration.

**Assembly:** `PolyhydraGames.APi.Youtube`

## Architecture (query + streaming)

```
IYoutubeQuery (Read) -> YouTubeQueryService
Live options -> AddYouTubeApi / AddYouTubeLiveStreaming
    -> YouTubeApiClientFactory -> YouTubeAuthStateStore -> GoogleYouTubeLiveChatGateway
    -> YouTubeLiveBroadcastResolver -> YouTubeStreamContextResolver
    -> YouTubeLiveChatPoller (BackgroundService)
    -> YouTubeInboundSource -> Messaging bus
    -> IYouTubeLiveClient
    -> YouTubeOutboundSink
```

## Config examples

Has config (`hasConfig: true`). Expect:
- API key for read-only calls
- OAuth client credentials and refresh token for live chat / outbound chat
- live channel id or broadcast id for chat discovery

### API key only
```json
{
  "YouTube": {
    "Enabled": true,
    "ApiKey": "<read-only-key>",
    "ApplicationName": "MyApp"
  }
}
```

### Live streaming (OAuth + broadcast auto-discovery)
```json
{
  "YouTube": {
    "Enabled": true,
    "ApiKey": "<api-key>",
    "ClientId": "<oauth-client-id>",
    "ClientSecret": "<oauth-client-secret>",
    "RefreshToken": "<oauth-refresh-token>",
    "ApplicationName": "MyApp",
    "Live": {
      "Enabled": true,
      "AutoDiscoverActiveBroadcast": true,
      "ChannelId": "<youtube-channel-id>",
      "PollInterval": "00:00:06",
      "EnableOutboundChat": true
    }
  }
}
```

## Lifecycle and failure behavior

- `AddYouTubeApi` always registers validated read APIs and options.
- `AddYouTubeLiveStreaming` registers live-plane services, the direct live client facade, and starts a hosted poller.
- API key mode is for public metadata reads only.
- OAuth live-chat behavior resolves a streamer-specific auth-state entry instead of falling back to a shared API key.
- Missing or rotating broadcast or chat ids are treated as non-fatal transient states.
- Poller uses checkpoint-backed resume state, exponential backoff on quota or rate errors, and deduplicates by message id.
- Expired live-chat ids (`404`, `410`, or equivalent API reasons) trigger a context rebind instead of retrying the stale chat id forever.
- Outbound send is safe on missing IDs and logs structured warnings or errors without crashing the host.

## Failure matrix

| Condition | Poller behavior | Outbound behavior |
|---|---|---|
| No active broadcast | Retry with backoff and keep resolving stream context | No-op with warning |
| Chat expired or rotated | Rebind context, reset page token, preserve dedupe window only when the live chat id stays the same | Re-resolve live chat id on demand |
| Rate limit or quota error | Exponential backoff | Log and return |
| Auth missing | Validation fails before host starts live services | Validation fails before send path is used |
| Live chat id missing | Retry as a bounded transient state | No-op with warning |

## Checkpointing

- Live polling stores `pageToken`, phase, and the recent message-id window in `YouTubeLiveChatPollerState`.
- `IYouTubeLiveChatCheckpointStore` is the persistence seam for external storage.
- The default store is in-memory with TTL cleanup so current runtime behavior stays safe without adding IO requirements.
- `YouTubeLiveOptions.StateWindowSize` controls how many seen message ids are kept for duplicate suppression after restart.

## Related

- [Setup](./setup.md)
- [Roadmap v1](./roadmap/v1/README.md)

## Status

✅ **Working** (small library + tests)
