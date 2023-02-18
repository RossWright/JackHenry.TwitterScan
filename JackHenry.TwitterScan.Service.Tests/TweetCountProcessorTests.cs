using Microsoft.Extensions.Logging;

namespace JackHenry.TwitterScan.Service.Tests;

public class TweetCountProcessorTests
{
    [Fact]
    public void HappyPath()
    {
        var mockLogger = new Mock<ILogger<TweetCountProcessor>>();
        var tweetCountProcessor = new TweetCountProcessor(mockLogger.Object);
        
        DateTime start = DateTime.UtcNow;
        tweetCountProcessor.Start();

        for (var i = 0; i < 500; i++)
        {
            tweetCountProcessor.AddTweet(new Tweet());
        }

        var actualElapsed = (DateTime.UtcNow - start).TotalSeconds;
        var stats = tweetCountProcessor.GetCountStats();
                    
        Assert.Equal(500, (int)stats.Count);

        Assert.InRange(actualElapsed, 
            stats.ElapsedSeconds - 0.5, 
            stats.ElapsedSeconds + 0.5);
    }

    [Fact]
    public async Task Multithread()
    {
        var mockLogger = new Mock<ILogger<TweetCountProcessor>>();
        var tweetCountProcessor = new TweetCountProcessor(mockLogger.Object);

        long stopFlag = 0;

        int addedTweats = 0;
        var feedTask = Task.Run(() =>
        {
            while (Interlocked.Read(ref stopFlag) == 0)
            {
                tweetCountProcessor.AddTweet(new Tweet());
                addedTweats++;
            }
        });

        TweetCountStatistics stats = null!;
        var readTask = Task.Run(() =>
        {
            while (Interlocked.Read(ref stopFlag) == 0)
            {
                stats = tweetCountProcessor.GetCountStats();
            }
        });

        await Task.Delay(1000);
        Interlocked.Increment(ref stopFlag);
        await feedTask;
        await readTask;

        stats = tweetCountProcessor.GetCountStats();

        Assert.Equal(stats.Count, addedTweats);
    }

    [Fact]
    public void AddNullTweet()
    {
        var mockLogger = new Mock<ILogger<TweetCountProcessor>>();
        var tweetCountProcessor = new TweetCountProcessor(mockLogger.Object);

        tweetCountProcessor.AddTweet(null!);
        var stats = tweetCountProcessor.GetCountStats();
        Assert.Equal(0, stats.Count);

        mockLogger.Verify(_ => _.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
