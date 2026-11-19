using BigData.Domain.Entities;
using BigData.Domain.Repositories;
using BigData.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace BigData.Infrastructure.Persistence;

public class StationRepository(WeatherDWDbContext dbContext) : IStationRepository
{
    public async Task<List<Station>> GetByStationIdsAsync(IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken)
    {
        if (stationIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Stations
            .Where(s => stationIds.Contains(s.StationId))
            .ToListAsync(cancellationToken);
    }

    public Task Add(Station station, CancellationToken cancellationToken)
    {
        dbContext.Stations.Add(station);
        return Task.CompletedTask;
    }
}
