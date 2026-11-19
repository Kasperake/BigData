using BigData.UI.Clients.Models;
using System.Text.Json;

namespace BigData.UI.Clients;

public sealed class WeatherClient(HttpClient client) : IWeatherClient
{
    public async Task<GetMeasurementsForDisplayResponse> GetForDisplayAsync(CancellationToken ct = default)
    {
        return await client.GetFromJsonAsync<GetMeasurementsForDisplayResponse>($"api/weather/GetForDisplay", ct) ?? new GetMeasurementsForDisplayResponse();
    }

}
