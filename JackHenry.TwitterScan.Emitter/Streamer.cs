using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace JackHenry.TwitterScan;

public class Streamer
{
    public Streamer()
    {
        testTags = new TweetTagEntity[]
        {
            new TweetTagEntity{ Tag = "Bucks" },
            new TweetTagEntity{ Tag = "Celtics" },
            new TweetTagEntity{ Tag = "Valentine's Day" },
            new TweetTagEntity{ Tag = "Giannis" },
            new TweetTagEntity{ Tag = "Grant Williams" },
            new TweetTagEntity{ Tag = "Hauser" },
            new TweetTagEntity{ Tag = "Derrick White" },
            new TweetTagEntity{ Tag = "WWENXT" },
            new TweetTagEntity{ Tag = "FearTheDeer" },
            new TweetTagEntity{ Tag = "Middleton" },
            new TweetTagEntity{ Tag = "LoveCampingWorld" },
            new TweetTagEntity{ Tag = "Providence" },
            new TweetTagEntity{ Tag = "BleedGreen" },
            new TweetTagEntity{ Tag = "Brogdon" },
            new TweetTagEntity{ Tag = "WhyImSingle" },
            new TweetTagEntity{ Tag = "Horford" },
            new TweetTagEntity{ Tag = "Mustard" },
            new TweetTagEntity{ Tag = "Creighton" },
            new TweetTagEntity{ Tag = "Smart" },
            new TweetTagEntity{ Tag = "Ty Jerome" },
            new TweetTagEntity{ Tag = "Gradey Dick" },
            new TweetTagEntity{ Tag = "Tucson" },
            new TweetTagEntity{ Tag = "Harley Quinn" },
            new TweetTagEntity{ Tag = "Milwaukee" },
            new TweetTagEntity{ Tag = "No Tatum" },
            new TweetTagEntity{ Tag = "Juwan" },
            new TweetTagEntity{ Tag = "Dolores" },
            new TweetTagEntity{ Tag = "Roses" },
            new TweetTagEntity{ Tag = "Floki" },
            new TweetTagEntity{ Tag = "Keatts" },
            new TweetTagEntity{ Tag = "Boeheim" },
            new TweetTagEntity{ Tag = "Monica" },
            new TweetTagEntity{ Tag = "Doge" },
            new TweetTagEntity{ Tag = "Jinder" },
            new TweetTagEntity{ Tag = "NCT DREAM" },
            new TweetTagEntity{ Tag = "Kohl Center" },
            new TweetTagEntity{ Tag = "Jakob Poeltl" },
            new TweetTagEntity{ Tag = "sporting" },
            new TweetTagEntity{ Tag = "travel" },
            new TweetTagEntity{ Tag = "wisdom" },
            new TweetTagEntity{ Tag = "thoughts" },
            new TweetTagEntity{ Tag = "feeling" },
            new TweetTagEntity{ Tag = "love" },
            new TweetTagEntity{ Tag = "and humble" },
            new TweetTagEntity{ Tag = "dude" },
            new TweetTagEntity{ Tag = "smart" },
            new TweetTagEntity{ Tag = "super" },
            new TweetTagEntity{ Tag = "is a" },
            new TweetTagEntity{ Tag = "he" },
            new TweetTagEntity{ Tag = "wright" },
            new TweetTagEntity{ Tag = "ross" },
            new TweetTagEntity{ Tag = "hire" },
            new TweetTagEntity{ Tag = "should" },
            new TweetTagEntity{ Tag = "you" },
            new TweetTagEntity{ Tag = "is stuck" },
            new TweetTagEntity{ Tag = "this thing" },
            new TweetTagEntity{ Tag = "institutions" },
            new TweetTagEntity{ Tag = "and financial" },
            new TweetTagEntity{ Tag = "business" },
            new TweetTagEntity{ Tag = "people" },
            new TweetTagEntity{ Tag = "between" },
            new TweetTagEntity{ Tag = "connections" },
            new TweetTagEntity{ Tag = "strengthening" },
            new TweetTagEntity{ Tag = "JackHenry" },
        };
        stackedTestTagSets = new TweetTagEntity[][]
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
        tagSetTiming = new int[] { 10, 12, 30, 40, int.MaxValue };
    }
    readonly TweetTagEntity[] testTags = null!;
    readonly TweetTagEntity[][] stackedTestTagSets = null!;
    readonly int[] tagSetTiming = null!;

    public async IAsyncEnumerable<TweetDataWrapper> Stream(int? rate, string? tweetFieldsStr)
    {
        // Make one set of tweet objects that will be re-used within the loop
        var tweetDataWrapper = new TweetDataWrapper
        {
            Data = new Tweet
            {
                Entities = new TweetEntities(),
                PublicMetrics = new TweetPublicMetrics(),
                NonPublicMetrics = new TweetNonPublicMetrics()
            }
        };

        // pre-allocate hash tag arrays of different links which will be random picked and filled below
        var preAllocatedHashtagArrays = new TweetTagEntity[][]
        {
            new TweetTagEntity[0],
            new TweetTagEntity[1],
            new TweetTagEntity[2],
            new TweetTagEntity[3],
            new TweetTagEntity[4],
            new TweetTagEntity[5],
        };

        // If a rate query param was provided, use it. Otherwise default to 57 tweets per second
        var maxTweetsPerSecond = rate ?? TimeSpan.TicksPerSecond;
        var ticksPerTweet = TimeSpan.TicksPerSecond / maxTweetsPerSecond;

        var tweetFields = tweetFieldsStr?.Split(',');
        bool includeEntities = tweetFields?.Contains("entities") ?? false;
        bool includePublicMetrics = tweetFields?.Contains("public_metrics") ?? false;
        bool includeNonPublicMetrics = tweetFields?.Contains("non_public_metrics") ?? false;

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

            if (includeEntities)
            {
                // Randomly choose between a number of hashtags (use the pre-allocated arrays declared above)
                var hashtags = preAllocatedHashtagArrays[rand.Next(6)];

                // fill the hashtag array with random hash tags
                for (var i = 0; i < hashtags.Length; i++)
                {
                    hashtags[i] = stackedTestTags[rand.Next(stackedTestTags.Length)];
                }

                // set the hashtags into the re-used tweet object and send it.
                tweetDataWrapper.Data.Entities.Hashtags = hashtags;
            }

            // set the metrics to random values
            if (includeNonPublicMetrics)
            {
                tweetDataWrapper.Data.NonPublicMetrics.ImpressionCount = rand.Next(40);
            }

            if (includePublicMetrics)
            {
                tweetDataWrapper.Data.PublicMetrics.LikeCount = rand.Next(30);
                tweetDataWrapper.Data.PublicMetrics.RetweetClicks = rand.Next(20);
                tweetDataWrapper.Data.PublicMetrics.QuoteCount = rand.Next(10);
            }

            yield return tweetDataWrapper;

            count++;
        }
    }
}
