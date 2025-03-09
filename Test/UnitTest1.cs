using APi.Youtube;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.Design;
using Microsoft.Extensions.DependencyInjection;
using PolyhydraGames.Core.Test;

namespace Test
{
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
                services.AddSingleton<IYoutubeConfig>(x=> config.GetSection("Youtube").Get<YoutubeConfig>()!);
                //services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("redis.polyhydragames.com"));
                //services.AddSingleton<IIMVDBAuthorization, IMVDBAuthorization>()
                ;
            });

            Query = host.Services.GetService<IYoutubeQuery>();
        }

        [TestCase("segafan001")]
        public async Task GetUserVideos(string username)
        {
            var result = await Query.GetUserVideos(username);
            Assert.That(result.Count, Is.GreaterThan(0)); 
        }


        [TestCase("segafan001")]
        public async Task GetUserPlaylists(string username)
        {
            var result = await Query.GetUserPlaylists(username);
            Assert.That(result.Count, Is.GreaterThan(0));
        }

        //[TestCase("segafan001","Dungeon Explorer","tg16", ExpectedResult = 2)]
        //public async Task<int> GetVideosOfGameCount(string username,string gameName, string system)
        //{
        //    var result = await Query.GetVideosOfGameCount(gameName,system);
        //    return 0;
        //}

    }
}
