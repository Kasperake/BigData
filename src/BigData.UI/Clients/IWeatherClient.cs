using BigData.UI.Clients.Models;

namespace BigData.UI.Clients
{
    public interface IWeatherClient
    {
        Task<GetWeatherObservationsResponse> GetAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    }
}