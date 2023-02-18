using JackHenry.TwitterScan;
using JackHenry.TwitterScan.PocConsole;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetParent(AppContext.BaseDirectory)!.FullName)
            .AddJsonFile("appsettings.json", false)
            .Build();

// Prompt user for tweet rate to be passed to the mock emitter
int rate = 0;
do
{
    Console.Write("Tweets Per Second (leave blank to use twitter): ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        rate = -1;
    }
    else if (!int.TryParse(input, out rate) || rate < 1 || rate > TimeSpan.TicksPerSecond)
    {
        rate = 0;
        Console.WriteLine($"Invalid Rate (must be between 1 and {TimeSpan.TicksPerSecond:0,0}");
    }
}while (rate == 0);

var siteCfg = new SiteConfig();
string requestUri;
if (rate == -1)
{
    configuration.Bind("TwitterApi", siteCfg);
    requestUri = siteCfg.Url;
}
else
{
    configuration.Bind("Emitter", siteCfg);
    requestUri = $"{siteCfg.Url}?rate={rate}";
}

// output the header and prepare variables for loop
Console.WriteLine("Elapsed      Count        Rate");
var backspaces = new string('\b', 30);
Console.Write(new string('*', 30));

//limit how often the UI is updated to reduce performance impact from console output
var tweetsPerUiUpdate = rate > 1000 ? ( rate > 100000 ? 1000000 : 1000) : 1;   
var tweetUnits = rate > 1000 ? (rate > 100000 ? "M" : "K") : " ";

// open the stream
using HttpClient httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite)
};

httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", siteCfg.Jwt);

using var stream = await httpClient.GetStreamAsync(requestUri);
var jsonStream = JsonSerializer.DeserializeAsyncEnumerable<TweetDataWrapper>(stream,
    new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    });

// Prepare variables used in the loop
var count = 0;
var start = DateTime.UtcNow.Ticks;

// process the stream
await foreach (var tweet in jsonStream)
{
    count++;
    if (count % tweetsPerUiUpdate == 0)
    {
        var secondsElapsed = (double)(DateTime.UtcNow.Ticks - start) / TimeSpan.TicksPerSecond;
        Console.Write($"{backspaces}{(int)secondsElapsed,6}s  {count / tweetsPerUiUpdate,8}{tweetUnits}  {(int)(count / secondsElapsed),8}/s");
    }
}