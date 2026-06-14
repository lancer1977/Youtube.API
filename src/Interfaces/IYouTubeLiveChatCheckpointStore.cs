using PolyhydraGames.APi.Youtube.Models;
using PolyhydraGames.Platforms.Abstractions;

namespace PolyhydraGames.APi.Youtube.Interfaces;

public interface IYouTubeLiveChatCheckpointStore
{
    Task<YouTubeLiveChatPollerState?> LoadAsync(ChannelKey channelKey, CancellationToken cancellationToken = default);

    Task SaveAsync(
        ChannelKey channelKey,
        YouTubeLiveChatPollerState state,
        CancellationToken cancellationToken = default);
}
