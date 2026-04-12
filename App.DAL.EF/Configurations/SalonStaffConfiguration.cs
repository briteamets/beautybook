using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class SalonStaffConfiguration : IEntityTypeConfiguration<SalonStaff>
{
    public void Configure(EntityTypeBuilder<SalonStaff> builder)
    {
        builder.Property(e => e.Role).HasMaxLength(32);
    }
}
