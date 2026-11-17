using BigData.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BigData.Infrastructure.BackgroundServices
{
    public class MongoDbTransformationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MongoDbTransformationWorker> _logger;

        public MongoDbTransformationWorker(IServiceScopeFactory scopeFactory, ILogger<MongoDbTransformationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MongoDbTransformationWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await TransformMongoToSqlAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning("MongoDbTransformationWorker canceled.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Mongo to SQL transformation failed.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }

            _logger.LogInformation("MongoDbTransformationWorker stopped.");
        }

        private async Task TransformMongoToSqlAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var mongoDb = scope.ServiceProvider.GetRequiredService<WeatherMongoDbContext>();
            var sqlDb = scope.ServiceProvider.GetRequiredService<WeatherDWDbContext>();

            var insertedTotal = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var mongoBatch = await mongoDb.WeatherObservations
                    .AsNoTracking()
                    .OrderBy(x => x.CreatedAtUtc)
                    .ToListAsync(cancellationToken);

                if (mongoBatch.Count == 0)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Mongo to SQL sync completed. Inserted rows: {InsertedTotal}", insertedTotal);
        }
    }
}