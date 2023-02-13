using JackHenry.TwitterScan;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseHttpsRedirection();

var testtags = new TweetHashtag[]
{
    new TweetHashtag{ tag = "competition" },
    new TweetHashtag{ tag = "influencer" },
    new TweetHashtag{ tag = "influencermarketing" },
    new TweetHashtag{ tag = "fridayfeeling" },
    new TweetHashtag{ tag = "MondayMotivation" },
    new TweetHashtag{ tag = "tbt" },
    new TweetHashtag{ tag = "traveltuesday" },
    new TweetHashtag{ tag = "vegan" },
    new TweetHashtag{ tag = "fitness" },
    new TweetHashtag{ tag = "UCLdraw" },
    new TweetHashtag{ tag = "UEFA" },
    new TweetHashtag{ tag = "Messi" },
    new TweetHashtag{ tag = "Bayern" },
    new TweetHashtag{ tag = "THE_W" },
    new TweetHashtag{ tag = "NtMv1_6" },
    new TweetHashtag{ tag = "Lille" },
    new TweetHashtag{ tag = "Benfica" },
    new TweetHashtag{ tag = "Villarreal" },
    new TweetHashtag{ tag = "Atletico " },
    new TweetHashtag{ tag = "Ajax" },
    new TweetHashtag{ tag = "Sporting" },
    new TweetHashtag{ tag = "TravelTuesday" },
    new TweetHashtag{ tag = "WednesdayWisdom" },
    new TweetHashtag{ tag = "ThursdayThoughts" },
    new TweetHashtag{ tag = "FridayFeeling" },
    new TweetHashtag{ tag = "love" },
    new TweetHashtag{ tag = "Twitterers" },
    new TweetHashtag{ tag = "smile" },
    new TweetHashtag{ tag = "picoftheday" },
    new TweetHashtag{ tag = "follow" },
    new TweetHashtag{ tag = "fun" },
    new TweetHashtag{ tag = "lol" },
    new TweetHashtag{ tag = "friends" },
    new TweetHashtag{ tag = "life" },
    new TweetHashtag{ tag = "amazing" },
    new TweetHashtag{ tag = "family" },
    new TweetHashtag{ tag = "music" },
};

app.MapGet("/stream", (int? rate) =>
{
    async IAsyncEnumerable<Tweet> Stream()
    {
        var ratePerSecond = rate ?? 57;
        var ticksPerTweet = TimeSpan.TicksPerSecond / ratePerSecond;
        Tweet tweet = new Tweet();
        var start = DateTime.UtcNow.Ticks;
        var count = 0;
        var rand = new Random();
        var hashArrays = new TweetHashtag[][]
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

            tweet.entities.hashtags = hashArrays[rand.Next(6)];
            for (var i = 0; i < tweet.entities.hashtags.Length; i++)
            {
                var pick = (int)Math.Sqrt(rand.Next(squareOfLength) + 1) - 1;
                tweet.entities.hashtags[i] = testtags[pick];
            }
            
            yield return tweet;
            
            count++;
        }
    }
    return Stream();
});

app.Run();