using PolyhydraGames.Platforms.Abstractions;

namespace PolyhydraGames.APi.Youtube.Models;

public sealed record YouTubeChatMessageReceived(
    string MessageId,
    string? LiveChatId,
    string? BroadcastId,
    string? VideoId,
    ChannelKey Channel,
    string? PlatformRawViewerId,
    string? ViewerName,
    ChannelRole UserRole,
    string Text,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string> Meta);
