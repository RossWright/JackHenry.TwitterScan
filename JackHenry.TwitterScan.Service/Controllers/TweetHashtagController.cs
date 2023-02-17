using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace JackHenry.TwitterScan.Service.Controllers;

[ApiController]
[Route("hashtags")]
[ExcludeFromCodeCoverage] // Testing the plumbing of ASP.NET has diminishing returns
public class TweetHashtagController : ControllerBase
{
    readonly ILogger<TweetHashtagController> _logger;

    public TweetHashtagController(
        ITweetHashtagProcessor tweetHashtagProcessor,
        ILogger<TweetHashtagController> logger)
    {
        _tweetHashtagProcessor = tweetHashtagProcessor;
        _logger = logger;
    }
    readonly ITweetHashtagProcessor _tweetHashtagProcessor;

    [HttpGet]
    [ProducesResponseType(typeof(TweetHashtagStatistics), 200)]
    public TweetHashtagStatistics Get() =>
        _tweetHashtagProcessor.GetTweetStats();
}