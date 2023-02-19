using Microsoft.Extensions.Logging;

namespace JackHenry.TwitterScan.Service.Tests;

public class TweetMetricsProcessorTests
{
    [Fact]
    public void HappyPath()
    {
        var mockLogger = new Mock<ILogger<TweetMetricsProcessor>>();
        var tweetMetricsProcessor = new TweetMetricsProcessor(mockLogger.Object);
        
        DateTime start = DateTime.UtcNow;
        tweetMetricsProcessor.Start();

        for (var i = 0; i < 500; i++)
        {
            tweetMetricsProcessor.AddTweet(new Tweet
            {
                NonPublicMetrics = new TweetNonPublicMetrics
                {
                    ImpressionCount = 10 
                },
                PublicMetrics = new TweetPublicMetrics
                {
                    LikeCount = 5,
                    RetweetClicks = 2,
                    QuoteCount = i % 2
                }
            });
        }

        var actualElapsed = (DateTime.UtcNow - start).TotalSeconds;
        var stats = tweetMetricsProcessor.GetMetricsStats();
                    
        Assert.Equal(500 * 10, (int)stats.ImpressionCount);
        Assert.Equal(500 * 5, (int)stats.LikeCount);
        Assert.Equal(500 * 2, (int)stats.RetweetCount);
        Assert.Equal(250, (int)stats.QuoteCount);
    }

    [Fact]
    public void VerifyRequireFields()
    {
        var mockLogger = new Mock<ILogger<TweetMetricsProcessor>>();
        var tweetCountProcessor = new TweetMetricsProcessor(mockLogger.Object);
        Assert.NotNull(tweetCountProcessor.RequiredFields!);
        Assert.Contains("public_metrics", tweetCountProcessor.RequiredFields!);
        Assert.Contains("non_public_metrics", tweetCountProcessor.RequiredFields!);
    }

    [Fact]
    public async Task Multithread()
    {
        var mockLogger = new Mock<ILogger<TweetMetricsProcessor>>();
        var tweetMetricsProcessor = new TweetMetricsProcessor(mockLogger.Object);

        long stopFlag = 0;

        int addedTweats = 0;
        var feedTask = Task.Run(() =>
        {
            while (Interlocked.Read(ref stopFlag) == 0)
            {
                tweetMetricsProcessor.AddTweet(new Tweet
                {
                    NonPublicMetrics = new TweetNonPublicMetrics
                    {
                        ImpressionCount = 10
                    },
                    PublicMetrics = new TweetPublicMetrics
                    {
                        LikeCount = 5,
                        RetweetClicks = 2,
                        QuoteCount = 1
                    }
                });
                addedTweats++;
            }
        });

        TweetMetricsStatistics stats = null!;
        var readTask = Task.Run(() =>
        {
            while (Interlocked.Read(ref stopFlag) == 0)
            {
                stats = tweetMetricsProcessor.GetMetricsStats();
            }
        });

        await Task.Delay(1000);
        Interlocked.Increment(ref stopFlag);
        await feedTask;
        await readTask;

        stats = tweetMetricsProcessor.GetMetricsStats();

        Assert.Equal(addedTweats * 10, (int)stats.ImpressionCount);
        Assert.Equal(addedTweats * 5, (int)stats.LikeCount);
        Assert.Equal(addedTweats * 2, (int)stats.RetweetCount);
        Assert.Equal(addedTweats * 1, (int)stats.QuoteCount);
    }

    [Fact]
    public void AddNullTweet()
    {
        var mockLogger = new Mock<ILogger<TweetMetricsProcessor>>();
        var tweetMetricsProcessor = new TweetMetricsProcessor(mockLogger.Object);

        tweetMetricsProcessor.AddTweet(null!);
        var stats = tweetMetricsProcessor.GetMetricsStats();
        Assert.Equal(0, stats.ImpressionCount);
        Assert.Equal(0, stats.LikeCount);
        Assert.Equal(0, stats.RetweetCount);
        Assert.Equal(0, stats.QuoteCount);

        mockLogger.Verify(_ => _.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
