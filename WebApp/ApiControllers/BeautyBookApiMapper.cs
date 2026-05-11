using App.Domain;
using App.DTO.v1;
using Base.Domain;

namespace WebApp.ApiControllers;

public static class BeautyBookApiMapper
{
    public static string? Translate(this LangStr? value)
    {
        return value?.Translate(Thread.CurrentThread.CurrentUICulture.Name);
    }

    public static LangStr ToLangStr(this Dictionary<string, string> values)
    {
        var result = new LangStr();
        foreach (var pair in values)
        {
            result[pair.Key] = pair.Value;
        }

        return result;
    }

    public static LangStr? ToLangStrOrNull(this Dictionary<string, string>? values)
    {
        return values == null || values.Count == 0 ? null : values.ToLangStr();
    }

    public static AvailabilityResponse ToResponse(this Availability entity)
    {
        return new AvailabilityResponse
        {
            Id = entity.Id,
            Date = entity.Date,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            IsBooked = entity.IsBooked
        };
    }

    public static ServiceResponse ToResponse(this Service entity)
    {
        return new ServiceResponse
        {
            Id = entity.Id,
            Name = entity.Name.Translate() ?? string.Empty,
            Description = entity.Description?.Translate(Thread.CurrentThread.CurrentUICulture.Name),
            Price = entity.Price,
            DurationMinutes = entity.DurationMinutes,
            CategoryId = entity.ServiceCategoryId,
            CategoryName = entity.ServiceCategory?.Name.Translate() ?? string.Empty,
            ServiceTypeId = entity.ServiceTypeId,
            IsActive = entity.IsActive
        };
    }

    public static BeautyProviderResponse ToResponse(
        this BeautyProvider entity,
        decimal averageRating,
        int reviewCount,
        List<AvailabilityResponse> todayAvailability)
    {
        return new BeautyProviderResponse
        {
            Id = entity.Id,
            ProfileName = entity.ProfileName,
            Bio = entity.Bio,
            Address = entity.Address,
            Lat = entity.Latitude,
            Lng = entity.Longitude,
            ProfilePictureUrl = entity.ProfilePictureUrl,
            AverageRating = averageRating,
            ReviewCount = reviewCount,
            TodayAvailability = todayAvailability
        };
    }

    public static BookingResponse ToResponse(this Booking entity)
    {
        return new BookingResponse
        {
            Id = entity.Id,
            ServiceName = entity.Service?.Name.Translate() ?? string.Empty,
            ProviderName = entity.Service?.BeautyProvider?.ProfileName ?? string.Empty,
            SalonName = entity.Salon?.Name.Translate(Thread.CurrentThread.CurrentUICulture.Name),
            AppointmentDateTime = entity.AppointmentDateTime,
            Status = entity.Status,
            TotalPrice = entity.Service?.Price ?? 0,
            IsPaid = entity.BookingPayments?.Any(bp => bp.Payment?.Status == Payment.StatusPaid) ?? false,
            Notes = entity.Notes,
            HasReview = entity.Review != null
        };
    }

    public static PaymentResponse ToResponse(this Payment entity)
    {
        return new PaymentResponse
        {
            Id = entity.Id,
            Amount = entity.Amount,
            Currency = entity.Currency,
            Status = entity.Status,
            PaidAt = entity.PaidAt
        };
    }

    public static ReviewResponse ToResponse(this Review entity)
    {
        return new ReviewResponse
        {
            Id = entity.Id,
            Rating = entity.Rating,
            Comment = entity.Comment,
            UserName = entity.User?.Email ?? string.Empty,
            CreatedAt = entity.From
        };
    }

    public static SalonResponse ToResponse(this Salon entity)
    {
        return new SalonResponse
        {
            Id = entity.Id,
            Name = entity.Name.Translate() ?? string.Empty,
            Description = entity.Description?.Translate(Thread.CurrentThread.CurrentUICulture.Name),
            Address = entity.Address,
            Lat = entity.Latitude,
            Lng = entity.Longitude,
            ProfilePictureUrl = entity.ProfilePictureUrl,
            StaffMembers = entity.Staff?
                .Select(s => new SalonStaffResponse
                {
                    ProviderId = s.BeautyProviderId,
                    ProviderName = s.BeautyProvider?.ProfileName ?? string.Empty,
                    Role = s.Role,
                    ProfilePictureUrl = s.BeautyProvider?.ProfilePictureUrl
                }).ToList() ?? new List<SalonStaffResponse>()
        };
    }

    public static ServiceTypeResponse ToResponse(this ServiceType entity)
    {
        return new ServiceTypeResponse
        {
            Id = entity.Id,
            ServiceCategoryId = entity.ServiceCategoryId,
            Name = entity.Name.Translate() ?? string.Empty,
            Description = entity.Description?.Translate(Thread.CurrentThread.CurrentUICulture.Name)
        };
    }

    public static ServiceCategoryResponse ToResponse(this ServiceCategory entity)
    {
        return new ServiceCategoryResponse
        {
            Id = entity.Id,
            Name = entity.Name.Translate() ?? string.Empty,
            Description = entity.Description?.Translate(Thread.CurrentThread.CurrentUICulture.Name)
        };
    }

    public static NotificationResponse ToResponse(this Notification entity)
    {
        return new NotificationResponse
        {
            Id = entity.Id,
            Type = entity.Type,
            Title = entity.Title,
            Body = entity.Body,
            IsRead = entity.IsRead,
            CreatedAt = entity.From
        };
    }

    public static FavoriteProviderResponse ToResponse(this FavoriteProvider entity, decimal averageRating)
    {
        return new FavoriteProviderResponse
        {
            Id = entity.Id,
            ProviderId = entity.BeautyProviderId,
            ProfileName = entity.BeautyProvider?.ProfileName ?? string.Empty,
            Address = entity.BeautyProvider?.Address ?? string.Empty,
            AverageRating = averageRating
        };
    }
}
