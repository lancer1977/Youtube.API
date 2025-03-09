namespace APi.Youtube
{
    public record YoutubeConfig(string ApiKey, string ApplicationName) : IYoutubeConfig;
}