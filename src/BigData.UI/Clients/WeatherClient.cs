using BigData.UI.Clients.Models;
using System.Text.Json;

namespace BigData.UI.Clients
{
    public sealed class WeatherClient(HttpClient client) : IWeatherClient
    {
        public async Task<GetWeatherObservationsResponse> GetAsync(int page, int pageSize, CancellationToken ct = default)
        {
            return await client.GetFromJsonAsync<GetWeatherObservationsResponse>($"api/weather?page={page}&pageSize={pageSize}", ct);
        }
    }
}
