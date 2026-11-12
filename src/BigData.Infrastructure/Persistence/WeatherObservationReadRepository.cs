using BigData.Application.GetWeatherObservations;
using BigData.Domain.Entities;
using BigData.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace BigData.Infrastructure.Persistence;

public class WeatherObservationReadRepository(WeatherDbContext dbContext) : IWeatherObservationReadRepository
{
    public async Task<List<GetWeatherObservationsItem>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await dbContext.WeatherObservations
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new GetWeatherObservationsItem
            {
                Id = x.Id,
                StationId = x.StationId,
                ParameterId = x.ParameterId,
                ObservedAtUtc = x.ObservedAtUtc,
                CreatedAtUtc = x.CreatedAtUtc,
                Value = x.Value,
                Longitude = x.Longitude,
                Latitude = x.Latitude
            })
            .OrderByDescending(x => x.ObservedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.WeatherObservations.CountAsync(cancellationToken);
    }
}
