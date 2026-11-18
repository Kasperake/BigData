using BigData.Application.TransformMongoToSql;
using Cqrs.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BigData.Infrastructure.BackgroundServices
{
    public class MongoDbTransformationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<MongoDbTransformationWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("MongoDbTransformationWorker started.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    await mediator.SendAsync(new TransformMongoToSqlCommand(), stoppingToken);
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("MongoDbTransformationWorker canceled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Mongo to SQL transformation failed.");
            }

            logger.LogInformation("MongoDbTransformationWorker stopped.");
        }
    }
}