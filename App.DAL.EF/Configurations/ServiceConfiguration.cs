using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.Property(e => e.Price).HasColumnType("numeric(18,2)");

        builder.HasMany(e => e.Bookings)
            .WithOne(e => e.Service)
            .HasForeignKey(e => e.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ServiceType)
            .WithMany(e => e.Services)
            .HasForeignKey(e => e.ServiceTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
