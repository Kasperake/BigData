using BigData.UI.Clients.Models;

namespace BigData.UI.Clients
{
    public interface IWeatherClient
    {
        Task<GetWeatherObservationsItemDTO> GetAsync(CancellationToken ct);
    }
}