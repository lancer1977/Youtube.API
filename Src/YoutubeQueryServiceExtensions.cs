using Google.Apis.YouTube.v3.Data;

namespace PolyhydraGames.APi.Youtube;

public static class YoutubeQueryServiceExtensions
{
    public static int ToSeconds(this string duration)
    {
        var time = System.Xml.XmlConvert.ToTimeSpan(duration);
        return (int)time.TotalSeconds;
    }

    public static int ToMinutes(this string duration)
    {
        var time = System.Xml.XmlConvert.ToTimeSpan(duration);
        return (int)time.TotalSeconds/60;
    }


    public static void ToConsole(this Video video)
    {
        Console.WriteLine($"Title: {video.Snippet.Title}");
        Console.WriteLine($"Description: {video.Snippet.Description}");
        Console.WriteLine($"Channel: {video.Snippet.ChannelTitle}");
        Console.WriteLine($"Published Date: {video.Snippet.PublishedAtDateTimeOffset}");
        Console.WriteLine($"Duration: {video.ContentDetails.Duration}");
        Console.WriteLine($"Views: {video.Statistics.ViewCount}");
        Console.WriteLine($"Likes: {video.Statistics.LikeCount}");
        Console.WriteLine($"Comments: {video.Statistics.CommentCount}");
        Console.WriteLine($"Tags: {video.ContentDetails.Duration}");
    }
}