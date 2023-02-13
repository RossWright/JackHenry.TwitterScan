﻿using JackHenry.TwitterScan;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetParent(AppContext.BaseDirectory)!.FullName)
            .AddJsonFile("appsettings.json", false)
            .Build();
var accessToken = configuration["TwitterAccessToken"];

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
    else if (!int.TryParse(input, out rate) || rate < 1 || rate > 1500000)
    {
        rate = 0;
        Console.WriteLine("Invalid Rate (must be between 1 and 1,500,000");
    }
}while (rate == 0);

var requestUri = rate == -1
        ? "https://api.twitter.com/2/tweets/sample/stream?tweet.fields=entities"
        : $"https://localhost:7260/stream?rate={rate}";

// output the header and prepare variables for loop
Console.WriteLine("Elapsed      Count        Rate");
var backspaces = new string('\b', 30);
Console.Write(new string('*', 30));
var count = 0;
var jsonOpts = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
};
//limit how often the UI is updated to reduce performance impact from console output
var tweetsPerUiUpdate = rate > 1000 ? ( rate > 100000 ? 1000000 : 1000) : 1;   
var tweetUnits = rate > 1000 ? (rate > 100000 ? "M" : "K") : " ";

// open the stream
using HttpClient httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite)
};
if (accessToken != null)
{
    httpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", accessToken);
}
using var stream = await httpClient.GetStreamAsync(requestUri);
using var reader = new StreamReader(stream);

// process the stream
var start = DateTime.UtcNow.Ticks;
await foreach (var tweet in JsonSerializer.DeserializeAsyncEnumerable<Tweet>(stream, jsonOpts))
{
    count++;
    if (count % tweetsPerUiUpdate == 0)
    {
        var secondsElapsed = (double)(DateTime.UtcNow.Ticks - start) / TimeSpan.TicksPerSecond;
        Console.Write($"{backspaces}{(int)secondsElapsed,6}s  {count / tweetsPerUiUpdate,8}{tweetUnits}  {(int)(count / secondsElapsed),8}/s");
    }
}