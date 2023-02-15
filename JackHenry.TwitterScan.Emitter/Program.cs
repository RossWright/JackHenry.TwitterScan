using JackHenry.TwitterScan;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseHttpsRedirection();

var testTags = new TweetHashtag[]
{
    new TweetHashtag{ Tag = "Bucks" },
    new TweetHashtag{ Tag = "Celtics" },
    new TweetHashtag{ Tag = "Valentine's Day" },
    new TweetHashtag{ Tag = "Giannis" },
    new TweetHashtag{ Tag = "Grant Williams" },
    new TweetHashtag{ Tag = "Hauser" },
    new TweetHashtag{ Tag = "Derrick White" },
    new TweetHashtag{ Tag = "WWENXT" },
    new TweetHashtag{ Tag = "FearTheDeer" },
    new TweetHashtag{ Tag = "Middleton" },
    new TweetHashtag{ Tag = "LoveCampingWorld" },
    new TweetHashtag{ Tag = "Providence" },
    new TweetHashtag{ Tag = "BleedGreen" },
    new TweetHashtag{ Tag = "Brogdon" },
    new TweetHashtag{ Tag = "WhyImSingle" },
    new TweetHashtag{ Tag = "Horford" },
    new TweetHashtag{ Tag = "Mustard" },
    new TweetHashtag{ Tag = "Creighton" },
    new TweetHashtag{ Tag = "Smart" },
    new TweetHashtag{ Tag = "Ty Jerome" },
    new TweetHashtag{ Tag = "Gradey Dick" },
    new TweetHashtag{ Tag = "Tucson" },
    new TweetHashtag{ Tag = "Harley Quinn" },
    new TweetHashtag{ Tag = "Milwaukee" },
    new TweetHashtag{ Tag = "No Tatum" },
    new TweetHashtag{ Tag = "Juwan" },
    new TweetHashtag{ Tag = "Dolores" },
    new TweetHashtag{ Tag = "Roses" },
    new TweetHashtag{ Tag = "Floki" },
    new TweetHashtag{ Tag = "Keatts" },
    new TweetHashtag{ Tag = "Boeheim" },
    new TweetHashtag{ Tag = "Monica" },
    new TweetHashtag{ Tag = "Doge" },
    new TweetHashtag{ Tag = "Jinder" },
    new TweetHashtag{ Tag = "NCT DREAM" },
    new TweetHashtag{ Tag = "Kohl Center" },
    new TweetHashtag{ Tag = "Jakob Poeltl" },
    new TweetHashtag{ Tag = "sporting" },
    new TweetHashtag{ Tag = "travel" },
    new TweetHashtag{ Tag = "wisdom" },
    new TweetHashtag{ Tag = "thoughts" },
    new TweetHashtag{ Tag = "feeling" },
    new TweetHashtag{ Tag = "love" },
    new TweetHashtag{ Tag = "and humble" },
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
    new TweetHashtag{ Tag = "is stuck" },
    new TweetHashtag{ Tag = "this thing" },
    new TweetHashtag{ Tag = "institutions" },
    new TweetHashtag{ Tag = "and financial" },
    new TweetHashtag{ Tag = "business" },
    new TweetHashtag{ Tag = "people" },
    new TweetHashtag{ Tag = "between" },
    new TweetHashtag{ Tag = "connections" },
    new TweetHashtag{ Tag = "strengthening" },
    new TweetHashtag{ Tag = "JackHenry" },
};

// define 3 phases of tweet hashtag frequency to illustrate changing trends
var stackedTestTagSets = new TweetHashtag[][]
{   
    // fair deck, all tag have equal chance
    testTags,

    // stack the deck so only tags in the select set are picked (weighted in reverse order)
    Enumerable
        .Range(testTags.Length-10, 10)
        .SelectMany(i => Enumerable.Repeat(testTags[i], i*i + 1))
        .ToArray(),

    // stack the deck so only hashtags outside the select set are picked
    Enumerable
        .Range(0, testTags.Length-10)
        .Select(i => testTags[i])
        .ToArray(),

    // stack the deck so higher index hashtags are picked (excluding select set)
    Enumerable
        .Range(0, testTags.Length-10)
        .SelectMany(i => Enumerable.Repeat(testTags[i], i + 1))
        .ToArray(),
            
    // stack the deck so lower index hashtags are picked slightly more often
    Enumerable
        .Range(0, testTags.Length-20)
        .SelectMany(i => Enumerable.Repeat(testTags[i], (testTags.Length - i - 1)/5 + 1))
        .ToArray(),
};
var tagSetTiming = new int[] { 10, 12, 30, 40, int.MaxValue };

app.MapGet("/stream", (int? rate) =>
{
    async IAsyncEnumerable<TweetDataWrapper> Stream()
    {
        // Make one set of tweet objects that will be re-used within the loop
        var tweetDataWrapper = new TweetDataWrapper();

        // pre-allocate hash tag arrays of different links which will be random picked and filled below
        var preAllocatedHashtagArrays = new TweetHashtag[][]
        {
            new TweetHashtag[0],
            new TweetHashtag[1],
            new TweetHashtag[2],
            new TweetHashtag[3],
            new TweetHashtag[4],
            new TweetHashtag[5],
        };

        // If a rate query param was provided, use it. Otherwise default to 57 tweets per second
        var tweetsPerSecond = rate ?? 57.0;
        var ticksPerTweet = TimeSpan.TicksPerSecond / tweetsPerSecond;

        // Start with the first stacked Test Tag Set
        var setIndex = 0;

        // initialize variables used in the loop
        var start = DateTime.UtcNow.Ticks;
        var count = 0;
        var rand = new Random();
        var stackedTestTags = stackedTestTagSets[setIndex];
        double elapsedTicks = 0;
        var nextSetChange = tagSetTiming![setIndex] * TimeSpan.TicksPerSecond;

        // The never ending stream loop
        while (true)
        {
            // if the number of tweet sent out is equal or greater than the number we should have sent out
            //      Yield the process until the timer catches up.
            // Note that using Thread.Sleep or Task.Delay would not work as we are delaying on a
            //      tick/nanoseconds scale and those methods only support millisecond resolution.
            while (count > elapsedTicks / ticksPerTweet)
            {
                await Task.Yield();
                elapsedTicks = DateTime.UtcNow.Ticks - start;
            }

            // Trigger test tag set changes
            if (elapsedTicks > nextSetChange)
            {
                setIndex++;
                stackedTestTags = stackedTestTagSets[setIndex];
                if (setIndex < tagSetTiming.Length)
                    nextSetChange = tagSetTiming![setIndex] * TimeSpan.TicksPerSecond;
            }

            // Randomly choose between a number of hashtags (use the pre-allocated arrays declared above)
            var hashtags = preAllocatedHashtagArrays[rand.Next(6)];

            // fill the hashtag array with random hash tags
            for (var i = 0; i < hashtags.Length; i++)
            {
                hashtags[i] = stackedTestTags[rand.Next(stackedTestTags.Length)];
            }

            // set the hashtags into the re-used tweet object and send it.
            tweetDataWrapper.Data.Entities.Hashtags = hashtags;
            yield return tweetDataWrapper;
            
            count++;
        }
    }
    return Stream();
});

app.Run();