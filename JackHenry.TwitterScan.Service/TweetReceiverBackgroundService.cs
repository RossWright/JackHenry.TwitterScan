namespace JackHenry.TwitterScan.Service;
public class TweetReceiverBackgroundService : BackgroundService
{
    public TweetReceiverBackgroundService(IServiceProvider serviceProvider, 
        ILogger<TweetReceiverBackgroundService> logger) => 
        (_serviceProvider, _logger) = 
        ( serviceProvider,  logger);
    readonly IServiceProvider _serviceProvider;
    readonly ILogger<TweetReceiverBackgroundService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Tweet Receiver Background Service is running.");
        
        using var serviceScope = _serviceProvider.CreateScope();
        var receiver = serviceScope.ServiceProvider.GetRequiredService<ITweetReceiver>();

        await receiver.OpenStream(stoppingToken);

        _logger.LogInformation("Tweet Receiver Background Service has stopped.");
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Tweet Receiver Background Service is stopping.");
        return base.StopAsync(cancellationToken);
    }
}
