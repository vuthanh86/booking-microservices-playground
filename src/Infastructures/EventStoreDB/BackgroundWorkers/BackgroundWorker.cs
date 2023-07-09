using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.EventStoreDB.BackgroundWorkers;

public class BackgroundWorker : BackgroundService
{
    private readonly ILogger<BackgroundWorker> _logger;
    private readonly Func<CancellationToken, Task> _perform;

    public BackgroundWorker(
        ILogger<BackgroundWorker> logger,
        Func<CancellationToken, Task> perform
    )
    {
        this._logger = logger;
        this._perform = perform;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            await Task.Yield();
            _logger.LogInformation("Background worker stopped");
            await _perform(stoppingToken);
            _logger.LogInformation("Background worker stopped");
        }, stoppingToken);
    }
}
