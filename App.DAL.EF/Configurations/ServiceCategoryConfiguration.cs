using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.HasMany(e => e.Services)
            .WithOne(e => e.ServiceCategory)
            .HasForeignKey(e => e.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.ServiceTypes)
            .WithOne(e => e.ServiceCategory)
            .HasForeignKey(e => e.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
