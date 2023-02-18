namespace JackHenry.TwitterScan;

public class TweetHashtagStatistics
{
    public double ElapsedSeconds { get; set; }
    public int Count { get; set; }
    public TweetHashtagRank[] TopTenHashtags { get; set; } = null!;
}

public class TweetHashtagRank
{
    public string Tag { get; set; } = null!;
    public int Count { get; set; }
}
