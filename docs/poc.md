# YouTube POC

This POC validates live chat capture and outbound chat for YouTube.

## What it proves

- `AddYouTubeApi` and `AddYouTubeLiveStreaming` register correctly.
- The hosted listener polls live chat.
- Inbound chat reaches `YouTubeInboundSource`.
- Outbound messages route through `IOutboundSink` when the target channel is YouTube.

## What you need

- `YouTube:Enabled=true`
- `YouTube:ClientId`
- `YouTube:ClientSecret`
- `YouTube:RefreshToken`
- `YouTube:Live:Enabled=true`
- `YouTube:Live:OwnerUserId`
- `YouTube:Live:ChannelId` or `YouTube:Live:BroadcastId`

## Minimal host

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PolyhydraGames.APi.Youtube.Extensions;
using PolyhydraGames.APi.Youtube.Interfaces;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddYouTubeLiveStreaming(builder.Configuration);

using var host = builder.Build();
var listener = host.Services.GetRequiredService<IYouTubeChatListener>();

await listener.StartAsync();
Console.WriteLine("YouTube chat listener running. Press Enter to stop.");
Console.ReadLine();
await listener.StopAsync();
```

## Expected result

- The listener starts without throwing.
- The active broadcast resolves to a live chat ID.
- Messages are published into the shared inbound bus with `Platform.Youtube`.
- A YouTube outbound envelope with `liveChatId` metadata is sent to the live chat.

## Notes

- `YouTube:Live:AutoDiscoverActiveBroadcast=true` discovers the current broadcast from the configured channel.
- `YouTube:Live:EnableOutboundChat=true` keeps outbound chat enabled.
- The package uses OAuth for live chat. An API key alone is not enough.
