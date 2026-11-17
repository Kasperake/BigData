using BigData.Application.SyncStations;
using Cqrs.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BigData.Infrastructure.BackgroundServices;

public class DmiStationImportWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DmiStationImportWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await mediator.SendAsync(new SyncStationsCommand(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while importing station data.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
