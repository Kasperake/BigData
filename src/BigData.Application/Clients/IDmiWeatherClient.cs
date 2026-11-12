namespace BigData.Application.Clients;

public interface IDmiWeatherClient
{
    Task<IReadOnlyList<ObservationInput>> FetchAsync(CancellationToken cancellationToken = default);
}
