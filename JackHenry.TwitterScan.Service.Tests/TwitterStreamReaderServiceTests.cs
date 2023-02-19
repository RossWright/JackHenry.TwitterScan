using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using System.Net;
using System.Net.Http;

namespace JackHenry.TwitterScan.Service.Tests;

public class TwitterStreamReaderServiceTests
{
    [Fact(Timeout = 5000)]
    public void OpenStream_HappyPath()
    {
        var mockLogger = new Mock<ILogger<TwitterStreamReaderService>>();

        SetupMockHttp(new HttpResponseMessage 
        { 
            StatusCode = HttpStatusCode.OK, 
            Content = new StringContent(JsonSerializer.Serialize(testStreamData)) 
        });

        SetupMockTweetProcessor();

        // Intantiate Service
        var svc = new TwitterStreamReaderService(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockServiceProvider.Object);

        // Conduct Test
        var cancelSource = new CancellationTokenSource();
        var opTask = svc.OpenStream(cancelSource.Token, retyClosedConnection: false);

        VerifyTweetProcessor();

        // Verify expected calls made to HttpClient
        mockHttpClientFactory.Verify(_ => _.CreateClient(It.IsAny<string>()), Times.Once);
        mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact(Timeout = 5000)]
    public async Task OpenStream_BadUrl()
    {
        cfg.Url = "BAD URL";

        var mockLogger = new Mock<ILogger<TwitterStreamReaderService>>();
        SetupMockHttp(new HttpResponseMessage());

        SetupMockTweetProcessor();

        // Intantiate Service
        var svc = new TwitterStreamReaderService(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockServiceProvider.Object);

        // Conduct Test
        var token = new CancellationTokenSource().Token;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
        svc.OpenStream(token, retyClosedConnection: false));
        Assert.Contains("invalid request URI", exception.Message);
    }

    [Fact(Timeout = 5000)]
    public async Task OpenStream_401()
    {
        var mockLogger = new Mock<ILogger<TwitterStreamReaderService>>();
        SetupMockHttp(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized });
        SetupMockTweetProcessor();

        // Intantiate Service
        var svc = new TwitterStreamReaderService(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockServiceProvider.Object);

        // Conduct Test
        var token = new CancellationTokenSource().Token;
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            svc.OpenStream(token, retyClosedConnection: false));
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact(Timeout = 5000)]
    public void OpenStream_WithRequiredField()
    {
        var mockLogger = new Mock<ILogger<TwitterStreamReaderService>>();

        SetupMockHttp(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(testStreamData))
        });

        SetupMockTweetProcessor("reqfield");

        // Intantiate Service
        var svc = new TwitterStreamReaderService(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockServiceProvider.Object);

        // Conduct Test
        var cancelSource = new CancellationTokenSource();
        var opTask = svc.OpenStream(cancelSource.Token, retyClosedConnection: false);

        // Verify expected calls made to HttpClient
        mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(r => 
                r.Method == HttpMethod.Get &&
                r.RequestUri!.ToString() == $"{cfg.Url}?tweet.fields=reqfield"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact(Timeout = 5000)]
    public void OpenStream_WithRequiredFields()
    {
        var mockLogger = new Mock<ILogger<TwitterStreamReaderService>>();

        SetupMockHttp(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(testStreamData))
        });

        SetupMockTweetProcessor("reqfield1", "reqfield2");

        // Intantiate Service
        var svc = new TwitterStreamReaderService(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockServiceProvider.Object);

        // Conduct Test
        var cancelSource = new CancellationTokenSource();
        var opTask = svc.OpenStream(cancelSource.Token, retyClosedConnection: false);

        // Verify expected calls made to HttpClient
        mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Get &&
                r.RequestUri!.ToString() == $"{cfg.Url}?tweet.fields=reqfield1,reqfield2"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact(Timeout = 5000)]
    public void OpenStream_WithRequiredFieldsAndConfigQueryParam()
    {
        cfg.Url = "https://another.com/?test=works";
        var mockLogger = new Mock<ILogger<TwitterStreamReaderService>>();

        SetupMockHttp(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(testStreamData))
        });

        SetupMockTweetProcessor("reqfield1", "reqfield2");

        // Intantiate Service
        var svc = new TwitterStreamReaderService(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockServiceProvider.Object);

        // Conduct Test
        var cancelSource = new CancellationTokenSource();
        var opTask = svc.OpenStream(cancelSource.Token, retyClosedConnection: false);

        // Verify expected calls made to HttpClient
        mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Get &&
                r.RequestUri!.ToString() == $"{cfg.Url}&tweet.fields=reqfield1,reqfield2"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact(Timeout=5000)]
    public async Task OpenStream_CancelToken()
    {
        var mockLogger = new Mock<ILogger<TwitterStreamReaderService>>();
        SetupMockHttp(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StreamContent(new NeverEndingTweetStream())
        });
        SetupMockTweetProcessor();

        // Intantiate Service
        var svc = new TwitterStreamReaderService(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockServiceProvider.Object);

        // Conduct Test
        var cancelSource = new CancellationTokenSource();
        var opTask = svc.OpenStream(cancelSource.Token, retyClosedConnection: false);
        await Task.Delay(1000);
        cancelSource.Cancel();
        await opTask;
    }

    [Fact(Timeout = 5000)]
    public async Task OpenStream_ConnectionRecovery()
    {
        var mockLogger = new Mock<ILogger<TwitterStreamReaderService>>();

        var stream = new NeverEndingTweetStream();
        SetupMockHttp(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StreamContent(stream)
        });

        SetupMockTweetProcessor();

        // Intantiate Service
        var svc = new TwitterStreamReaderService(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockServiceProvider.Object);

        // Conduct Test
        var cancelSource = new CancellationTokenSource();
        var opTask = svc.OpenStream(cancelSource.Token);

        await Task.Delay(500);
        stream.SuddenlyFail();
        await Task.Delay(500);
        stream.SuddenlyFail();
        await Task.Delay(500);
        cancelSource.Cancel();
        await opTask;

        // Verify expected calls made to HttpClient
        mockHttpClientFactory.Verify(_ => _.CreateClient(It.IsAny<string>()), Times.Once);
        mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(3),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ProcessStream_LogThrottling()
    {
        Mock<ILogger<TwitterStreamReaderService>> mockLogger = new Mock<ILogger<TwitterStreamReaderService>>(MockBehavior.Strict);
        mockLogger.Setup(_ => _.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        Mock <IHttpClientFactory> mockHttpClientFactory = new Mock<IHttpClientFactory>();
        SetupMockTweetProcessor();

        // Intantiate Service
        var svc = new TwitterStreamReaderService(cfg,
            mockLogger.Object,
            mockHttpClientFactory.Object,
            mockServiceProvider.Object);

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

        VerifyTweetProcessor(isOpenStreamTest: false);

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

    TwitterStreamReaderServiceConfiguration cfg = new TwitterStreamReaderServiceConfiguration
    {
        logFrequencySeconds = 1,
        Url = "http://testserver.com/NotCalled",
        AccessToken = "The Access Token"
    };

    // Make test stream data with each tweet having a unique hashtag for equality checking
    TweetDataWrapper[] testStreamData = Enumerable
            .Range(0, 500)
            .Select(_ => new TweetDataWrapper(Guid.NewGuid().ToString()))
            .ToArray();

    void SetupMockTweetProcessor(params string[] requiredFields)
    {
        mockTweetProcessor = new Mock<ITweetProcessor>(MockBehavior.Strict);
        mockTweetProcessor.Setup(_ => _.RequiredFields)
            .Returns(() => requiredFields)
            .Verifiable();
        mockTweetProcessor
            .Setup(_ => _.Start())
            .Verifiable();
        mockTweetProcessor
            .Setup(_ => _.AddTweet(Capture.In(addedTweets)))
            .Verifiable();

        mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(_ => _.GetService(typeof(IEnumerable<ITweetProcessor>)))
            .Returns(new ITweetProcessor[] { mockTweetProcessor.Object });
    }
    Mock<ITweetProcessor> mockTweetProcessor = null!;
    Mock<IServiceProvider> mockServiceProvider = null!;
    List<Tweet> addedTweets = new List<Tweet>();
    void VerifyTweetProcessor(bool isOpenStreamTest = true)
    {
        Assert.Equal(testStreamData.Length, addedTweets.Count);
        for (int i = 0; i < testStreamData.Length; i++)
            Assert.Equal(testStreamData[i].Data!.Entities!.Hashtags![0].Tag, addedTweets[i].Entities!.Hashtags![0].Tag);
        mockTweetProcessor.Verify(_ => _.RequiredFields, isOpenStreamTest ? Times.Once() : Times.Never());
        mockTweetProcessor.Verify(_ => _.Start(), Times.Once());
        mockTweetProcessor.Verify(_ => _.AddTweet(It.IsAny<Tweet>()), Times.Exactly(testStreamData.Length));
    }

    void SetupMockHttp(HttpResponseMessage httpResponseMessage)
    {
        mockHttpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
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
    }
    Mock<IHttpClientFactory> mockHttpClientFactory = null!;
    Mock<HttpMessageHandler> mockHttpMessageHandler = null!;
}