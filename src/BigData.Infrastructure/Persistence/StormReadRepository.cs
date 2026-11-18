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
}
