using JackHenry.TwitterScan;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseHttpsRedirection();

var testtags = new string[]
{
    "competition",
    "influencer",
    "influencermarketing",
    "fridayfeeling",
    "MondayMotivation",
    "tbt",
    "traveltuesday",
    "vegan",
    "fitness",
    "UCLdraw",
    "UEFA",
    "Messi",
    "Bayern",
    "THE_W",
    "NtMv1_6",
    "Lille",
    "Benfica",
    "Villarreal",
    "Atletico ",
    "Ajax",
    "Sporting",
    "TravelTuesday",
    "WednesdayWisdom",
    "ThursdayThoughts",
    "FridayFeeling",
    "love",
    "Twitterers",
    "smile",
    "picoftheday",
    "follow",
    "fun",
    "lol",
    "friends",
    "life",
    "amazing",
    "family",
    "music",
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
        while (true)
        {
            while (count >= (DateTime.UtcNow.Ticks - start) / ticksPerTweet) await Task.Yield();
            tweet.entities.hashtags =Enumerable.Range(0, rand.Next(4))
                .Select(i => new TweetHashtag { tag = testtags[i] })
                .ToArray();
            yield return tweet;
            count++;
        }
    }
    return Stream();
});

app.Run();