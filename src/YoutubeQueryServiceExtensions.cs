using System.Globalization;
using System.Xml;
using Google.Apis.YouTube.v3.Data;
using PolyhydraGames.APi.Youtube.Interfaces;

namespace PolyhydraGames.APi.Youtube;

public static class YoutubeQueryServiceExtensions
{
    public static IYoutubeUserQuery AsUserQuery(this IYoutubeQuery query, string username)
        => new YoutubeQueryUserService(query, username);

    public static int ToSeconds(this string duration)
    {
        var time = XmlConvert.ToTimeSpan(duration);
        return (int)time.TotalSeconds;
    }

    public static int ToMinutes(this string duration)
    {
        var time = XmlConvert.ToTimeSpan(duration);
        return (int)time.TotalSeconds / 60;
    }
}
