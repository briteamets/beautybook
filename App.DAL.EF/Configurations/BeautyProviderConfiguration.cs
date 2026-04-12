using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class BeautyProviderConfiguration : IEntityTypeConfiguration<BeautyProvider>
{
    public void Configure(EntityTypeBuilder<BeautyProvider> builder)
    {
        builder.Property(e => e.ProfileName).HasMaxLength(128);
        builder.Property(e => e.ProfilePictureUrl).HasMaxLength(512);
        builder.Property(e => e.Address).HasMaxLength(256);
        builder.Property(e => e.Bio).HasMaxLength(2000);

        builder.HasOne(e => e.User)
            .WithMany(e => e.BeautyProviders)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Services)
            .WithOne(e => e.BeautyProvider)
            .HasForeignKey(e => e.BeautyProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Availabilities)
            .WithOne(e => e.BeautyProvider)
            .HasForeignKey(e => e.BeautyProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Portfolio)
            .WithOne(e => e.BeautyProvider)
            .HasForeignKey(e => e.BeautyProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.SalonStaff)
            .WithOne(e => e.BeautyProvider)
            .HasForeignKey(e => e.BeautyProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.FavoritedBy)
            .WithOne(e => e.BeautyProvider)
            .HasForeignKey(e => e.BeautyProviderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
