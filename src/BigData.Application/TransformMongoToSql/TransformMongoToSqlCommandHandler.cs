using BigData.Application.Abstractions;
using BigData.Application.Repositories;
using BigData.Domain.Entities;
using BigData.Domain.Entities.Enums;
using Cqrs.Abstractions;

namespace BigData.Application.TransformMongoToSql;

public sealed class TransformMongoToSqlCommandHandler(IWeatherObservationReadRepository datalakeReadRepository, IStormReadRepository stormReadRepository, IStormRepository repository, ISqlUnitOfWork unitOfWork) : ICommandHandler<TransformMongoToSqlCommand>
{
    public async Task HandleAsync(TransformMongoToSqlCommand command, CancellationToken cancellationToken)
    {
        var items = await datalakeReadRepository
            .GetAllAsync(cancellationToken);

        var existingIds = await stormReadRepository.GetExistingIdsAsync(
            items.Select(x => x.Id).Distinct().ToList(),
            cancellationToken);

        foreach (var o in items.DistinctBy(x => x.Id))
        {
            if (existingIds.Contains(o.Id))
            {
                continue;
            }
            var observation = StormMeasurement.Create(
                o.Id,
                o.StationId,
                o.ObservedAtUtc,
                new List<Parameter>
                {
                    Parameter.Create(Guid.CreateVersion7(), o.ParameterId, o.Value)
                });
            await repository.Add(observation, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}