using BigData.Domain.Entities;
using BigData.Domain.Repositories;
using BigData.Infrastructure.Persistence.EFCore;

namespace BigData.Infrastructure.Persistence;

public class WeatherObservationRepository(WeatherDbContext dbContext) : IWeatherObservationRepository
{
    public Task Add(WeatherObservation observation, CancellationToken cancellationToken)
    {
        dbContext.WeatherObservations.Add(observation);
        return Task.CompletedTask;
    }
}
