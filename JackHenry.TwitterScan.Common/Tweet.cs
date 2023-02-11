namespace JackHenry.TwitterScan;
public record Tweet
{
    public TweetEntities entities { get; } = new TweetEntities();
}

public class TweetEntities
{
    public TweetHashtag[] hashtags { get; set; } = null!;
}

public class TweetHashtag
{
    public string tag { get; set; } = null!;
}
