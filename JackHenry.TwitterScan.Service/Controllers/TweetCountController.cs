using Microsoft.AspNetCore.Mvc;

namespace JackHenry.TwitterScan.Service.Controllers;

[ApiController]
[Route("count")]
public class TweetCountController : ControllerBase
{
    public TweetCountController(
        ITweetCountProcessor tweetHashtagProcessor,
        ILogger<TweetCountController> logger)
    {
        _tweetCountProcessor = tweetHashtagProcessor;
        _logger = logger;
    }
    readonly ITweetCountProcessor _tweetCountProcessor;
    readonly ILogger<TweetCountController> _logger;

    [HttpGet]
    [ProducesResponseType(typeof(TweetCountStatistics), 200)]
    public TweetCountStatistics Get() =>
        _tweetCountProcessor.GetCountStats();
}