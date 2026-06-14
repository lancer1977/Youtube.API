using PolyhydraGames.APi.Youtube.Models;

namespace PolyhydraGames.APi.Youtube.Interfaces;

public interface IYouTubeStreamContextResolver
{
    Task<YouTubeStreamContext?> ResolveAsync(CancellationToken ct = default);
}
