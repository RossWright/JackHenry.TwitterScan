using JackHenry.TwitterScan.Service.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace JackHenry.TwitterScan.Service.Controllers;

[ApiController]
[Route("stats")]
[ExcludeFromCodeCoverage] // Testing the plumbing of ASP.NET has diminishing returns
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