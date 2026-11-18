using BigData.Application.GetMeasurementsForDisplay;
using BigData.Application.Repositories;
using BigData.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace BigData.Infrastructure.Persistence;

internal class StormReadRepository(WeatherDWDbContext dbContext) : IStormReadRepository
{
    public async Task<HashSet<Guid>> GetExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await dbContext.StormMeasurements
            .AsNoTracking()
            .Where(o => ids.Contains(o.Id))
            .Select(o => o.Id)
            .ToHashSetAsync(cancellationToken);
    }

    public async Task<List<WeatherDto>> DisplayData(CancellationToken cancellationToken)
    {
        return await (from m in dbContext.StormMeasurements
                      join s in dbContext.Stations on m.StationId equals s.StationId
                      into stations
                      from s in stations.DefaultIfEmpty()
                      select new WeatherDto
                      {
                          StationId = m.StationId,
                          StationName = s.Name,
                          ObservedAtUtc = m.ObservedAtUtc,
                          Parameters = m.Parameters.Select(p => new ParameterDto
                          {
                              Type = p.Type.ToString(),
                              Value = p.Value
                          }).ToList()
                      }).ToListAsync(cancellationToken);
    }
}