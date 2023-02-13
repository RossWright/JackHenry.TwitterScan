using System.Collections.Concurrent;
using static System.Net.Mime.MediaTypeNames;

namespace JackHenry.TwitterScan.Service;

public interface ITweetStatRepository
{
    void Start();
    void AddTweet(Tweet tweet);
    TweetStats GetTweetStats();
}

public class TweetStatRepository : ITweetStatRepository
{
    public TweetStatRepository(ILogger<TweetStatRepository> logger) => _logger = logger;
    readonly ILogger<TweetStatRepository> _logger;

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
        if (tweet.entities?.hashtags?.Any() == true)
        {
            foreach (var hashtag in tweet.entities.hashtags)
            {
                _hashTagCount.AddOrUpdate(hashtag.tag, 1, (key, value) => value + 1);
            }
        }
    }

    public TweetStats GetTweetStats()
    {
        var stats = new TweetStats
        {
            ElapsedSeconds = (DateTime.UtcNow - _start).TotalSeconds,
            Count = _count,
            TopTenHashtags = _hashTagCount
                .OrderByDescending(_ => _.Value)
                .Take(10)
                .Select(_ => new HashtagRank { Tag = _.Key, Count = _.Value })
                .ToArray()
        };
        _logger.LogInformation("Tweet Stats calculated", stats);
        return stats;
    }
}
