using BigData.Application.Abstractions;
using BigData.Application.Clients;
using BigData.Application.Repositories;
using BigData.Domain.Repositories;
using BigData.Infrastructure.BackgroundServices;
using BigData.Infrastructure.Clients;
using BigData.Infrastructure.Persistence;
using BigData.Infrastructure.Persistence.EFCore;
using Cqrs.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BigData.Infrastructure.ServiceCollection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddWorkerServices(services);
        AddPersistence(services, configuration);
        AddRepositories(services);
        AddDmiWeatherClient(services, configuration);

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WeatherMongoDbContext>(options =>
            options.UseMongoDB(configuration.GetConnectionString("MongoDbConnection") ?? throw new InvalidOperationException("ConnectionString is missing")));
        services.AddDbContext<WeatherDWDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionString is missing")));

        // Handlers persist explicitly through these. Do NOT register Cqrs' IUnitOfWork: the pipeline would then
        // wrap every command in a transaction, and with several registrations it silently picks the last one.
        services.AddScoped<IMongoUnitOfWork, MongoUnitOfWork>();
        services.AddScoped<ISqlUnitOfWork, SqlUnitOfWork>();

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IWeatherObservationReadRepository, WeatherObservationMongoReadRepository>();
        services.AddScoped<IWeatherObservationRepository, WeatherObservationMongoRepository>();
        services.AddScoped<IStationRepository, StationRepository>();
        services.AddScoped<IStormRepository, StormRepository>();
        services.AddScoped<IStormReadRepository, StormReadRepository>();

        return services;
    }


    private static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        services.AddHostedService<DmiWeatherTempImportWorker>();
        services.AddHostedService<DmiWeatherPressureImportWorker>();
        services.AddHostedService<DmiWeatherWindDirectionImportWorker>();
        services.AddHostedService<DmiWeatherWindSpeedImportWorker>();
        services.AddHostedService<MongoDbTransformationWorker>();
        services.AddHostedService<DmiStationImportWorker>();
        return services;
    }


    private static IServiceCollection AddDmiWeatherClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DmiWeatherClientOptions>(configuration.GetSection(DmiWeatherClientOptions.SectionName));

        services.AddHttpClient<IDmiWeatherClient, DmiWeatherClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<DmiWeatherClientOptions>>().Value;
            http.BaseAddress = new Uri(options.BaseAddress);
        });

        services.AddHttpClient<IDmiStationClient, DmiStationClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<DmiWeatherClientOptions>>().Value;
            http.BaseAddress = new Uri(options.BaseAddress);
        });

        return services;
    }
}
