using BigData.Application.GetWeatherObservations;

namespace BigData.Application.Repositories;

public interface IWeatherObservationReadRepository
{
    Task<List<GetWeatherObservationsItem>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<HashSet<Guid>> GetExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<List<GetWeatherObservationsItem>> GetAllAsync(CancellationToken cancellationToken = default);
}
