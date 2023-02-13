namespace JackHenry.TwitterScan;
public class Tweet
{
    public TweetEntities entities { get; set; } = new TweetEntities();

    public Tweet() { }
    public Tweet(params string[] hashtags) => 
        entities.hashtags = hashtags
            .Select(_ => new TweetHashtag { tag = _ })
            .ToArray();
}

public class TweetEntities
{
    public TweetHashtag[] hashtags { get; set; } = null!;
}

public class TweetHashtag
{
    public string tag { get; set; } = null!;
}
