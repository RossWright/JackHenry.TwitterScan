using JackHenry.TwitterScan;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseHttpsRedirection();

var testtags = new TweetHashtag[]
{
    new TweetHashtag{ Tag = "competition" },
    new TweetHashtag{ Tag = "influencer" },
    new TweetHashtag{ Tag = "influencermarketing" },
    new TweetHashtag{ Tag = "fridayfeeling" },
    new TweetHashtag{ Tag = "MondayMotivation" },
    new TweetHashtag{ Tag = "tbt" },
    new TweetHashtag{ Tag = "traveltuesday" },
    new TweetHashtag{ Tag = "vegan" },
    new TweetHashtag{ Tag = "fitness" },
    new TweetHashtag{ Tag = "UCLdraw" },
    new TweetHashtag{ Tag = "UEFA" },
    new TweetHashtag{ Tag = "Messi" },
    new TweetHashtag{ Tag = "Bayern" },
    new TweetHashtag{ Tag = "THE_W" },
    new TweetHashtag{ Tag = "NtMv1_6" },
    new TweetHashtag{ Tag = "Lille" },
    new TweetHashtag{ Tag = "Benfica" },
    new TweetHashtag{ Tag = "Villarreal" },
    new TweetHashtag{ Tag = "Atletico " },
    new TweetHashtag{ Tag = "Ajax" },
    new TweetHashtag{ Tag = "Sporting" },
    new TweetHashtag{ Tag = "TravelTuesday" },
    new TweetHashtag{ Tag = "WednesdayWisdom" },
    new TweetHashtag{ Tag = "ThursdayThoughts" },
    new TweetHashtag{ Tag = "FridayFeeling" },
    new TweetHashtag{ Tag = "love" },
    new TweetHashtag{ Tag = "Twitterers" },
    new TweetHashtag{ Tag = "smile" },
    new TweetHashtag{ Tag = "picoftheday" },
    new TweetHashtag{ Tag = "follow" },
    new TweetHashtag{ Tag = "fun" },
    new TweetHashtag{ Tag = "lol" },
    new TweetHashtag{ Tag = "friends" },
    new TweetHashtag{ Tag = "life" },
    new TweetHashtag{ Tag = "amazing" },
    new TweetHashtag{ Tag = "family" },
    new TweetHashtag{ Tag = "music" },
};

app.MapGet("/stream", (int? rate) =>
{
    async IAsyncEnumerable<TweetDataWrapper> Stream()
    {
        var ratePerSecond = rate ?? 57;
        var ticksPerTweet = TimeSpan.TicksPerSecond / ratePerSecond;
        var tweetDataWrapper = new TweetDataWrapper();
        var start = DateTime.UtcNow.Ticks;
        var count = 0;
        var rand = new Random();
        var preAllocatedHashtagArrays = new TweetHashtag[][]
        {
            new TweetHashtag[0],
            new TweetHashtag[1],
            new TweetHashtag[2],
            new TweetHashtag[3],
            new TweetHashtag[4],
            new TweetHashtag[5],
        };

        var squareOfLength = testtags.Length * testtags.Length;
        while (true)
        {
            while (count >= (DateTime.UtcNow.Ticks - start) / ticksPerTweet) await Task.Yield();

            // Randomly choose between a tweet with 0 and 5 hashtags
            var hashtags = preAllocatedHashtagArrays[rand.Next(6)];
            // Random pick the hashtags using a method that picks the hashtags near the end of the array more often
            for (var i = 0; i < hashtags.Length; i++)
            {
                var pick = (int)Math.Sqrt(rand.Next(squareOfLength) + 1);
                hashtags[i] = testtags[pick];
            }
            tweetDataWrapper.Data.Entities.Hashtags = hashtags;

            yield return tweetDataWrapper;
            
            count++;
        }
    }
    return Stream();
});

app.Run();