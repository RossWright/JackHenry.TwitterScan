using JackHenry.TwitterScan;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseHttpsRedirection();

var testTags = new TweetHashtag[]
{
    new TweetHashtag{ Tag = "atletico" },
    new TweetHashtag{ Tag = "ajax" },
    new TweetHashtag{ Tag = "sporting" },
    new TweetHashtag{ Tag = "travel" },
    new TweetHashtag{ Tag = "wisdom" },
    new TweetHashtag{ Tag = "thoughts" },
    new TweetHashtag{ Tag = "feeling" },
    new TweetHashtag{ Tag = "love" },
    new TweetHashtag{ Tag = "midpoint" },
    new TweetHashtag{ Tag = "dude" },
    new TweetHashtag{ Tag = "smart" },
    new TweetHashtag{ Tag = "super" },
    new TweetHashtag{ Tag = "is a" },
    new TweetHashtag{ Tag = "he" },
    new TweetHashtag{ Tag = "wright" },
    new TweetHashtag{ Tag = "ross" },
    new TweetHashtag{ Tag = "hire" },
    new TweetHashtag{ Tag = "should" },
    new TweetHashtag{ Tag = "you" },
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

        // define 3 phases of tweet hashtag frequency to illustrate changing trends
        var stackedTestTagSets = new TweetHashtag[][]
        {
            // fair deck, all tag have equal chance
            testTags,

            //stack the deck so higher index hashtags are picked more
            Enumerable
                .Range(0, testTags.Length)
                .SelectMany(i => Enumerable.Repeat(testTags[i], i + 1))
                .ToArray(),

            //stack the deck so lower index hashtags are picked more
            Enumerable
                .Range(0, testTags.Length)
                .SelectMany(i => Enumerable.Repeat(testTags[i], (testTags.Length - 1 - i)/3 + 1))
                .ToArray(),
        };

        // Start with the first stacked Test Tag Set
        var stackedTestTags = stackedTestTagSets[0];

        var squareOfLength = testTags.Length * testTags.Length;
        while (true)
        {
            var elapsedTicks = DateTime.UtcNow.Ticks - start;
            while (count >= elapsedTicks / ticksPerTweet) await Task.Yield();

            if (stackedTestTags != stackedTestTagSets[2]) 
            {
                if (stackedTestTags == stackedTestTagSets[1])
                {
                    // After 30 seconds, switch to the last set
                    if (elapsedTicks > 30 * TimeSpan.TicksPerSecond)
                        stackedTestTags = stackedTestTagSets[2];
                }
                else
                {
                    // after 15 seconds, switch to the next set
                    if (elapsedTicks > 15 * TimeSpan.TicksPerSecond)
                        stackedTestTags = stackedTestTagSets[1];
                }
            }

            // Randomly choose between a tweet with 0 and 5 hashtags
            var hashtags = preAllocatedHashtagArrays[rand.Next(6)];
            for (var i = 0; i < hashtags.Length; i++)
            {
                hashtags[i] = stackedTestTags[rand.Next(stackedTestTags.Length)];
            }
            tweetDataWrapper.Data.Entities.Hashtags = hashtags;

            yield return tweetDataWrapper;
            
            count++;
        }
    }
    return Stream();
});

app.Run();