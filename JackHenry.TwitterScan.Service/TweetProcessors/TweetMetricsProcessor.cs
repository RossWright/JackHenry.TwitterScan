namespace JackHenry.TwitterScan.Service;

public interface ITweetMetricsProcessor
{
    TweetMetricsStatistics GetMetricsStats();
}

public class TweetMetricsProcessor :
    ITweetMetricsProcessor, ISingleton<ITweetMetricsProcessor>,
    ITweetProcessor, ISingleton<ITweetProcessor>
{
    public TweetMetricsProcessor(ILogger<TweetMetricsProcessor> logger) => _logger = logger;
    readonly ILogger<TweetMetricsProcessor> _logger;

    long _impressionCount = 0;
    long _retweetCount = 0;
    long _quoteCount = 0;
    long _likeCount = 0;

    public IEnumerable<string>? RequiredFields => 
        new string[] { "public_metrics", "non_public_metrics" };

    public void Start() { }

    public void AddTweet(Tweet tweet)
    {
        if (tweet == null)
        {
            _logger.LogWarning("Attempted to add null tweet");
            return;
        }
        if (tweet.NonPublicMetrics != null)
        {
            Interlocked.Add(ref _impressionCount, tweet.NonPublicMetrics.ImpressionCount);
        }
        if (tweet.PublicMetrics != null)
        {
            Interlocked.Add(ref _retweetCount, tweet.PublicMetrics.RetweetClicks);
            Interlocked.Add(ref _quoteCount, tweet.PublicMetrics.QuoteCount);
            Interlocked.Add(ref _likeCount, tweet.PublicMetrics.LikeCount);
        }
    }

    public TweetMetricsStatistics GetMetricsStats()
    {
        var stats = new TweetMetricsStatistics
        {
            ImpressionCount = Interlocked.Read(ref _impressionCount),
            RetweetCount = Interlocked.Read(ref _retweetCount),
            QuoteCount = Interlocked.Read(ref _quoteCount),
            LikeCount = Interlocked.Read(ref _likeCount)
        };
        _logger.LogInformation("Tweet Metric Stats calculated", stats);
        return stats;
    }
}
