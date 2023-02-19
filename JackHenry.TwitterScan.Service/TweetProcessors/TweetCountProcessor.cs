namespace JackHenry.TwitterScan.Service;

public interface ITweetCountProcessor
{
    TweetCountStatistics GetCountStats();
}

public class TweetCountProcessor : 
    ITweetCountProcessor, ISingleton<ITweetCountProcessor>,
    ITweetProcessor, ISingleton<ITweetProcessor>
{
    public TweetCountProcessor(ILogger<TweetCountProcessor> logger) => _logger = logger;
    readonly ILogger<TweetCountProcessor> _logger;

    long _count = 0;

    public IEnumerable<string>? RequiredFields => null;
    public void Start() => _start = DateTime.UtcNow;
    DateTime _start;

    public void AddTweet(Tweet tweet)
    {
        if (tweet == null)
        {
            _logger.LogWarning("Attempted to add null tweet");
            return;
        }
        Interlocked.Increment(ref _count);
    }

    public TweetCountStatistics GetCountStats()
    {
        var stats = new TweetCountStatistics
        {
            ElapsedSeconds = (DateTime.UtcNow - _start).TotalSeconds,
            Count = Interlocked.Read(ref _count),
        };
        _logger.LogInformation("Tweet Count Stats calculated", stats);
        return stats;
    }
}
