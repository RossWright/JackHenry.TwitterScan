using Microsoft.AspNetCore.Mvc;

namespace JackHenry.TwitterScan.Service.Controllers;

[ApiController]
[Route("metrics")]
public class TweetMetricsController : ControllerBase
{
    public TweetMetricsController(
        ITweetMetricsProcessor tweetMetricsProcessor,
        ILogger<TweetMetricsController> logger)
    {
        _tweetMetricsProcessor = tweetMetricsProcessor;
        _logger = logger;
    }
    readonly ITweetMetricsProcessor _tweetMetricsProcessor;
    readonly ILogger<TweetMetricsController> _logger;

    [HttpGet]
    [ProducesResponseType(typeof(TweetMetricsStatistics), 200)]
    public TweetMetricsStatistics Get() =>
        _tweetMetricsProcessor.GetMetricsStats();
}