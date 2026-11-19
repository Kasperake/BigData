using BigData.Domain.Entities;

namespace BigData.Domain.Repositories;

public interface IStationRepository
{
    Task<List<Station>> GetByStationIdsAsync(IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken);
    Task Add(Station station, CancellationToken cancellationToken);
}
