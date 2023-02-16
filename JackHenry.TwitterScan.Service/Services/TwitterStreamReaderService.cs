using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JackHenry.TwitterScan.Service.Services;

public interface ITwitterStreamReaderService
{
    Task OpenStream(CancellationToken stoppingToken);
}

[ExcludeFromCodeCoverage]
public class TwitterStreamReaderServiceConfiguration
{
    public string Url { get; set; } = null!;
    public string? AccessToken { get; set; }
    public int logFrequencySeconds { get; set; }
}

public class TwitterStreamReaderService : ITwitterStreamReaderService
{
    public TwitterStreamReaderService(
        TwitterStreamReaderServiceConfiguration config,
        ILogger<TwitterStreamReaderService> logger,
        IHttpClientFactory httpClientFactory,
        ITweetStatisticsRepository tweetStatRepo) =>
        (_config, _logger, _httpClientFactory, _tweetStatRepo) =
        (config, logger, httpClientFactory, tweetStatRepo);
    readonly TwitterStreamReaderServiceConfiguration _config;
    readonly ILogger<TwitterStreamReaderService> _logger;
    readonly IHttpClientFactory _httpClientFactory;
    readonly ITweetStatisticsRepository _tweetStatRepo;

    public Task OpenStream(CancellationToken stoppingToken) => OpenStream(stoppingToken, true);
    internal async Task OpenStream(CancellationToken stoppingToken, bool retyClosedConnection)
    {
        // set up HTTP Client
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.AccessToken);
        httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);

        do
        {
            // open the stream
            _logger.LogInformation("Opening Tweet stream.", new
            {
                Url = _config.Url,
                HasAccessToken = !string.IsNullOrWhiteSpace(_config.AccessToken)
            });
            using var stream = await httpClient.GetStreamAsync(_config.Url, stoppingToken);
            _logger.LogInformation("Stream opened.");
            using var reader = new StreamReader(stream);
            var jsonStream = JsonSerializer.DeserializeAsyncEnumerable<TweetDataWrapper>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, stoppingToken);

            // process the stream
            try
            { 
                await ProcessStream(jsonStream);
                if (!retyClosedConnection) break;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Stream interrupted with exception: {ex.Message}.");
            }
        } while (!stoppingToken.IsCancellationRequested);
    }

    internal async Task ProcessStream(IAsyncEnumerable<TweetDataWrapper?> jsonStream)
    {
        var count = 0;
        var rate = 1.0;
        var start = DateTime.UtcNow.Ticks;
        _tweetStatRepo.Start();
        await foreach (var dataWrapper in jsonStream)
        {
            if (dataWrapper == null) continue;
            _tweetStatRepo.AddTweet(dataWrapper.Data);
            count++;

            // limit rate adjustment and logging chances to about once per second
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
