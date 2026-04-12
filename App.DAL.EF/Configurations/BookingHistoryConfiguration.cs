using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class BookingHistoryConfiguration : IEntityTypeConfiguration<BookingHistory>
{
    public void Configure(EntityTypeBuilder<BookingHistory> builder)
    {
        builder.Property(e => e.OldStatus).HasMaxLength(32);
        builder.Property(e => e.NewStatus).HasMaxLength(32);
        builder.Property(e => e.Reason).HasMaxLength(2000);

        builder.HasOne(e => e.User)
            .WithMany(e => e.BookingHistoryEntries)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
