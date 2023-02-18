using RossWright;
using System.Collections.Concurrent;

namespace JackHenry.TwitterScan.Service;

public interface ITweetHashtagProcessor
{
    TweetHashtagStatistics GetTweetStats();
}

public class TweetHashtagProcessor : 
    ITweetHashtagProcessor, ISingleton<ITweetHashtagProcessor>,
    ITweetProcessor, ISingleton<ITweetProcessor>
{
    public TweetHashtagProcessor(ILogger<TweetHashtagProcessor> logger) => _logger = logger;
    readonly ILogger<TweetHashtagProcessor> _logger;

    int _count = 0;
    ConcurrentDictionary<string, int> _hashTagCount = new ConcurrentDictionary<string, int>();

    public void Start() => _start = DateTime.UtcNow;
    DateTime _start;

    public void AddTweet(Tweet tweet)
    {
        if (tweet == null)
        {
            _logger.LogWarning("Attempted to add null tweet to Tweet Repository");
            return;
        }
        Interlocked.Increment(ref _count);
        if (tweet.Hashtags?.Any() == true)
        {
            foreach (var hashtag in tweet.Hashtags)
            {
                _hashTagCount.AddOrUpdate(hashtag, 1, (key, value) => value + 1);
            }
        }
    }

    public TweetHashtagStatistics GetTweetStats()
    {
        var stats = new TweetHashtagStatistics
        {
            ElapsedSeconds = (DateTime.UtcNow - _start).TotalSeconds,
            Count = _count,
            TopTenHashtags = _hashTagCount
                .OrderByDescending(_ => _.Value)
                .Take(10)
                .Select(_ => new TweetHashtagRank { Tag = _.Key, Count = _.Value })
                .ToArray()
        };
        _logger.LogInformation("Tweet Hashtag Stats calculated", stats);
        return stats;
    }
}
