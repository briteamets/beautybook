using System.Text.Json;
using App.Domain;
using App.Domain.Identity;
using Base.Contracts;
using Base.Domain;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace App.DAL.EF;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>, IDataProtectionKeyContext
{
    public DbSet<ListItem> ListItems { get; set; }
    public DbSet<BeautyProvider> BeautyProviders { get; set; } = default!;
    public DbSet<ServiceCategory> ServiceCategories { get; set; } = default!;
    public DbSet<ServiceType> ServiceTypes { get; set; } = default!;
    public DbSet<Service> Services { get; set; } = default!;
    public DbSet<Availability> Availabilities { get; set; } = default!;
    public DbSet<ProviderSchedule> ProviderSchedules { get; set; } = default!;
    public DbSet<ProviderBreak> ProviderBreaks { get; set; } = default!;
    public DbSet<Salon> Salons { get; set; } = default!;
    public DbSet<SalonOwner> SalonOwners { get; set; } = default!;
    public DbSet<SalonStaff> SalonStaff { get; set; } = default!;
    public DbSet<Booking> Bookings { get; set; } = default!;
    public DbSet<BookingHistory> BookingHistories { get; set; } = default!;
    public DbSet<Payment> Payments { get; set; } = default!;
    public DbSet<BookingPayment> BookingPayments { get; set; } = default!;
    public DbSet<Review> Reviews { get; set; } = default!;
    public DbSet<ProviderPortfolio> ProviderPortfolios { get; set; } = default!;
    public DbSet<FavoriteProvider> FavoriteProviders { get; set; } = default!;
    public DbSet<Notification> Notifications { get; set; } = default!;
    
    public DbSet<AppRefreshToken> RefreshTokens { get; set; } = default!;

    // This maps to the table that stores data protection keys.
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;


    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureLangStrProperties(builder);
    }

    private static void ConfigureLangStrProperties(ModelBuilder builder)
    {
        var langStrConverter = new ValueConverter<LangStr?, string?>(
            value => SerializeLangStr(value),
            value => DeserializeLangStr(value)
        );

        var langStrComparer = new ValueComparer<LangStr?>(
            (left, right) => SerializeLangStr(left) == SerializeLangStr(right),
            value => SerializeLangStr(value) == null ? 0 : SerializeLangStr(value)!.GetHashCode(),
            value => DeserializeLangStr(SerializeLangStr(value))
        );

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(IBaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            foreach (var property in entityType.ClrType.GetProperties()
                         .Where(p => p.PropertyType == typeof(LangStr)))
            {
                var propertyBuilder = builder.Entity(entityType.ClrType)
                    .Property(property.Name);

                propertyBuilder.Metadata.SetValueConverter(langStrConverter);
                propertyBuilder.Metadata.SetValueComparer(langStrComparer);
                propertyBuilder.HasColumnType("jsonb");
            }
        }
    }

    private static string? SerializeLangStr(LangStr? value)
    {
        return value == null ? null : JsonSerializer.Serialize(value, (JsonSerializerOptions?) null);
    }

    private static LangStr? DeserializeLangStr(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : JsonSerializer.Deserialize<LangStr>(value, (JsonSerializerOptions?) null);
    }
}
