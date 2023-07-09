using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.MessageProcessor;

public class PersistMessageBackgroundService : BackgroundService
{
    private readonly ILogger<PersistMessageBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    [CanBeNull] private Task _executingTask;
    private readonly PersistMessageOptions _options;

    public PersistMessageBackgroundService(
        ILogger<PersistMessageBackgroundService> logger,
        IServiceProvider serviceProvider,
        IOptions<PersistMessageOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PersistMessage Background Service Start");

        _executingTask = ProcessAsync(stoppingToken);

        return _executingTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PersistMessage Background Service Stop");

        return base.StopAsync(cancellationToken);
    }

    private async Task ProcessAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using (AsyncServiceScope scope = _serviceProvider.CreateAsyncScope())
            {
                IPersistMessageProcessor service = scope.ServiceProvider.GetRequiredService<IPersistMessageProcessor>();
                await service.ProcessAllAsync(stoppingToken);
            }

            TimeSpan delay = _options.Interval is not null
                                 ? TimeSpan.FromSeconds((int)_options.Interval)
                                 : TimeSpan.FromSeconds(30);

            await Task.Delay(delay, stoppingToken);
        }
    }
}
