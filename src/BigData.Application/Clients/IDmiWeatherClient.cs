namespace BigData.Application.Clients;

public interface IDmiWeatherClient
{
    Task<IReadOnlyList<ObservationInput>> FetchAsync(string parameterId, CancellationToken cancellationToken = default);
}
