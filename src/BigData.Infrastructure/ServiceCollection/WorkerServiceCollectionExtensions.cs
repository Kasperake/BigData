using BigData.Infrastructure.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;

namespace BigData.Infrastructure.ServiceCollection;

public static class WorkerServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        services.AddHostedService<DmiWeatherTempImportWorker>();
        services.AddHostedService<DmiWeatherPressureImportWorker>();
        services.AddHostedService<DmiWeatherWindDirectionImportWorker>();
        services.AddHostedService<DmiWeatherWindSpeedImportWorker>();
        return services;
    }
}
