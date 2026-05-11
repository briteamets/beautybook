using System.Net;
using App.BLL.Services;
using App.DAL.EF;
using App.Domain;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/bookings")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BookingsController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly ISlotGeneratorService _slotGeneratorService;

    public BookingsController(AppDbContext context, ISlotGeneratorService slotGeneratorService)
    {
        _context = context;
        _slotGeneratorService = slotGeneratorService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetBookings()
    {
        var userId = GetCurrentUserId();
        var bookings = await BookingQuery()
            .Where(b => b.UserId == userId && b.To == null)
            .OrderByDescending(b => b.AppointmentDateTime)
            .ToListAsync();

        return Ok(bookings.Select(b => b.ToResponse()));
    }

    [HttpGet("my")]
    public Task<ActionResult<IEnumerable<BookingResponse>>> GetMyBookings()
    {
        return GetBookings();
    }

    [HttpGet("incoming")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ServiceProvider")]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetIncomingBookings()
    {
        var userId = GetCurrentUserId();
        var bookings = await _context.Bookings
            .Include(b => b.Service).ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Salon)
            .Include(b => b.BookingPayments!).ThenInclude(bp => bp.Payment)
            .Include(b => b.Review)
            .Include(b => b.User)
            .Where(b => b.Service!.BeautyProvider!.UserId == userId && b.To == null)
            .OrderByDescending(b => b.AppointmentDateTime)
            .ToListAsync();

        return Ok(bookings.Select(b =>
        {
            var r = b.ToResponse();
            r.ClientName = b.User?.Email;
            return r;
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> GetBooking(Guid id)
    {
        var booking = await BookingQuery()
            .FirstOrDefaultAsync(b => b.Id == id && b.To == null);
        if (booking == null) return NotFound();
        if (!IsCurrentUser(booking.UserId)) return Forbid();

        return Ok(booking.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> PostBooking([FromBody] BookingCreateRequest request)
    {
        if (request.ProviderServiceId.HasValue)
        {
            return await CreateDynamicBooking(request);
        }

        var userId = GetCurrentUserId();
        var service = await _context.Services
            .Include(s => s.BeautyProvider)
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.To == null && s.IsActive);
        if (service == null) return ApiError(HttpStatusCode.BadRequest, "Service not found.");

        var availability = await _context.Availabilities
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AvailabilityId && a.To == null);
        if (availability == null) return ApiError(HttpStatusCode.BadRequest, "Availability not found.");
        if (availability.BeautyProviderId != service.BeautyProviderId)
        {
            return ApiError(HttpStatusCode.BadRequest, "Availability does not belong to the selected service provider.");
        }
        if (availability.IsBooked) return ApiError(HttpStatusCode.BadRequest, "Availability is already booked.");

        App.Domain.Salon? salon = null;
        if (request.SalonId.HasValue)
        {
            salon = await _context.Salons.FirstOrDefaultAsync(s => s.Id == request.SalonId.Value && s.To == null);
            if (salon == null) return ApiError(HttpStatusCode.BadRequest, "Salon not found.");
        }

        var appointmentDateTime = availability.Date.ToDateTime(availability.StartTime, DateTimeKind.Utc);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        availability.IsBooked = true;

        var booking = new Booking
        {
            UserId = userId,
            ServiceId = service.Id,
            AvailabilityId = availability.Id,
            SalonId = salon?.Id,
            BookedAt = DateTime.UtcNow,
            AppointmentDateTime = appointmentDateTime,
            BookingDate = availability.Date,
            StartTime = availability.StartTime.ToTimeSpan(),
            EndTime = availability.EndTime.ToTimeSpan(),
            Status = Booking.StatusPending,
            Notes = request.Notes,
            From = DateTime.UtcNow
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        _context.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = Booking.StatusPending,
            NewStatus = Booking.StatusPending,
            UserId = userId,
            ChangedAt = DateTime.UtcNow,
            From = DateTime.UtcNow,
            Reason = "Booking created"
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var createdBooking = await BookingQuery().FirstAsync(b => b.Id == booking.Id);
        return CreatedAtAction(nameof(GetBooking), new { id = booking.Id, version = "1.0" }, createdBooking.ToResponse());
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> UpdateBooking(Guid id, [FromBody] BookingUpdateRequest request)
    {
        var booking = await _context.Bookings
            .AsTracking()
            .Include(b => b.Service).ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Salon)
            .FirstOrDefaultAsync(b => b.Id == id && b.To == null);
        if (booking == null) return NotFound();
        if (!IsCurrentUser(booking.UserId)) return Forbid();

        booking.Notes = request.Notes;
        await _context.SaveChangesAsync();

        return Ok(booking.ToResponse());
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<BookingResponse>> CancelBooking(Guid id, [FromBody] BookingCancelRequest? request)
    {
        var booking = await _context.Bookings
            .AsTracking()
            .Include(b => b.Availability)
            .Include(b => b.Service)
            .ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Salon)
            .FirstOrDefaultAsync(b => b.Id == id && b.To == null);
        if (booking == null) return NotFound();
        if (!IsCurrentUser(booking.UserId)) return Forbid();
        if (booking.Status == Booking.StatusCancelled) return ApiError(HttpStatusCode.BadRequest, "Booking is already cancelled.");
        if (booking.AppointmentDateTime <= DateTime.UtcNow.AddHours(1))
        {
            return ApiError(HttpStatusCode.BadRequest, "Booking can no longer be cancelled within one hour of the appointment.");
        }

        if (booking.Availability != null) booking.Availability.IsBooked = false;
        var oldStatus = booking.Status;
        booking.Status = Booking.StatusCancelled;

        _context.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = oldStatus,
            NewStatus = Booking.StatusCancelled,
            UserId = booking.UserId,
            ChangedAt = DateTime.UtcNow,
            Reason = request?.Reason,
            From = DateTime.UtcNow
        });

        _context.Notifications.Add(new Notification
        {
            UserId = booking.UserId,
            BookingId = booking.Id,
            Type = Notification.TypeBookingCancelled,
            Title = "Booking cancelled",
            Body = "Your booking has been cancelled.",
            IsRead = false,
            From = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var response = await BookingQuery().FirstAsync(b => b.Id == booking.Id);
        return Ok(response.ToResponse());
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ServiceProvider")]
    public async Task<ActionResult<BookingResponse>> UpdateBookingStatus(Guid id, [FromBody] BookingStatusUpdateRequest request)
    {
        var booking = await _context.Bookings
            .AsTracking()
            .Include(b => b.Availability)
            .Include(b => b.Service)
            .ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Salon)
            .FirstOrDefaultAsync(b => b.Id == id && b.To == null);
        if (booking == null) return NotFound();
        if (!IsCurrentUser(booking.Service!.BeautyProvider!.UserId)) return Forbid();

        var allowed = (booking.Status, request.Status) switch
        {
            (Booking.StatusPending, Booking.StatusConfirmed) => true,
            (Booking.StatusPending, Booking.StatusCancelled) => true,
            (Booking.StatusConfirmed, Booking.StatusCompleted) => true,
            _ => false
        };

        if (!allowed)
        {
            return ApiError(HttpStatusCode.BadRequest, "Invalid booking status transition.");
        }

        var oldStatus = booking.Status;
        booking.Status = request.Status;
        if (request.Status == Booking.StatusCancelled && booking.Availability != null)
        {
            booking.Availability.IsBooked = false;
        }

        _context.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = oldStatus,
            NewStatus = booking.Status,
            UserId = GetCurrentUserId(),
            ChangedAt = DateTime.UtcNow,
            Reason = request.Reason,
            From = DateTime.UtcNow
        });

        var notificationTitle = request.Status switch
        {
            Booking.StatusConfirmed => "Booking confirmed",
            Booking.StatusCancelled => "Booking cancelled by provider",
            Booking.StatusCompleted => "Booking completed",
            _ => null
        };
        if (notificationTitle != null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = booking.UserId,
                Type = request.Status == Booking.StatusCancelled
                    ? Notification.TypeBookingCancelled
                    : Notification.TypeBookingConfirmed,
                Title = notificationTitle,
                Body = $"Your booking for {booking.Service?.Name.Translate()} on {booking.AppointmentDateTime:yyyy-MM-dd HH:mm} has been updated.",
                IsRead = false,
                From = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        var response = await BookingQuery().FirstAsync(b => b.Id == id);
        return Ok(response.ToResponse());
    }

    private async Task<ActionResult<BookingResponse>> CreateDynamicBooking(BookingCreateRequest request)
    {
        if (!request.ProviderServiceId.HasValue || !request.Date.HasValue || !request.StartTime.HasValue)
        {
            return ApiError(HttpStatusCode.BadRequest, "providerServiceId, date, and startTime are required.");
        }

        var userId = GetCurrentUserId();
        var service = await _context.Services
            .Include(s => s.BeautyProvider)
            .FirstOrDefaultAsync(s => s.Id == request.ProviderServiceId.Value && s.To == null && s.IsActive);
        if (service == null) return ApiError(HttpStatusCode.BadRequest, "Provider service not found.");
        if (request.Date.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return ApiError(HttpStatusCode.BadRequest, "Cannot book in the past.");
        }

        var slots = await _slotGeneratorService.GenerateSlotsAsync(service.Id, request.Date.Value);
        var slot = slots.FirstOrDefault(s => s.StartTime == request.StartTime.Value);
        if (slot == null)
        {
            return ApiError(HttpStatusCode.BadRequest, "Selected slot is not available.");
        }

        var hasClientOverlap = await _context.Bookings.AnyAsync(b =>
            b.UserId == userId &&
            b.Service!.BeautyProviderId == service.BeautyProviderId &&
            b.BookingDate == request.Date.Value &&
            b.To == null &&
            (b.Status == Booking.StatusPending || b.Status == Booking.StatusConfirmed) &&
            request.StartTime.Value < b.EndTime &&
            slot.EndTime > b.StartTime);

        if (hasClientOverlap)
        {
            return ApiError(HttpStatusCode.BadRequest, "Client already has an overlapping booking with this provider.");
        }

        var booking = new Booking
        {
            UserId = userId,
            ServiceId = service.Id,
            BookedAt = DateTime.UtcNow,
            BookingDate = request.Date.Value,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            AppointmentDateTime = request.Date.Value.ToDateTime(TimeOnly.FromTimeSpan(slot.StartTime), DateTimeKind.Utc),
            Status = Booking.StatusPending,
            Notes = request.Notes,
            From = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        _context.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = Booking.StatusPending,
            NewStatus = Booking.StatusPending,
            UserId = userId,
            ChangedAt = DateTime.UtcNow,
            Reason = "Dynamic API booking created",
            From = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var created = await BookingQuery().FirstAsync(b => b.Id == booking.Id);
        return CreatedAtAction(nameof(GetBooking), new { id = booking.Id, version = "1.0" }, created.ToResponse());
    }

    private IQueryable<Booking> BookingQuery()
    {
        return _context.Bookings
            .Include(b => b.Service)
            .ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Salon)
            .Include(b => b.BookingPayments!)
            .ThenInclude(bp => bp.Payment)
            .Include(b => b.Review)
            .AsQueryable();
    }
}
