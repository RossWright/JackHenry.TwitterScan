using Microsoft.Extensions.Logging;

namespace JackHenry.TwitterScan.Service.Tests;

public class TweetHashtagProcessorTests
{
    [Fact]
    public void HappyPath()
    {
        var mockLogger = new Mock<ILogger<TweetHashtagProcessor>>();
        var tweetHashtagProcessor = new TweetHashtagProcessor(mockLogger.Object);
        
        string[] hashtags = Enumerable.Range(0, 100)
            .Select(i => $"hashtagindex{i}")
            .ToArray();

        DateTime start = DateTime.UtcNow;
        tweetHashtagProcessor.Start();

        // Add Tweets such that the number of tweets with each hashtag is equal to it's index
        for (var i = 0; i < hashtags.Length; i++)
        {
            tweetHashtagProcessor.AddTweet(new Tweet(
                Enumerable.Range(i, hashtags.Length - i)
                    .Select(j => hashtags[j])
                    .ToArray()));
        }

        var actualElapsed = (DateTime.UtcNow - start).TotalSeconds;
        var stats = tweetHashtagProcessor.GetTweetStats();
                    
        Assert.Equal(hashtags.Length, stats.Count);

        Assert.InRange(actualElapsed, 
            stats.ElapsedSeconds-0.5, 
            stats.ElapsedSeconds + 0.5);

        // Verify the Top Ten Hashtags are right
        for (var i = 0; i < stats.TopTenHashtags.Length; i++)
        {
            Assert.Equal(hashtags[hashtags.Length - i - 1], stats.TopTenHashtags[i].Tag);
            Assert.Equal(hashtags.Length - i, stats.TopTenHashtags[i].Count);
        }
    }

    [Fact]
    public void CheckHashtagCount()
    {
        var mockLogger = new Mock<ILogger<TweetHashtagProcessor>>();
        var tweetHashtagProcessor = new TweetHashtagProcessor(mockLogger.Object);

        string[] hashtags = Enumerable.Range(0, 10)
            .Select(i => $"hashtagindex{i}")
            .ToArray();
        var rand = new Random();
        int tagCount = 0;
        for (var i = 0; i < 100; i++)
        {
            var tags = Enumerable.Range(0, rand.Next(10))
                    .Select(j => hashtags[j])
                    .ToArray();
            tagCount += tags.Length;
            tweetHashtagProcessor.AddTweet(new Tweet(tags));
        }
        var stats = tweetHashtagProcessor.GetTweetStats();
        Assert.Equal(tagCount, stats.TopTenHashtags.Sum(_ => _.Count));
    }


    [Fact]
    public async Task Multithread()
    {
        var mockLogger = new Mock<ILogger<TweetHashtagProcessor>>();
        var tweetHashtagProcessor = new TweetHashtagProcessor(mockLogger.Object);

        bool stop = false;

        string[] hashtags = Enumerable.Range(0, 10)
            .Select(i => $"hashtagindex{i}")
            .ToArray();

        var rand = new Random();
        int addedTweats = 0;
        int tagCount = 0;
        _ = Task.Run(() =>
        {
            while (!stop)            
            {
                var tags = Enumerable.Range(0, rand.Next(10))
                        .Select(j => hashtags[j])
                        .ToArray();
                tagCount += tags.Length;
                tweetHashtagProcessor.AddTweet(new Tweet(tags));
                addedTweats++;
            }
        });

        TweetHashtagStatistics stats = null!;
        _ = Task.Run(() =>
        {
            while (!stop)
            {
                stats = tweetHashtagProcessor.GetTweetStats();
            }
        });

        await Task.Delay(5000);
        stop = true;

        stats = tweetHashtagProcessor.GetTweetStats();

        Assert.Equal(stats.Count, addedTweats);
        Assert.Equal(tagCount, stats.TopTenHashtags.Sum(_ => _.Count));
    }

    [Fact]
    public void AddTweetsWithSameHashtags()
    {
        var mockLogger = new Mock<ILogger<TweetHashtagProcessor>>();
        var tweetHashtagProcessor = new TweetHashtagProcessor(mockLogger.Object);

        for (var i = 0; i < 100; i++)
            tweetHashtagProcessor.AddTweet(new Tweet("thehashtag"));
        var stats = tweetHashtagProcessor.GetTweetStats();
        Assert.Equal(100, stats.Count);
        Assert.Single(stats.TopTenHashtags);
        Assert.Equal("thehashtag", stats.TopTenHashtags[0].Tag);
    }

    [Fact]
    public void AddNullTweet()
    {
        var mockLogger = new Mock<ILogger<TweetHashtagProcessor>>();
        var tweetHashtagProcessor = new TweetHashtagProcessor(mockLogger.Object);

        tweetHashtagProcessor.AddTweet(null!);
        var stats = tweetHashtagProcessor.GetTweetStats();
        Assert.Equal(0, stats.Count);
        Assert.Empty(stats.TopTenHashtags);

        mockLogger.Verify(_ => _.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void AddTweetWithoutHashtags()
    {
        var mockLogger = new Mock<ILogger<TweetHashtagProcessor>>();
        var tweetHashtagProcessor = new TweetHashtagProcessor(mockLogger.Object);

        tweetHashtagProcessor.AddTweet(new Tweet
        { 
            Entities = new TweetEntities 
            {
                Hashtags = new TweetTagEntity[0] 
            } 
        });
        var stats = tweetHashtagProcessor.GetTweetStats();
        Assert.Equal(1, stats.Count);
        Assert.Empty(stats.TopTenHashtags);
    }
}
