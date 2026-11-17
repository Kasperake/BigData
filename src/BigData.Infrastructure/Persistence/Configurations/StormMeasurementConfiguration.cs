using BigData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BigData.Infrastructure.Persistence.Configurations;

internal class StormMeasurementConfiguration : IEntityTypeConfiguration<StormMeasurement>
{
    public void Configure(EntityTypeBuilder<StormMeasurement> builder)
    {
        builder.HasKey(sm => sm.Id);
        builder.Property(sm => sm.StationId).IsRequired();
        builder.Property(sm => sm.ObservedAtUtc).IsRequired();
        builder.HasMany(sm => sm.Parameters)
               .WithOne()
               .HasForeignKey("StormMeasurementId")
               .IsRequired();
    }
}
