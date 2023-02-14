using JackHenry.TwitterScan.Service.Services;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using System.Net;

namespace JackHenry.TwitterScan.Service.Tests;

public partial class TweetReceiverTests
{
    [Fact]
    public async Task OpenStream_HappyPath()
    {
        var mockLogger = new Mock<ILogger<TweetReciever>>();

        (var mockHttpClientFactory, var mockHttpMessageHandler) = SetupMockHttp(new HttpResponseMessage
        { 
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(testStreamData))
        });

        (var mockTweetStatRepository, var addedTweets) = SetupTweetStatsRepo();

        // Intantiate Service
        var svc = new TweetReciever(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockTweetStatRepository.Object);

        // Conduct Test
        var token = new CancellationTokenSource().Token;
        await svc.OpenStream(token);

        VerifyTweetStatsRepo(mockTweetStatRepository, addedTweets);

        // Verify expected calls made to HttpClient
        mockHttpClientFactory.Verify(_ => _.CreateClient(It.IsAny<string>()));
        mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task OpenStream_BadUrl()
    {
        cfg.Url = "BAD URL";

        var mockLogger = new Mock<ILogger<TweetReciever>>();
        (var mockHttpClientFactory, _) = SetupMockHttp(new HttpResponseMessage()); 
        var mockTweetStatRepository = new Mock<ITweetStatRepository>();

        // Intantiate Service
        var svc = new TweetReciever(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockTweetStatRepository.Object);

        // Conduct Test
        var token = new CancellationTokenSource().Token;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.OpenStream(token));
        Assert.Contains("invalid request URI", exception.Message);
    }

    [Fact]
    public async Task OpenStream_401()
    {
        var mockLogger = new Mock<ILogger<TweetReciever>>();
        (var mockHttpClientFactory, _) = SetupMockHttp(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Unauthorized
        });
        var mockTweetStatRepository = new Mock<ITweetStatRepository>();

        // Intantiate Service
        var svc = new TweetReciever(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockTweetStatRepository.Object);

        // Conduct Test
        var token = new CancellationTokenSource().Token;
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => svc.OpenStream(token));
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact(Timeout=5000)]
    public async Task OpenStream_CancelToken()
    {
        var mockLogger = new Mock<ILogger<TweetReciever>>();
        (var mockHttpClientFactory, _) = SetupMockHttp(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StreamContent(new NeverEndingTweetStream())
        });
        var mockTweetStatRepository = new Mock<ITweetStatRepository>();

        // Intantiate Service
        var svc = new TweetReciever(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockTweetStatRepository.Object);

        // Conduct Test
        var cancelSource = new CancellationTokenSource();
        var token = cancelSource.Token;
        var opTask = svc.OpenStream(token);
        await Task.Delay(1000);
        cancelSource.Cancel();
        await Assert.ThrowsAsync<TaskCanceledException>(() => opTask);
    }

    [Fact]
    public async Task ProcessStream_LogThrottling()
    {
        Mock<ILogger<TweetReciever>> mockLogger = new Mock<ILogger<TweetReciever>>(MockBehavior.Strict);
        mockLogger.Setup(_ => _.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        Mock <IHttpClientFactory> mockHttpClientFactory = new Mock<IHttpClientFactory>();
        (var mockTweetStatRepository, var addedTweets) = SetupTweetStatsRepo();

        // Intantiate Service
        var svc = new TweetReciever(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockTweetStatRepository.Object);

        // Conduct Test
        var delayPerTweet = 10;

        // Define an Async Enumerable that has a delay between each yield
        async IAsyncEnumerable<TweetDataWrapper> SlowRollAsyncStream()
        {
            foreach (var item in testStreamData)
            {
                await Task.Delay(delayPerTweet);
                yield return item;
            }
        }

        await svc.ProcessStream(SlowRollAsyncStream());

        VerifyTweetStatsRepo(mockTweetStatRepository, addedTweets);

        // Establish some basic bounds for tweet logging
        //    somewhere between the stream rate
        //    and one log entry per stream item
        var tweetsPerSecond = 1000 / delayPerTweet;
        var seconds = testStreamData.Length / tweetsPerSecond;
        var expectedLogEntriesMin = seconds / cfg.logFrequencySeconds;
        var expectedLogEntriesMax = testStreamData.Length;
        mockLogger.Verify(_ => _.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Between(expectedLogEntriesMin, expectedLogEntriesMax, Moq.Range.Inclusive));
    }

    TwitterReceiverConfiguration cfg = new TwitterReceiverConfiguration
    {
        logFrequencySeconds = 1,
        Url = "http://TestServer.com/NotCalled",
        AccessToken = "The Access Token"
    };

    // Make test stream data with each tweet having a unique hashtag for equality checking
    TweetDataWrapper[] testStreamData = Enumerable
            .Range(0, 500)
            .Select(_ => new TweetDataWrapper(Guid.NewGuid().ToString()))
            .ToArray();

    (Mock<ITweetStatRepository>, List<Tweet>) SetupTweetStatsRepo()
    {
        var mockTweetStatRepository = new Mock<ITweetStatRepository>(MockBehavior.Strict);
        var addedTweets = new List<Tweet>();
        mockTweetStatRepository.Setup(_ => _.Start()).Verifiable();
        mockTweetStatRepository.Setup(_ => _.AddTweet(Capture.In(addedTweets))).Verifiable();
        return (mockTweetStatRepository, addedTweets);
    }

    void VerifyTweetStatsRepo(Mock<ITweetStatRepository> mockTweetStatRepository, List<Tweet> addedTweets)
    {
        Assert.Equal(testStreamData.Length, addedTweets.Count);
        for (int i = 0; i < testStreamData.Length; i++)
            Assert.Equal(testStreamData[i].Data.Entities.Hashtags[0].Tag, addedTweets[i].Entities.Hashtags[0].Tag);
        mockTweetStatRepository.Verify(_ => _.Start(), Times.Once());
        mockTweetStatRepository.Verify(_ => _.AddTweet(It.IsAny<Tweet>()), Times.Exactly(testStreamData.Length));
    }

    static (Mock<IHttpClientFactory>, Mock<HttpMessageHandler>) SetupMockHttp(HttpResponseMessage httpResponseMessage)
    {
        var mockHttpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(mockHttpMessageHandler.Object))
            .Verifiable();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage)
            .Verifiable();
        return (mockHttpClientFactory, mockHttpMessageHandler);
    }
}