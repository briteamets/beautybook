using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class ProviderBreakConfiguration : IEntityTypeConfiguration<ProviderBreak>
{
    public void Configure(EntityTypeBuilder<ProviderBreak> builder)
    {
        builder.HasIndex(e => new { e.ProviderId, e.DayOfWeek });

        builder.HasOne(e => e.Provider)
            .WithMany(e => e.Breaks)
            .HasForeignKey(e => e.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
