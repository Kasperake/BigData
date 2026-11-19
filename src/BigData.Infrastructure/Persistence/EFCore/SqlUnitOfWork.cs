using BigData.Application.Abstractions;

namespace BigData.Infrastructure.Persistence.EFCore;

internal sealed class SqlUnitOfWork(WeatherDWDbContext dbContext) : ISqlUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
