using PolyhydraGames.APi.Youtube.Interfaces;

namespace PolyhydraGames.APi.Youtube;

public record YoutubeConfig(
    string ApiKey,
    string ApplicationName,
    string? ClientId = null,
    string? ClientSecret = null,
    string? RefreshToken = null) : IYoutubeConfig
{
    public bool HasOAuthCredentials =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        !string.IsNullOrWhiteSpace(RefreshToken);
}