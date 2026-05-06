using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class üBookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.Property(e => e.Status).HasMaxLength(32);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.CancellationReason).HasMaxLength(2000);
        builder.HasIndex(e => new { e.UserId, e.BookingDate });
        builder.HasIndex(e => new { e.ServiceId, e.BookingDate, e.StartTime });

        builder.HasOne(e => e.User)
            .WithMany(e => e.Bookings)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CancelledByAdmin)
            .WithMany(e => e.AdminCancelledBookings)
            .HasForeignKey(e => e.CancelledByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.BookingHistory)
            .WithOne(e => e.Booking)
            .HasForeignKey(e => e.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.BookingPayments)
            .WithOne(e => e.Booking)
            .HasForeignKey(e => e.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Review)
            .WithOne(e => e.Booking)
            .HasForeignKey<Review>(e => e.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Notification)
            .WithOne(e => e.Booking)
            .HasForeignKey<Notification>(e => e.BookingId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
