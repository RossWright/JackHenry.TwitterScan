namespace JackHenry.TwitterScan;

public class TweetHashtagStatistics
{
    public TweetHashtagRank[] TopTenHashtags { get; set; } = null!;
}

public class TweetHashtagRank
{
    public string Tag { get; set; } = null!;
    public int Count { get; set; }
}
