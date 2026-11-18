using BigData.Domain.Entities;
using BigData.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BigData.Infrastructure.Persistence.EFCore;

[Obsolete("This will deprecate - Look at WeatherMongoDbContext.cs")]
public class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options)
        : base(options)
    {
    }


    public DbSet<WeatherObservation> WeatherObservations { get; set; }
    public DbSet<Station> Stations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new WeatherObservationConfiguration());
        modelBuilder.ApplyConfiguration(new StationConfiguration());
    }
}
