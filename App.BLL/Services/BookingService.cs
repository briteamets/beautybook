using App.Contracts;
using App.Domain;

namespace App.BLL.Services;

public class BookingService : IBookingService
{
    private readonly IAppUnitOfWork _uow;

    public BookingService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<Booking>> GetUserBookingsAsync(Guid userId) =>
        await _uow.Bookings.GetByUserIdAsync(userId);

    public async Task<IEnumerable<Booking>> GetProviderBookingsAsync(Guid providerUserId) =>
        await _uow.Bookings.GetByProviderUserIdAsync(providerUserId);

    public Task<Booking?> GetByIdAsync(Guid id) =>
        _uow.Bookings.GetByIdWithDetailsAsync(id);

    public async Task<Booking> CreateBookingAsync(
        Guid userId, Guid serviceId, Guid availabilityId, Guid? salonId, string? notes)
    {
        var service = await _uow.Services.GetActiveWithProviderAsync(serviceId)
            ?? throw new ArgumentException("Service not found.");

        var availability = await _uow.Availabilities.GetByIdTrackedAsync(availabilityId)
            ?? throw new ArgumentException("Availability not found.");

        if (availability.BeautyProviderId != service.BeautyProviderId)
            throw new ArgumentException("Availability does not belong to the selected service provider.");
        if (availability.IsBooked)
            throw new ArgumentException("Availability is already booked.");

        if (salonId.HasValue && !await _uow.Availabilities.SalonExistsAsync(salonId.Value))
            throw new ArgumentException("Salon not found.");

        var now = DateTime.UtcNow;
        var appointmentDateTime = availability.Date.ToDateTime(availability.StartTime, DateTimeKind.Utc);

        await using var tx = await _uow.BeginTransactionAsync();

        availability.IsBooked = true;

        var booking = new Booking
        {
            UserId = userId,
            ServiceId = service.Id,
            AvailabilityId = availability.Id,
            SalonId = salonId,
            BookedAt = now,
            AppointmentDateTime = appointmentDateTime,
            BookingDate = availability.Date,
            StartTime = availability.StartTime.ToTimeSpan(),
            EndTime = availability.EndTime.ToTimeSpan(),
            Status = Booking.StatusPending,
            Notes = notes,
            From = now
        };
        await _uow.Bookings.AddAsync(booking);
        await _uow.SaveChangesAsync();

        _uow.Bookings.AddHistory(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = Booking.StatusPending,
            NewStatus = Booking.StatusPending,
            UserId = userId,
            ChangedAt = now,
            Reason = "Booking created",
            From = now
        });
        _uow.Bookings.AddNotification(new Notification
        {
            UserId = service.BeautyProvider!.UserId,
            BookingId = booking.Id,
            Type = Notification.TypeBookingRequested,
            Title = "New booking request",
            Body = $"New booking request for {service.Name.Translate(Thread.CurrentThread.CurrentUICulture.Name)} on {booking.AppointmentDateTime:yyyy-MM-dd HH:mm}.",
            IsRead = false,
            From = now
        });
        await _uow.SaveChangesAsync();
        await tx.CommitAsync();

