using BigData.Domain.Entities;

namespace BigData.Domain.Repositories;

public interface IWeatherObservationRepository
{
    Task Add(WeatherObservation observation, CancellationToken cancellationToken);
}
