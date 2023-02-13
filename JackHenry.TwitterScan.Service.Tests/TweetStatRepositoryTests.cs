namespace JackHenry.TwitterScan.Service.Tests;

public partial class TweetStatRepositoryTests
{
    [Fact]
    public void HappyPath()
    {
        var hashtags = Enumerable.Range(0, 100)
            .Select(i => $"hashtagindex{i}")
            .ToArray();

        var statRepo = new TweetStatRepository();
        DateTime start = DateTime.UtcNow;
        statRepo.Start();

        // Add Tweets such that the number of tweets with each hashtag is equal to it's index
        for (var i = 0; i < hashtags.Length; i++)
        {
            statRepo.AddTweet(new Tweet(
                Enumerable.Range(i, hashtags.Length - i)
                    .Select(j => hashtags[j])
                    .ToArray()));
        }

        var actualElapsed = (DateTime.UtcNow - start).TotalSeconds;
        var stats = statRepo.GetTweetStats();
                    
        Assert.Equal(hashtags.Length, stats.Count);

        Assert.InRange(actualElapsed, 
            stats.ElapsedSeconds-0.5, 
            stats.ElapsedSeconds + 0.5);

        // Verify the Top Ten Hashtags are right
        for (var i = 0; i < stats.TopTenHashtags.Length; i++)
        {
            Assert.Equal(hashtags[hashtags.Length - i - 1], stats.TopTenHashtags[i].Tag);
            Assert.Equal(hashtags.Length - i, stats.TopTenHashtags[i].Count);
        }
    }

    // TODO: Test Multi-threaded
}
