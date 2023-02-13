using System.Net.Http.Headers;
using System.Text.Json;
namespace JackHenry.TwitterScan.Service;

public interface ITweetReceiver
{
    Task OpenStream(CancellationToken stoppingToken);
}

public class TwitterReceiverConfiguration
{
    public string Url { get; set; } = null!;
    public string? AccessToken { get; set; }
    public int logFrequencySeconds { get; set; }
}

public class TweetReciever : ITweetReceiver
{
    public TweetReciever(
        TwitterReceiverConfiguration config,
        ILogger<TweetReciever> logger,
        IHttpClientFactory httpClientFactory,
        ITweetStatRepository tweetStatRepo) =>
        (_config, _logger, _httpClientFactory, _tweetStatRepo) =
        ( config, logger, httpClientFactory, tweetStatRepo);
    readonly TwitterReceiverConfiguration _config;
    readonly ILogger<TweetReciever> _logger;
    readonly IHttpClientFactory _httpClientFactory;
    readonly ITweetStatRepository _tweetStatRepo;

    public async Task OpenStream(CancellationToken stoppingToken)
    {
        // set up HTTP Client
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.AccessToken);
        httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);

        // open the stream
        _logger.LogInformation("Tweet Receiver is opening Tweet stream.", new
        {
            Url = _config.Url,
            HasAccessToken = !string.IsNullOrWhiteSpace(_config.AccessToken)
        });
        using var stream = await httpClient.GetStreamAsync(_config.Url, stoppingToken);
        _logger.LogInformation("Tweet Receiver Tweet stream opened.");
        using var reader = new StreamReader(stream);
        var jsonStream = JsonSerializer.DeserializeAsyncEnumerable<Tweet>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, stoppingToken);

        // process the stream
        await ProcessStream(jsonStream);

        _logger.LogInformation("Tweet Receiver has stopped.");
    }

    internal async Task ProcessStream(IAsyncEnumerable<Tweet?> jsonStream)
    {
        var count = 0;
        var rate = 1.0;
        var start = DateTime.UtcNow.Ticks;
        _tweetStatRepo.Start();
        await foreach (var tweet in jsonStream)
        {
            _tweetStatRepo.AddTweet(tweet!);
            count++;

            // try to limit rate adjustment and logging chances to about once per second
            if (count % (int)rate == 0)
            {
                var secondsElapsed = (double)(DateTime.UtcNow.Ticks - start) / TimeSpan.TicksPerSecond;
                rate = count / secondsElapsed;

                // if it's time to log, log progress
                if ((int)secondsElapsed % _config.logFrequencySeconds == 0)
                {
                    _logger.LogInformation($"Tweet Stream Update", new
                    {
                        SecondsElapsed = secondsElapsed,
                        TweetsRecevied = count,
                        TweetsPerSecond = rate
                    });
                }
            }
        }
    }
}
