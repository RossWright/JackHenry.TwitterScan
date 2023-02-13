namespace JackHenry.TwitterScan;

public class TweetStats
{
    public double ElapsedSeconds { get; set; }
    public int Count { get; set; }
    public HashtagRank[] TopTenHashtags { get; set; } = null!;
}

public class HashtagRank
{
    public string Tag { get; set; } = null!;
    public int Count { get; set; }
}
