using System.Collections.Generic;

namespace PolyhydraGames.APi.Youtube.Models;

public sealed record YouTubeLiveChatMessageBatch(
    IReadOnlyList<YouTubeLiveChatMessage> Messages,
    string? NextPageToken,
    TimeSpan PollingInterval);
