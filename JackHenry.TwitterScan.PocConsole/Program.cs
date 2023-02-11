using JackHenry.TwitterScan;
using System.Text.Json;

int rate = 0;
do
{
    Console.Write("Tweets Per Second (default 5600): ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        rate = 5600;
    }
    else if (!int.TryParse(input, out rate) || rate < 1 || rate > 1500000)
    {
        rate = 0;
        Console.WriteLine("Invalid Rate (must be between 1 and 1,500,000");
    }
}while (rate == 0);

using HttpClient httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite)
};

////var requestUri = "https://api.twitter.com/2/tweets/sample/stream?tweet.fields=entities";
var requestUri = $"https://localhost:7260/stream?rate={rate}";

using var stream = await httpClient.GetStreamAsync(requestUri);

Console.WriteLine("Elapsed      Count        Rate");
var back = new string('\b', 30);
Console.Write(new string('*', 30));

var jsonOpts = new JsonSerializerOptions 
{ 
    PropertyNameCaseInsensitive= true,
};
using var reader = new StreamReader(stream);

var count = 0;
var start = DateTime.UtcNow.Ticks;
var step = rate > 1000 ? ( rate > 100000 ? 1000000 : 1000) : 1;
var stepUnit = rate > 1000 ? (rate > 100000 ? "M" : "K") : " ";

await foreach (var tweet in JsonSerializer.DeserializeAsyncEnumerable<Tweet>(stream, jsonOpts))
{
    count++;
    if (count % step == 0)
    {
        var sec = (double)(DateTime.UtcNow.Ticks - start) / TimeSpan.TicksPerSecond;
        Console.Write($"{back}{(int)sec,6}s  {count/step,8}{stepUnit}  {(int)(count / sec),8}/s");
    }
}