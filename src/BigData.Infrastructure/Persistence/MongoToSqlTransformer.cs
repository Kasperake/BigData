using BigData.Application.TransformMongoToSql;
using BigData.Domain.Entities;
using BigData.Domain.Entities.Enums;
using BigData.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace BigData.Infrastructure.Persistence;

internal sealed class MongoToSqlTransformer(WeatherMongoDbContext mongoDb, WeatherDWDbContext sqlDb) : IMongoToSqlTransformer
{
    public async Task TransformAsync(CancellationToken cancellationToken)
    {
        var mongoRows = await mongoDb.WeatherObservations
            .AsNoTracking()
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var groups = mongoRows.GroupBy(x => new { x.StationId, x.ObservedAtUtc });

        foreach (var group in groups)
        {
            var stormMeasurement = await sqlDb.StormMeasurements
                .Include(x => x.Parameters)
                .FirstOrDefaultAsync(
                    x => x.StationId == group.Key.StationId &&
                         x.ObservedAtUtc == group.Key.ObservedAtUtc,
                    cancellationToken);

            if (stormMeasurement is null)
            {
                stormMeasurement = StormMeasurement.Create(
                    Guid.NewGuid(),
                    group.Key.StationId,
                    ParameterType.Temperature,
                    group.Key.ObservedAtUtc,
                    []);

                sqlDb.StormMeasurements.Add(stormMeasurement);
            }

            foreach (var row in group)
            {
                if (!TryMapParameterType(row.ParameterId, out var type))
                {
                    continue;
                }

                var existing = stormMeasurement.Parameters.FirstOrDefault(p => p.Type == type);
                if (existing is null)
                {
                    stormMeasurement.Parameters.Add(
                        Parameters.Create(Guid.NewGuid(), type, row.Value));
                }
                else if (!existing.Value.Equals(row.Value))
                {
                    existing.SetValue(row.Value);
                }
            }
        }

        await sqlDb.SaveChangesAsync(cancellationToken);
    }

    private static bool TryMapParameterType(string parameterId, out ParameterType type)
    {
        switch (parameterId.Trim().ToLowerInvariant())
        {
            case "wind_speed":
                type = ParameterType.WindSpeed;
                return true;
            case "wind_direction":
            case "wind_dir":
                type = ParameterType.WindDirection;
                return true;
            case "temp_dry":
            case "temperature":
                type = ParameterType.Temperature;
                return true;
            default:
                type = default;
                return false;
        }
    }
}