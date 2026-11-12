using BigData.Domain.Entities;

namespace BigData.Domain.Repositories;

public interface IWeatherObservationRepository
{
    Task UpsertAsync(WeatherObservation observation, CancellationToken cancellationToken);
}
