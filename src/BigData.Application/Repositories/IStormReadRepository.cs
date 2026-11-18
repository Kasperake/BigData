namespace BigData.Application.Repositories
{
    public interface IStormReadRepository
    {
        Task<HashSet<Guid>> GetExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    }
}