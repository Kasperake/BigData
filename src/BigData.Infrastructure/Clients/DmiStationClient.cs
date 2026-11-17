using System.Net.Http.Json;
using BigData.Application.Clients;

namespace BigData.Infrastructure.Clients;

public class DmiStationClient(HttpClient client) : IDmiStationClient
{
    private const int PageSize = 1000;

    public async Task<IReadOnlyList<StationInput>> FetchAsync(CancellationToken cancellationToken = default)
    {
        var features = new List<DmiStationFeature>();
        var offset = 0;

        while (true)
        {
            var response = await client.GetFromJsonAsync<DmiStationResponse>(
                $"collections/station/items?limit={PageSize}&offset={offset}",
                cancellationToken);

            var page = response?.Features ?? [];
            features.AddRange(page);

            if (page.Count < PageSize)
            {
                break;
            }

            offset += PageSize;
        }

        return features
            .GroupBy(f => f.Properties.StationId)
            .Select(g => g
                .OrderBy(f => f.Properties.ValidTo is null ? 0 : 1)
                .ThenByDescending(f => f.Properties.ValidFrom)
                .First())
            .Select(f => new StationInput(
                f.Id,
                f.Properties.StationId,
                f.Properties.Country ?? string.Empty,
                f.Properties.Name ?? string.Empty))
            .ToList();
    }
}
