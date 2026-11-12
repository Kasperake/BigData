using BigData.Domain.Entities;
using BigData.Domain.Repositories;
using BigData.Infrastructure.Persistence.EFCore;

namespace BigData.Infrastructure.Persistence;

public class WeatherObservationRepository(WeatherDbContext dbContext) : IWeatherObservationRepository
{
    public async Task UpsertAsync(WeatherObservation observation, CancellationToken cancellationToken)
    {
        var existing = await dbContext.WeatherObservations.FindAsync([observation.Id], cancellationToken);

        if (existing is null)
        {
            dbContext.WeatherObservations.Add(observation);
            return;
        }

        dbContext.Entry(existing).CurrentValues.SetValues(observation);
    }
}
