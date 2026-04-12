using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        builder.HasMany(e => e.Bookings)
            .WithOne(e => e.Availability)
            .HasForeignKey(e => e.AvailabilityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
