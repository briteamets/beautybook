using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class SalonConfiguration : IEntityTypeConfiguration<Salon>
{
    public void Configure(EntityTypeBuilder<Salon> builder)
    {
        builder.Property(e => e.Address).HasMaxLength(256);
        builder.Property(e => e.ProfilePictureUrl).HasMaxLength(512);

        builder.HasMany(e => e.Staff)
            .WithOne(e => e.Salon)
            .HasForeignKey(e => e.SalonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Bookings)
            .WithOne(e => e.Salon)
            .HasForeignKey(e => e.SalonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
