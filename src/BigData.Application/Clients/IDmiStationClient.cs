namespace BigData.Application.Clients;

public interface IDmiStationClient
{
    Task<IReadOnlyList<StationInput>> FetchAsync(CancellationToken cancellationToken = default);
}
