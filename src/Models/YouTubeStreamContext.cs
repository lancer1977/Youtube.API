using PolyhydraGames.Platforms.Abstractions;

namespace PolyhydraGames.APi.Youtube.Models;

public sealed record YouTubeStreamContext(
    string? BroadcastId,
    string? LiveChatId,
    string? ChannelId,
    string? VideoId,
    ChannelKey? ChannelKey);
