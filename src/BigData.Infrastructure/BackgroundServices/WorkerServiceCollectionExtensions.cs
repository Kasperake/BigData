using Microsoft.Extensions.DependencyInjection;

namespace BigData.Infrastructure.BackgroundServices;

public static class WorkerServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        services.AddHostedService<WeatherImportWorker>();
        return services;
    }
}
