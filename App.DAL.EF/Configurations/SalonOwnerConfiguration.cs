using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class SalonOwnerConfiguration : IEntityTypeConfiguration<SalonOwner>
{
    public void Configure(EntityTypeBuilder<SalonOwner> builder)
    {
        builder.HasIndex(e => new { e.SalonId, e.UserId }).IsUnique();

        builder.HasOne(e => e.Salon)
            .WithMany(e => e.Owners)
            .HasForeignKey(e => e.SalonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany(e => e.SalonOwners)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
