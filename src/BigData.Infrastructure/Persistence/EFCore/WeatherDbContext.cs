using BigData.Domain.Entities;
using Cqrs.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BigData.Infrastructure.Persistence.EFCore;

public class WeatherDbContext : DbContext, IUnitOfWork
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options)
        : base(options)
    {
    }
    

    public DbSet<WeatherObservation> WeatherObservations { get; set; }

    Task IUnitOfWork.CommitAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WeatherDbContext).Assembly);
    }
}
