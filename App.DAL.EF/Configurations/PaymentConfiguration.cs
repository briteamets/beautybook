using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(e => e.Amount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.Currency).HasMaxLength(3);
        builder.Property(e => e.Status).HasMaxLength(32);
        builder.Property(e => e.PaymentMethod).HasMaxLength(64);
        builder.Property(e => e.TransactionId).HasMaxLength(128);

        builder.HasOne(e => e.User)
            .WithMany(e => e.Payments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.BookingPayments)
            .WithOne(e => e.Payment)
            .HasForeignKey(e => e.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