        return (await _uow.Bookings.GetByIdWithDetailsAsync(booking.Id))!;
    }

    public async Task<Booking> CreateDynamicBookingAsync(
        Guid userId, Guid providerServiceId, DateOnly date, TimeSpan startTime, TimeSpan endTime, string? notes)
    {
        var service = await _uow.Services.GetActiveWithProviderAsync(providerServiceId)
            ?? throw new ArgumentException("Provider service not found.");

        if (await _uow.Bookings.HasOverlapAsync(userId, service.BeautyProviderId, date, startTime, endTime))
            throw new ArgumentException("Client already has an overlapping booking with this provider.");

        var now = DateTime.UtcNow;
        var booking = new Booking
        {
            UserId = userId,
            ServiceId = service.Id,
            BookedAt = now,
            BookingDate = date,
            StartTime = startTime,
            EndTime = endTime,
            AppointmentDateTime = date.ToDateTime(TimeOnly.FromTimeSpan(startTime), DateTimeKind.Utc),
            Status = Booking.StatusPending,
            Notes = notes,
            From = now
        };
        await _uow.Bookings.AddAsync(booking);
        await _uow.SaveChangesAsync();

        _uow.Bookings.AddHistory(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = Booking.StatusPending,
            NewStatus = Booking.StatusPending,
            UserId = userId,
            ChangedAt = now,
            Reason = "Dynamic API booking created",
            From = now
        });
        _uow.Bookings.AddNotification(new Notification
        {
            UserId = service.BeautyProvider!.UserId,
            BookingId = booking.Id,
            Type = Notification.TypeBookingRequested,
            Title = "New booking request",
            Body = $"New booking request for {service.Name.Translate(Thread.CurrentThread.CurrentUICulture.Name)} on {booking.AppointmentDateTime:yyyy-MM-dd HH:mm}.",
            IsRead = false,
            From = now
        });
        await _uow.SaveChangesAsync();

        return (await _uow.Bookings.GetByIdWithDetailsAsync(booking.Id))!;
    }

    public async Task<Booking> CancelBookingAsync(Guid bookingId, Guid requestingUserId, string? reason)
    {
        var booking = await _uow.Bookings.GetByIdWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.UserId != requestingUserId)
            throw new UnauthorizedAccessException();
        if (booking.Status == Booking.StatusCancelled)
            throw new ArgumentException("Booking is already cancelled.");
        if (booking.AppointmentDateTime <= DateTime.UtcNow.AddHours(1))
            throw new ArgumentException("Booking can no longer be cancelled within one hour of the appointment.");

        if (booking.AvailabilityId.HasValue)
        {
            var availability = await _uow.Availabilities.GetByIdTrackedAsync(booking.AvailabilityId.Value);
            if (availability != null) availability.IsBooked = false;
        }

        var oldStatus = booking.Status;
        booking.Status = Booking.StatusCancelled;
        _uow.Bookings.Update(booking);

        var now = DateTime.UtcNow;
        _uow.Bookings.AddHistory(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = oldStatus,
            NewStatus = Booking.StatusCancelled,
            UserId = requestingUserId,
            ChangedAt = now,
            Reason = reason,
            From = now
        });
        _uow.Bookings.AddNotification(new Notification
        {
            UserId = booking.UserId,
            BookingId = booking.Id,
            Type = Notification.TypeBookingCancelled,
            Title = "Booking cancelled",
            Body = "Your booking has been cancelled.",
            IsRead = false,
            From = now
        });

        await _uow.SaveChangesAsync();
        return (await _uow.Bookings.GetByIdWithDetailsAsync(booking.Id))!;
    }

    public async Task<Booking> UpdateStatusAsync(
        Guid bookingId, Guid providerUserId, string newStatus, string? reason)
    {
        var booking = await _uow.Bookings.GetByIdWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.Service?.BeautyProvider?.UserId != providerUserId)
            throw new UnauthorizedAccessException();

        var allowed = (booking.Status, newStatus) switch
        {
            (Booking.StatusPending, Booking.StatusConfirmed) => true,
            (Booking.StatusPending, Booking.StatusCancelled) => true,
            (Booking.StatusConfirmed, Booking.StatusCompleted) => true,
            _ => false
        };
        if (!allowed) throw new ArgumentException("Invalid booking status transition.");

        var oldStatus = booking.Status;
        booking.Status = newStatus;
        _uow.Bookings.Update(booking);
        if (newStatus == Booking.StatusCancelled && booking.AvailabilityId.HasValue)
        {
            var availability = await _uow.Availabilities.GetByIdTrackedAsync(booking.AvailabilityId.Value);
            if (availability != null) availability.IsBooked = false;
        }

        var now = DateTime.UtcNow;
        _uow.Bookings.AddHistory(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            UserId = providerUserId,
            ChangedAt = now,
            Reason = reason,
            From = now
        });

        var notifTitle = newStatus switch
        {
            Booking.StatusConfirmed => "Booking confirmed",
            Booking.StatusCancelled => "Booking cancelled by provider",
            Booking.StatusCompleted => "Booking completed",
            _ => null
        };
        if (notifTitle != null)
        {
            _uow.Bookings.AddNotification(new Notification
            {
                UserId = booking.UserId,
                Type = newStatus == Booking.StatusCancelled
                    ? Notification.TypeBookingCancelled
                    : Notification.TypeBookingConfirmed,
                Title = notifTitle,
                Body = $"Your booking for {booking.Service?.Name.Translate(Thread.CurrentThread.CurrentUICulture.Name)} on {booking.AppointmentDateTime:yyyy-MM-dd HH:mm} has been updated.",
                IsRead = false,
                From = now
            });
        }

        await _uow.SaveChangesAsync();
        return (await _uow.Bookings.GetByIdWithDetailsAsync(booking.Id))!;
    }

    public async Task<Booking> UpdateNotesAsync(Guid bookingId, Guid requestingUserId, string? notes)
    {
        var booking = await _uow.Bookings.GetByIdWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.UserId != requestingUserId)
            throw new UnauthorizedAccessException();

        booking.Notes = notes;
        _uow.Bookings.Update(booking);
        await _uow.SaveChangesAsync();

        return (await _uow.Bookings.GetByIdWithDetailsAsync(booking.Id))!;
    }
}
