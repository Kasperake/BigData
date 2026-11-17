using BigData.Domain.Entities;
using BigData.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace BigData.Infrastructure.Persistence.EFCore;

public class WeatherMongoDbContext : DbContext
{
    public WeatherMongoDbContext(DbContextOptions<WeatherMongoDbContext> options)
        : base(options)
    {
    }
    public DbSet<WeatherObservation> WeatherObservations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WeatherObservation>().ToCollection("WeatherObservations");
        // Stations live in SQL Server only (see WeatherDbContext).
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(WeatherMongoDbContext).Assembly,
            type => type != typeof(StationConfiguration));
    }
}
