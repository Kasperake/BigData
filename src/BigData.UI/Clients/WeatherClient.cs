using BigData.UI.Clients.Models;

namespace BigData.UI.Clients
{
    public sealed class WeatherClient(HttpClient client) : IWeatherClient
    {
        public async Task<GetWeatherObservationsItemDTO> GetAsync(int page, int pageSize, CancellationToken ct = default)
        {
            return await client.GetFromJsonAsync<GetWeatherObservationsItemDTO>($"api/weather?page={page}&pageSize={pageSize}", ct);
        }
    }
}
