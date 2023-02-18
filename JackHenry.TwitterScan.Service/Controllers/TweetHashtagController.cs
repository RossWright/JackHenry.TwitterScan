using Microsoft.AspNetCore.Mvc;

namespace JackHenry.TwitterScan.Service.Controllers;

[ApiController]
[Route("hashtags")]
public class TweetHashtagController : ControllerBase
{
    public TweetHashtagController(
        ITweetHashtagProcessor tweetHashtagProcessor,
        ILogger<TweetHashtagController> logger)
    {
        _tweetHashtagProcessor = tweetHashtagProcessor;
        _logger = logger;
    }
    readonly ITweetHashtagProcessor _tweetHashtagProcessor;
    readonly ILogger<TweetHashtagController> _logger;

    [HttpGet]
    [ProducesResponseType(typeof(TweetHashtagStatistics), 200)]
    public TweetHashtagStatistics Get() =>
        _tweetHashtagProcessor.GetHashtagStats();
}
