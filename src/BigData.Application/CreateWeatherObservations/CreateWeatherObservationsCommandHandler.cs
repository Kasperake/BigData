using BigData.Application.Clients;
using BigData.Domain.Entities;
using BigData.Domain.Repositories;
using Cqrs.Abstractions;

namespace BigData.Application.CreateWeatherObservations;

public class CreateWeatherObservationsCommandHandler(
    IDmiWeatherClient client,
    IWeatherObservationRepository repository) : ICommandHandler<CreateWeatherObservationsCommand>
{
    public async Task HandleAsync(CreateWeatherObservationsCommand command, CancellationToken cancellationToken)
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

            await repository.InsertIfExistsAsync(observation, cancellationToken);
        }
    }
}
