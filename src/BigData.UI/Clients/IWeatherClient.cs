using BigData.UI.Clients.Models;

namespace BigData.UI.Clients
{
    public interface IWeatherClient
    {
        Task<GetMeasurementsForDisplayResponse> GetForDisplayAsync(CancellationToken ct = default);
    }
}