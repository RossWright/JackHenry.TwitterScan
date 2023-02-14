using System.Text.Json.Serialization;

namespace JackHenry.TwitterScan;

public class TweetDataWrapper
{
    public TweetDataWrapper() => 
        Data = new Tweet();
    public TweetDataWrapper(params string[] hashtags) =>
        Data = new Tweet(hashtags);

    [JsonPropertyName("data")]
    public Tweet Data { get; init; }
}

public class Tweet
{
    public Tweet() { }
    public Tweet(params string[] hashtags) => 
        Entities.Hashtags = hashtags
            .Select(_ => new TweetHashtag { Tag = _ })
            .ToArray();

    [JsonPropertyName("entities")]
    public TweetEntities Entities { get; set; } = new TweetEntities();

    public IEnumerable<string>? Hashtags => Entities.Hashtags?.Select(_ => _.Tag);
}

public class TweetEntities
{
    [JsonPropertyName("hashtags")]
    public TweetHashtag[] Hashtags { get; set; } = null!;
}

public class TweetHashtag
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = null!;
}
