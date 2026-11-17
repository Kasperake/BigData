using BigData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BigData.Infrastructure.Persistence.Configurations;

[Obsolete("This will deprecate - We are initializing MongoDb instead.")]
internal class WeatherObservationConfiguration : IEntityTypeConfiguration<WeatherObservation>
{
    public void Configure(EntityTypeBuilder<WeatherObservation> builder)
    {
        builder.ToTable("WeatherObservation");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.StationId)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ParameterId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ObservedAtUtc)
            .HasColumnType("DATETIME2(3)")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("DATETIME2(3)")
            .IsRequired();

        builder.Property(x => x.Value)
            .IsRequired();

        builder.Property(x => x.Longitude)
            .IsRequired(false);

        builder.Property(x => x.Latitude)
            .IsRequired(false);

        builder.HasIndex(x => x.CreatedAtUtc)
            .HasDatabaseName("IX_WeatherObservation_CreatedAtUtc")
            .IsDescending(true);
    }
}
