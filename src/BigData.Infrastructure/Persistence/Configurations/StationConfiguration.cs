using BigData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BigData.Infrastructure.Persistence.Configurations;

internal class StationConfiguration : IEntityTypeConfiguration<Station>
{
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.ToTable("Station");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StationId)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CountryCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.StationId)
            .HasDatabaseName("IX_Station_StationId")
            .IsUnique();
    }
}
