using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(e => e.Type).HasMaxLength(64);
        builder.Property(e => e.Title).HasMaxLength(256);
        builder.Property(e => e.Body).HasMaxLength(2000);

        builder.HasOne(e => e.User)
            .WithMany(e => e.Notifications)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
