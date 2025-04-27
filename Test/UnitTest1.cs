using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PolyhydraGames.APi.Youtube;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.Core.Test;

namespace Test;
[TestFixture]
public class ApiTests
{
    public IYoutubeQuery Query { get; set; }

    [SetUp]
    public void Setup()
    {


        var config = new ConfigurationBuilder()
            //.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            //.AddUserSecrets("977cb758-5c0c-46e9-97c4-9a167a8d117a")
            .AddUserSecrets("7f11cf3e-22bf-4d7a-b1ec-d1df49d5ee00") // Use the UserSecretsId generated earlier
            .Build();


        var host = TestHelpers.GetHost((_, services) =>
        {
            services.AddSingleton<IConfiguration>(config);
            //services.AddSingleton(_ => httpMock.Object);
            services.AddSingleton<IYoutubeQuery, YoutubeQueryService>();
            services.AddSingleton<IYoutubeConfig>(x => config.GetSection("Youtube").Get<YoutubeConfig>()!);
            //services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("redis.polyhydragames.com"));
            //services.AddSingleton<IIMVDBAuthorization, IMVDBAuthorization>()
            ;
        });

        Query = host.Services.GetService<IYoutubeQuery>();
    }

    [TestCase("@DreadBreadcrumb-w4q")]
    public async Task GetUser(string username)
    {
        var result = await Query.GetUserID(username);
        Console.WriteLine("ChannelID:" + result);
        Assert.That(result != null);
    }

    [TestCase("@DreadBreadcrumb-w4q", "Bomberman")]
    public async Task GetUserVideos(string username, string query)
    {
        var result = await Query.GetUserVideos(username, query);
        Assert.That(result.Count, Is.GreaterThan(0));
    }


    [TestCase("DreadBreadcrumb-w4q")]
    public async Task GetUserPlaylists(string username)
    {
        var result = await Query.GetUserPlaylists(username);
        Assert.That(result.Count, Is.GreaterThan(0));
    }

    [TestCase("fQqOWUk6INc", ExpectedResult = 88)]
    public async Task<int> GetVideoDetailsMinutes(string videoId)
    {
        var result = await Query.GetVideoDetails(videoId);
        Console.WriteLine(result.ContentDetails.Duration);
        return result.ContentDetails.Duration.ToMinutes();
    }

    //[TestCase("segafan001","Dungeon Explorer","tg16", ExpectedResult = 2)]
    //public async Task<int> GetVideosOfGameCount(string username,string gameName, string system)
    //{
    //    var result = await Query.GetVideosOfGameCount(gameName,system);
    //    return 0;
    //}

}

public class ExtensionTests
{
    //ToSeconds
    [TestCase("PT1H1M1S", ExpectedResult = 3661)]
    public int ToSeconds(string duration)
    {
        return duration.ToSeconds();
    }

    [TestCase("PT1H1M1S", ExpectedResult = 61)]
    public int ToMinutes(string duration)
    {
        return duration.ToMinutes();
    }
}