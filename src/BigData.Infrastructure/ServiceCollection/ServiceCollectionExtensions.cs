using BigData.Application.Clients;
using BigData.Application.TransformMongoToSql;
using BigData.Domain.Repositories;
using BigData.Infrastructure.Abstractions;
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
        services.AddDbContext<WeatherDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionString is missing")));
        services.AddDbContext<WeatherMongoDbContext>(options =>
            options.UseMongoDB(configuration.GetConnectionString("MongoDbConnection") ?? throw new InvalidOperationException("ConnectionString is missing")));
        services.AddDbContext<WeatherDWDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DataWarehouseConnection") ?? throw new InvalidOperationException("ConnectionString is missing")));

        services.AddScoped<IMongoToSqlTransformer, MongoToSqlTransformer>();

        // Auto commit unitofwork command behavior. 
        services.AddScoped<ISqlUnitOfWork, SqlUnitOfWork>();
        services.AddScoped<IMongoDbUnitOfWork, MongoDbUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IMongoDbUnitOfWork>());
        //services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ISqlUnitOfWork>()); // This is only necessary for switching back to MSSQL. 
        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IWeatherObservationReadRepository, WeatherObservationMongoReadRepository>();
        services.AddScoped<IWeatherObservationRepository, WeatherObservationMongoRepository>();
        services.AddScoped<IStationRepository, StationRepository>();

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
