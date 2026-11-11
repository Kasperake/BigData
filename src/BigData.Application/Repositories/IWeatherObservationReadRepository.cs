using BigData.Application.GetWeatherObservations;

namespace BigData.Infrastructure.Persistence
{
    public interface IWeatherObservationReadRepository
    {
        Task<List<GetWeatherObservationsItem>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}