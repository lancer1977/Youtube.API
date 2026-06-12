# Setup

YouTube integration library for Polyhydra Games hosts.

## What you need

- `YouTube:Enabled=true`
- For read-only API calls: `YouTube:ApiKey`
- For live chat and outbound chat: `YouTube:Live:Enabled=true`
- For live chat and outbound chat: `YouTube:ClientId`, `YouTube:ClientSecret`, and `YouTube:RefreshToken`
- For live chat capture routing: `YouTube:Live:OwnerUserId`
- For live chat discovery: `YouTube:Live:BroadcastId` or `YouTube:Live:ChannelId`

## Example

```json
{
  "YouTube": {
    "Enabled": true,
    "ApiKey": "your-api-key",
    "ClientId": "your-oauth-client-id",
    "ClientSecret": "your-oauth-client-secret",
    "RefreshToken": "your-refresh-token",
    "Live": {
      "Enabled": true,
      "OwnerUserId": "00000000-0000-0000-0000-000000000010",
      "ChannelId": "youtube-channel-id",
      "AutoDiscoverActiveBroadcast": true,
      "EnableOutboundChat": true
    }
  }
}
```

## Register

```csharp
using PolyhydraGames.APi.Youtube.Extensions;

services.AddYouTubeApi(configuration);
services.AddYouTubeLiveStreaming(configuration);
```

## Notes

- `AddYouTubeApi` is the cross-framework core API surface.
- `AddYouTubeLiveStreaming` is a separate live-chat surface and only activates on `net10.0` builds.
- `AddYouTubeLiveStreaming` registers the live chat gateway, inbound source, outbound sink, chat listener, and hosted service.
- `AddYouTubeApi` does not register live chat or outbound chat services.
- `Live.OwnerUserId` is required so inbound messages can be routed into the shared post-office channel key model.
- `Live.BroadcastId` bypasses discovery; `Live.ChannelId` plus `AutoDiscoverActiveBroadcast=true` discovers the active broadcast first.
- The hosted listener polls live chat, suppresses duplicate message IDs, and stops cleanly with the host.
- `Live.EnableOutboundChat=true` registers the outbound sink.
