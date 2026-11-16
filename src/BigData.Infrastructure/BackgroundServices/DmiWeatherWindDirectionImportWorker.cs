using BigData.Application.CreateWeatherObservations;
using Cqrs.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BigData.Infrastructure.BackgroundServices;

public class DmiWeatherWindDirectionImportWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DmiWeatherWindDirectionImportWorker> logger) : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await mediator.SendAsync(new CreateWeatherObservationsCommand("wind_dir"), stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (OperationCanceledException operationCanceledException)
        {
            logger.LogWarning("Weather import was canceled. {0}", operationCanceledException.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred while importing weather data.");
            logger.LogError(ex.ToString());
        }
    }
}

