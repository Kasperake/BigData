using BigData.Application.GetMeasurementsForDisplay;

namespace BigData.Application.Repositories
{
    public interface IStormReadRepository
    {
        Task<List<WeatherDto>> DisplayData(CancellationToken cancellationToken);
        Task<HashSet<Guid>> GetExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    }
}