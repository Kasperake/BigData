using BigData.Application.Repositories;
using BigData.Domain.Entities;
using BigData.Infrastructure.Persistence.EFCore;

namespace BigData.Infrastructure.Persistence;

internal class StormRepository(WeatherDWDbContext dbContext) : IStormRepository
{
    public Task Add(StormMeasurement entity, CancellationToken cancellationToken = default)
    {
        dbContext.Add(entity);
        return Task.CompletedTask;
    }
}

