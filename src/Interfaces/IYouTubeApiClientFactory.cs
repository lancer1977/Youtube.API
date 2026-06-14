using Google.Apis.YouTube.v3;

namespace PolyhydraGames.APi.Youtube.Interfaces;

public interface IYouTubeApiClientFactory
{
    YouTubeService CreatePublicReadService();

    YouTubeService CreateAuthorizedService(string streamerKey);
}
