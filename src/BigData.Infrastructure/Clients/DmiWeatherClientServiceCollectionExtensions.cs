using BigData.Application.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BigData.Infrastructure.Clients;

public static class DmiWeatherClientServiceCollectionExtensions
{
    public static IServiceCollection AddDmiWeatherClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DmiWeatherClientOptions>(configuration.GetSection(DmiWeatherClientOptions.SectionName));

        services.AddHttpClient<IDmiWeatherClient, DmiWeatherClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<DmiWeatherClientOptions>>().Value;
            http.BaseAddress = new Uri(options.BaseAddress);
        });

        return services;
    }
}
