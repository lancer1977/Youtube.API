using PolyhydraGames.APi.Youtube.Models;

namespace PolyhydraGames.APi.Youtube.Interfaces;

public interface IYouTubeLiveClient
{
    IObservable<YouTubeChatMessageReceived> OnMessageReceived { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
}
