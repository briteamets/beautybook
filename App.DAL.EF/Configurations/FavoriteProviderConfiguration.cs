using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class FavoriteProviderConfiguration : IEntityTypeConfiguration<FavoriteProvider>
{
    public void Configure(EntityTypeBuilder<FavoriteProvider> builder)
    {
        builder.HasIndex(e => new { e.UserId, e.BeautyProviderId }).IsUnique();

        builder.HasOne(e => e.User)
            .WithMany(e => e.FavoriteProviders)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
