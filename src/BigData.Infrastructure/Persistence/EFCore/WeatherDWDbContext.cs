using BigData.Domain.Entities;
using BigData.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BigData.Infrastructure.Persistence.EFCore;

public class WeatherDWDbContext : DbContext
{
    public WeatherDWDbContext(DbContextOptions<WeatherDWDbContext> options)
    : base(options)
    {
    }

    public DbSet<StormMeasurement> StormMeasurements { get; set; } = null!;
    public DbSet<Station> Stations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new StormMeasurementConfiguration());
        modelBuilder.ApplyConfiguration(new StationConfiguration());
    }
}
