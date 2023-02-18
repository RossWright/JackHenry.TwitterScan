namespace JackHenry.TwitterScan.Service;

public class TwitterStreamReaderBackgroundService : BackgroundService
{
    public TwitterStreamReaderBackgroundService(IServiceProvider serviceProvider,
        ILogger<TwitterStreamReaderBackgroundService> logger) =>
        (_serviceProvider, _logger) =
        (serviceProvider, logger);
    readonly IServiceProvider _serviceProvider;
    readonly ILogger<TwitterStreamReaderBackgroundService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Twitter Stream Reader Background Service is running.");

        using var serviceScope = _serviceProvider.CreateScope();
        var receiver = serviceScope.ServiceProvider.GetRequiredService<ITwitterStreamReaderService>();

        await receiver.OpenStream(stoppingToken);

        _logger.LogInformation("Twitter Stream Reader Background Service has stopped.");
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Twitter Stream Reader Background Service is stopping.");
        return base.StopAsync(cancellationToken);
    }
}
