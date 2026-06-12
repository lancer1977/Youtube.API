namespace PolyhydraGames.APi.Youtube.Interfaces;

public interface IYouTubeChatListener
{
    bool IsRunning { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
