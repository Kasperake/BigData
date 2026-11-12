using BigData.Application.GetWeatherObservations;

namespace BigData.Infrastructure.Persistence;

public interface IWeatherObservationReadRepository
{
    Task<List<GetWeatherObservationsItem>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<HashSet<Guid>> GetExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
