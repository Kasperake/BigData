using BigData.Application.Clients;
using BigData.Domain.Entities;
using BigData.Domain.Repositories;
using Cqrs.Abstractions;

namespace BigData.Application.UpsertWeatherObservations;

public class UpsertWeatherObservationsCommandHandler(
    IDmiWeatherClient client,
    IWeatherObservationRepository repository) : ICommandHandler<UpsertWeatherObservationsCommand>
{
    public async Task HandleAsync(UpsertWeatherObservationsCommand command, CancellationToken cancellationToken)
    {
        var observations = await client.FetchAsync(cancellationToken);

        foreach (var o in observations)
        {
            var observation = WeatherObservation.Create(
                o.Id,
                o.StationId,
                o.ParameterId,
                o.ObservedAtUtc,
                o.CreatedAtUtc,
                o.Value,
                o.Longitude,
                o.Latitude);

            await repository.UpsertAsync(observation, cancellationToken);
        }
    }
}
