namespace PolyhydraGames.APi.Youtube.Interfaces;

public interface IYoutubeConfig
{
    string ApiKey { get; }
    string ApplicationName { get; }
    string? ClientId { get; }
    string? ClientSecret { get; }
    string? RefreshToken { get; }
    bool HasOAuthCredentials { get; }
}