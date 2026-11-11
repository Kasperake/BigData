using BigData.UI.Clients.Models;
using System.Text.Json;

namespace BigData.UI.Clients
{
    public class WeatherClient
    {
        public async Task<GetWeatherObservationsItemDTO?> GetWeatherForecastsAsync(HttpClient httpClient)
        {
            var response = await httpClient.GetAsync("https://localhost:5001/api/weatherobservations");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var weatherObservations = JsonSerializer.Deserialize<GetWeatherObservationsItemDTO>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return weatherObservations;
        }
    }
}
