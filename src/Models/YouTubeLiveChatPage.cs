using Google.Apis.YouTube.v3.Data;

namespace PolyhydraGames.APi.Youtube.Models;

public sealed record YouTubeLiveChatPage(
    IReadOnlyList<LiveChatMessage> Messages,
    string? NextPageToken,
    TimeSpan? PollingInterval);
