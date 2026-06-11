using PolyhydraGames.APi.Youtube.Interfaces;

namespace PolyhydraGames.APi.Youtube;

public record YoutubeConfig(string ApiKey, string ApplicationName) : IYoutubeConfig;