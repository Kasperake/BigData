using BigData.UI.Clients.Models;

namespace BigData.UI.Clients
{
    public sealed class WeatherClient(HttpClient client) : IWeatherClient
    {
        public Task<GetWeatherObservationsItemDTO> GetAsync(CancellationToken ct) => client.GetFromJsonAsync<GetWeatherObservationsItemDTO>("api/weather?page=1&pageSize=50", ct);
    }
}
