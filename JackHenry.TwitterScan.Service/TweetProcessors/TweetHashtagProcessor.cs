using RossWright;
using System.Collections.Concurrent;

namespace JackHenry.TwitterScan.Service;

public interface ITweetHashtagProcessor
{
    TweetHashtagStatistics GetHashtagStats();
}

public class TweetHashtagProcessor : 
    ITweetHashtagProcessor, ISingleton<ITweetHashtagProcessor>,
    ITweetProcessor, ISingleton<ITweetProcessor>
{
    public TweetHashtagProcessor(ILogger<TweetHashtagProcessor> logger) => _logger = logger;
    readonly ILogger<TweetHashtagProcessor> _logger;

    ConcurrentDictionary<string, int> _hashTagCount = new ConcurrentDictionary<string, int>();

    public IEnumerable<string>? RequiredFields =>
        new string[] { "entities" };

    public void Start() { }

    public void AddTweet(Tweet tweet)
    {
        if (tweet == null)
        {
            _logger.LogWarning("Attempted to add null tweet");
            return;
        }
        if (tweet.Hashtags?.Any() == true)
        {
            foreach (var hashtag in tweet.Hashtags)
            {
                _hashTagCount.AddOrUpdate(hashtag, 1, (key, value) => value + 1);
            }
        }
    }

    public TweetHashtagStatistics GetHashtagStats()
    {
        var stats = new TweetHashtagStatistics
        {
            TopTenHashtags = _hashTagCount
                .ToArray() //On concurrent dictionary this is thread-safe
                .OrderByDescending(_ => _.Value)
                .Take(10)
                .Select(_ => new TweetHashtagRank { Tag = _.Key, Count = _.Value })
                .ToArray()
        };
        _logger.LogInformation("Tweet Hashtag Stats calculated", stats);
        return stats;
    }
}
