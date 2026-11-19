using BigData.Application.Abstractions;

namespace BigData.Infrastructure.Persistence.EFCore;

internal sealed class MongoUnitOfWork(WeatherMongoDbContext dbContext) : IMongoUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
