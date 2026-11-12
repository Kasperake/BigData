using System.Net.Http.Json;
using BigData.Application.Clients;

namespace BigData.Infrastructure.Clients;

public class DmiWeatherClient(HttpClient client) : IDmiWeatherClient
{
    public async Task<IReadOnlyList<ObservationInput>> FetchAsync(CancellationToken cancellationToken = default)
    {
        var response = await client.GetFromJsonAsync<DmiResponse>("collections/observation/items", cancellationToken);

        return response?.Features.Select(f => new ObservationInput(
            f.Id,
            f.Properties.StationId,
            f.Properties.ParameterId,
            f.Properties.Observed.UtcDateTime,
            f.Properties.Created.UtcDateTime,
            f.Properties.Value,
            f.Geometry?.Coordinates is [var lon, _, ..] ? lon : null,
            f.Geometry?.Coordinates is [_, var lat, ..] ? lat : null)).ToList() ?? [];
    }
}
