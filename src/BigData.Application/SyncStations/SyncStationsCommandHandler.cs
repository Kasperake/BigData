using BigData.Application.Abstractions;
using BigData.Application.Clients;
using BigData.Domain.Entities;
using BigData.Domain.Repositories;
using Cqrs.Abstractions;

namespace BigData.Application.SyncStations;

public class SyncStationsCommandHandler(
    IDmiStationClient client,
    IStationRepository repository,
    ISqlUnitOfWork unitOfWork) : ICommandHandler<SyncStationsCommand>
{
    public async Task HandleAsync(SyncStationsCommand command, CancellationToken cancellationToken)
    {
        var stations = (await client.FetchAsync(cancellationToken))
            .DistinctBy(s => s.StationId)
            .ToList();

        if (stations.Count == 0)
        {
            return;
        }

        var existing = (await repository.GetByStationIdsAsync(
                stations.Select(s => s.StationId).ToList(),
                cancellationToken))
            .ToDictionary(s => s.StationId);

        foreach (var s in stations)
        {
            if (existing.TryGetValue(s.StationId, out var station))
            {
                station.Update(s.CountryCode, s.Name);
                continue;
            }

            await repository.Add(Station.Create(s.Id, s.StationId, s.CountryCode, s.Name), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
