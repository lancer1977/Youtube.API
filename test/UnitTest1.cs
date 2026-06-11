using Google.Apis.YouTube.v3.Data;
using PolyhydraGames.APi.Youtube;
using PolyhydraGames.APi.Youtube.Interfaces;
using PolyhydraGames.Core.Test;

namespace Test;

[TestFixture]
public class ApiTests
{
    [TestCase("PT1H1M1S", ExpectedResult = 3661)]
    public int ToSeconds(string duration) => duration.ToSeconds();

    [TestCase("PT1H1M1S", ExpectedResult = 61)]
    public int ToMinutes(string duration) => duration.ToMinutes();

    [Test]
    public async Task AsUserQuery_DelegatesToUnderlyingQuery()
    {
        var query = new FakeYoutubeQuery();
        var userQuery = query.AsUserQuery("@DreadBreadcrumb-w4q");

        var videos = await userQuery.GetVideos();
        var playlists = await userQuery.GetUserPlaylists();
        var count = await userQuery.GetVideosOfGameCount("Bomberman", "tg16");

        Assert.That(query.LastUsername, Is.EqualTo("@DreadBreadcrumb-w4q"));
        Assert.That(videos, Has.Count.EqualTo(1));
        Assert.That(playlists, Has.Count.EqualTo(1));
        Assert.That(count, Is.EqualTo(7));
        Assert.That(query.LastQuery, Is.EqualTo("Bomberman:tg16"));
    }

    private sealed class FakeYoutubeQuery : IYoutubeQuery
    {
        public string? LastUsername { get; private set; }
        public string LastQuery { get; private set; } = string.Empty;

        public Task<IList<SearchResult>> GetUserVideos(string username, string query = "")
        {
            LastUsername = username;
            LastQuery = query;
            return Task.FromResult<IList<SearchResult>>(new List<SearchResult>
            {
                new()
                {
                    Id = new ResourceId { VideoId = "video-1" },
                    Snippet = new SearchResultSnippet { Title = "Sample video" }
                }
            });
        }

        public Task<IList<PlaylistSnippet>> GetUserPlaylists(string username)
        {
            LastUsername = username;
            return Task.FromResult<IList<PlaylistSnippet>>(new List<PlaylistSnippet>
            {
                new() { Title = "Sample playlist" }
            });
        }

        public Task<Video?> GetVideoDetails(string videoId)
            => Task.FromResult<Video?>(new Video());

        public Task<string?> GetUserID(string username)
        {
            LastUsername = username;
            return Task.FromResult<string?>("channel-123");
        }

        public Task<int> GetVideosOfGameCount(string username, string gameName, string system)
        {
            LastUsername = username;
            LastQuery = $"{gameName}:{system}";
            return Task.FromResult(7);
        }
    }
}

public class ExtensionTests
{
    [TestCase("PT1H1M1S", ExpectedResult = 3661)]
    public int ToSeconds(string duration) => duration.ToSeconds();

    [TestCase("PT1H1M1S", ExpectedResult = 61)]
    public int ToMinutes(string duration) => duration.ToMinutes();
}
