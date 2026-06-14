using Google.Apis.YouTube.v3.Data;

namespace PolyhydraGames.APi.Youtube.Models;

public sealed record YouTubeLiveChatAuthor(
    string? ChannelId,
    string? DisplayName,
    bool IsChatOwner = false,
    bool IsChatModerator = false,
    bool IsChatSponsor = false,
    bool IsVerified = false);

public sealed record YouTubeLiveChatMessage(
    string MessageId,
    string? LiveChatId,
    string? BroadcastId,
    string? VideoId,
    string Text,
    DateTimeOffset Timestamp,
    YouTubeLiveChatAuthor? Author,
    LiveChatMessage RawPayload);
