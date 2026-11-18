using BigData.Application.GetWeatherObservations;
using BigData.Application.Repositories;
using BigData.Domain.Entities;
using BigData.Domain.Entities.Enums;
using BigData.Domain.Repositories;
using Cqrs.Abstractions;

namespace BigData.Application.TransformMongoToSql;

public sealed class TransformMongoToSqlCommandHandler(IWeatherObservationReadRepository readRepository, IWeatherObservationRepository repository) : ICommandHandler<TransformMongoToSqlCommand>
{
    public async Task HandleAsync(TransformMongoToSqlCommand command, CancellationToken cancellationToken)
    {
        var items = await readRepository
            .GetAllAsync(cancellationToken);

        var entities = items.Select(x => StormMeasurement.Create(
            Guid.NewGuid(),
            x.StationId,
            ParameterType.Temperature,
            x.ObservedAtUtc,
            new List<Parameters>
            {
                Parameters.Create(Guid.NewGuid(), ParameterType.Temperature, x.Value)
            }));



    }
}