using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class ProviderPortfolioConfiguration : IEntityTypeConfiguration<ProviderPortfolio>
{
    public void Configure(EntityTypeBuilder<ProviderPortfolio> builder)
    {
        builder.Property(e => e.ImageUrl).HasMaxLength(512);

        builder.HasOne(e => e.Service)
            .WithMany()
            .HasForeignKey(e => e.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
