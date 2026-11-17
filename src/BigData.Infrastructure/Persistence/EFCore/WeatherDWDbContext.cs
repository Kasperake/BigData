using BigData.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BigData.Infrastructure.Persistence.EFCore;

internal class WeatherDWDbContext : DbContext
{
    public WeatherDWDbContext(DbContextOptions<WeatherDWDbContext> options)
    : base(options)
    {
    }

    public DbSet<StormMeasurement> StormMeasurements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WeatherDWDbContext).Assembly);
    }
}
