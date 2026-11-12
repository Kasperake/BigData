using BigData.Domain.Entities;

namespace BigData.Domain.Repositories;

public interface IWeatherObservationRepository
{
    Task InsertIfExistsAsync(WeatherObservation observation, CancellationToken cancellationToken);
}
