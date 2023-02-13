using Microsoft.AspNetCore.Mvc;

namespace JackHenry.TwitterScan.Service.Controllers;

[ApiController]
[Route("stats")]
public class TweetStatController : ControllerBase
{
    readonly ILogger<TweetStatController> _logger;

    public TweetStatController(
        ITweetStatRepository statRepo,
        ILogger<TweetStatController> logger)
    {
        _statRepo = statRepo;
        _logger = logger;
    }
    readonly ITweetStatRepository _statRepo;

    [HttpGet]
    [ProducesResponseType(typeof(TweetStats), 200)]
    public TweetStats Get()
    {
        return _statRepo.GetTweetStats();
    }
}