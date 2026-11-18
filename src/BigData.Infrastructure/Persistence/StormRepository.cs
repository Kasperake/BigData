using BigData.Application.Repositories;
using BigData.Domain.Entities;
using BigData.Infrastructure.Persistence.EFCore;

namespace BigData.Infrastructure.Persistence;

internal class StormRepository(WeatherDWDbContext dbContext) : IStormRepository
{
    public Task Add(StormMeasurement entity)
    {
        dbContext.Add(entity);
        return Task.CompletedTask;
    }
}

