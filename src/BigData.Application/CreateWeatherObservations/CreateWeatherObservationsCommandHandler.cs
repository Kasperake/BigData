using BigData.Application.Clients;
using BigData.Application.Repositories;
using BigData.Domain.Entities;
using BigData.Domain.Repositories;
using Cqrs.Abstractions;

namespace BigData.Application.CreateWeatherObservations;

public class CreateWeatherObservationsCommandHandler(
    IDmiWeatherClient client,
    IWeatherObservationRepository repository,
    IWeatherObservationReadRepository readRepository) : ICommandHandler<CreateWeatherObservationsCommand>
{

    public async Task HandleAsync(CreateWeatherObservationsCommand command, CancellationToken cancellationToken)
    {
        var observations = await client.FetchAsync(command.ParameterId, cancellationToken);

        var existingIds = await readRepository.GetExistingIdsAsync(
            observations.Select(o => o.Id).Distinct().ToList(),
            cancellationToken);

        foreach (var o in observations.DistinctBy(o => o.Id))
        {
            if (existingIds.Contains(o.Id))
            {
                continue;
            }

            var observation = WeatherObservation.Create(
                o.Id,
                o.StationId,
                o.ParameterId,
                o.ObservedAtUtc,
                o.CreatedAtUtc,
                o.Value,
                o.Longitude,
                o.Latitude);

            await repository.Add(observation, cancellationToken);
        }
    }
}
