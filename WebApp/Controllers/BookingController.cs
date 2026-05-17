using System.Text;
using System.Globalization;
using App.BLL.Services;
using App.Contracts;
using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels;

namespace WebApp.Controllers;

[Authorize]
public class BookingController : ClientControllerBase
{
    private readonly ISlotGeneratorService _slotGeneratorService;

    public BookingController(AppDbContext context, ISlotGeneratorService slotGeneratorService) : base(context)
    {
        _slotGeneratorService = slotGeneratorService;
    }

    [HttpGet]
    public async Task<IActionResult> Book(Guid serviceId, DateOnly? date = null, Guid? salonId = null)
    {
        var service = await Context.Services
            .Include(s => s.BeautyProvider)
            .Include(s => s.ServiceCategory)
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.To == null && s.IsActive);
        if (service == null) return NotFound();

        var vm = new DynamicBookingViewModel
        {
            ServiceId = service.Id,
            ServiceName = service.Name.Translate() ?? string.Empty,
            ProviderName = service.BeautyProvider?.ProfileName ?? string.Empty,
            ProviderAddress = service.BeautyProvider?.Address ?? string.Empty,
            ProviderPictureUrl = service.BeautyProvider?.ProfilePictureUrl,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            SelectedDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            SalonId = salonId
        };

        if (salonId.HasValue)
        {
            var categoryId = service.ServiceCategoryId;
            var currentProviderId = service.BeautyProviderId;
            var siblings = await Context.SalonStaff
                .Where(st => st.SalonId == salonId.Value && st.To == null && st.BeautyProviderId != currentProviderId)
                .Include(st => st.BeautyProvider)
                    .ThenInclude(p => p!.Services!.Where(s => s.To == null && s.IsActive && s.ServiceCategoryId == categoryId))
                .ToListAsync();

            vm.SiblingProviders = siblings
                .Where(st => st.BeautyProvider?.Services?.Any() == true)
                .Select(st => new SiblingProviderViewModel
                {
                    ProviderName = st.BeautyProvider!.ProfileName,
                    ProfilePictureUrl = st.BeautyProvider.ProfilePictureUrl,
                    ServiceId = st.BeautyProvider.Services!.First().Id,
                    ServicePrice = st.BeautyProvider.Services!.First().Price
                }).ToList();
        }

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Slots(Guid serviceId, string date)
    {
        if (!TryParseDate(date, out var parsedDate) ||
            parsedDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Json(Array.Empty<object>());
        }

        var slots = await _slotGeneratorService.GenerateSlotsAsync(serviceId, parsedDate);
        return Json(slots
            .Where(s => s.IsAvailable)
            .Select(s => new
            {
                startTime = TimeOnly.FromTimeSpan(s.StartTime).ToString("HH:mm"),
                endTime = TimeOnly.FromTimeSpan(s.EndTime).ToString("HH:mm")
            }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDynamic(Guid serviceId, DateOnly bookingDate, TimeSpan startTime, string? notes, Guid? salonId = null)
    {
        if (bookingDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            SetErrorMessage(T("ChooseFutureTimeSlot"));
            return RedirectToAction(nameof(Book), new { serviceId, date = bookingDate.ToString("yyyy-MM-dd"), salonId });
        }

        var service = await Context.Services
            .Include(s => s.BeautyProvider)
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.To == null && s.IsActive);
        if (service == null) return NotFound();

        var slots = await _slotGeneratorService.GenerateSlotsAsync(serviceId, bookingDate);
        var slot = slots.FirstOrDefault(s => s.StartTime == startTime);
        if (slot == null)
        {
            SetErrorMessage(T("SelectedTimeSlotUnavailable"));
            return RedirectToAction(nameof(Book), new { serviceId, date = bookingDate.ToString("yyyy-MM-dd"), salonId });
        }

        var userId = GetCurrentUserId();
        var hasClientOverlap = await Context.Bookings
            .AnyAsync(b => b.UserId == userId &&
                           b.Service!.BeautyProviderId == service.BeautyProviderId &&
                           b.BookingDate == bookingDate &&
                           b.To == null &&
                           (b.Status == Booking.StatusPending || b.Status == Booking.StatusConfirmed) &&
                           startTime < b.EndTime &&
                           slot.EndTime > b.StartTime);

        if (hasClientOverlap)
        {
            SetErrorMessage(T("ClientOverlappingBooking"));
            return RedirectToAction(nameof(Book), new { serviceId, date = bookingDate.ToString("yyyy-MM-dd"), salonId });
        }

        var booking = new Booking
        {
            UserId = userId,
            ServiceId = service.Id,
            SalonId = salonId,
            BookedAt = DateTime.UtcNow,
            BookingDate = bookingDate,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            AppointmentDateTime = bookingDate.ToDateTime(TimeOnly.FromTimeSpan(slot.StartTime), DateTimeKind.Utc),
            Status = Booking.StatusPending,
            Notes = notes,
            From = DateTime.UtcNow
        };

        Context.Bookings.Add(booking);
        await Context.SaveChangesAsync();

        Context.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = Booking.StatusPending,
            NewStatus = Booking.StatusPending,
            UserId = booking.UserId,
            ChangedAt = DateTime.UtcNow,
            Reason = "Dynamic booking created",
            From = DateTime.UtcNow
        });
        Context.Notifications.Add(new Notification
        {
            UserId = service.BeautyProvider!.UserId,
            BookingId = booking.Id,
            Type = Notification.TypeBookingRequested,
            Title = "New booking request",
            Body = $"New booking request for {service.Name.Translate()} on {booking.AppointmentDateTime:yyyy-MM-dd HH:mm}.",
            IsRead = false,
            From = DateTime.UtcNow
        });
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Payment), new { bookingId = booking.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Summary(Guid serviceId, Guid availabilityId, Guid? salonId = null, Guid? bookingId = null)
    {
        var vm = await BuildSummaryViewModel(serviceId, availabilityId, salonId, null, bookingId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Summary(BookingSummaryViewModel vm)
    {
        var hydratedVm = await BuildSummaryViewModel(vm.ServiceId, vm.AvailabilityId, vm.SalonId, vm.Notes, vm.BookingId);
        if (hydratedVm == null) return NotFound();
        if (!ModelState.IsValid) return View(hydratedVm);

        var service = await Context.Services
            .Include(s => s.BeautyProvider)
            .FirstAsync(s => s.Id == vm.ServiceId && s.To == null && s.IsActive);

        var availability = await Context.Availabilities
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Id == vm.AvailabilityId && a.To == null);
        if (availability == null || availability.IsBooked)
        {
            SetErrorMessage(T("SelectedTimeSlotUnavailable"));
            return RedirectToAction("Details", "ProviderProfile", new { id = service.BeautyProviderId });
        }

        if (vm.BookingId.HasValue)
        {
            var booking = await Context.Bookings
                .AsTracking()
                .Include(b => b.Availability)
                .Include(b => b.BookingPayments!)
                .FirstOrDefaultAsync(b => b.Id == vm.BookingId.Value && b.UserId == GetCurrentUserId() && b.To == null);
            if (booking == null) return NotFound();
            if (booking.Status == Booking.StatusCancelled)
            {
                SetErrorMessage(T("CancelledBookingsCannotBeRescheduled"));
                return RedirectToAction("Index", "MyBookings", new { tab = "Cancelled" });
            }
            if (booking.ServiceId != vm.ServiceId)
            {
                SetErrorMessage(T("RescheduleSameServiceOnly"));
                return RedirectToAction("Details", "ProviderProfile", new
                {
                    id = service.BeautyProviderId,
                    selectedServiceId = booking.ServiceId,
                    bookingId = booking.Id
                });
            }

            var oldSlot = booking.Availability;
            availability.IsBooked = true;
            if (oldSlot != null && oldSlot.Id != availability.Id)
            {
                oldSlot.IsBooked = false;
            }

            var oldAppointment = booking.AppointmentDateTime;
            booking.AvailabilityId = availability.Id;
            booking.SalonId = vm.SalonId;
            booking.AppointmentDateTime = availability.Date.ToDateTime(availability.StartTime, DateTimeKind.Utc);
            booking.BookingDate = availability.Date;
            booking.StartTime = availability.StartTime.ToTimeSpan();
            booking.EndTime = availability.EndTime.ToTimeSpan();
            booking.Notes = vm.Notes;

            Context.BookingHistories.Add(new BookingHistory
            {
                BookingId = booking.Id,
                OldStatus = booking.Status,
                NewStatus = booking.Status,
                UserId = booking.UserId,
                ChangedAt = DateTime.UtcNow,
                Reason = $"Booking rescheduled from {oldAppointment:u}",
                From = DateTime.UtcNow
            });

            await Context.SaveChangesAsync();
            SetSuccessMessage(T("BookingRescheduled"));
            return booking.BookingPayments?.Any() == true
                ? RedirectToAction(nameof(Confirmation), new { bookingId = booking.Id })
                : RedirectToAction(nameof(Payment), new { bookingId = booking.Id });
        }
        else
        {
            availability.IsBooked = true;
            var booking = new Booking
            {
                UserId = GetCurrentUserId(),
                ServiceId = service.Id,
                AvailabilityId = availability.Id,
                SalonId = vm.SalonId,
                BookedAt = DateTime.UtcNow,
                AppointmentDateTime = availability.Date.ToDateTime(availability.StartTime, DateTimeKind.Utc),
                BookingDate = availability.Date,
                StartTime = availability.StartTime.ToTimeSpan(),
                EndTime = availability.EndTime.ToTimeSpan(),
                Status = Booking.StatusPending,
                Notes = vm.Notes,
                From = DateTime.UtcNow
            };

            Context.Bookings.Add(booking);
            await Context.SaveChangesAsync();

            Context.BookingHistories.Add(new BookingHistory
            {
                BookingId = booking.Id,
                OldStatus = Booking.StatusPending,
                NewStatus = Booking.StatusPending,
                UserId = booking.UserId,
                ChangedAt = DateTime.UtcNow,
                Reason = "Booking created",
                From = DateTime.UtcNow
            });
            Context.Notifications.Add(new Notification
            {
                UserId = service.BeautyProvider!.UserId,
                BookingId = booking.Id,
                Type = Notification.TypeBookingRequested,
                Title = "New booking request",
                Body = $"New booking request for {service.Name.Translate()} on {booking.AppointmentDateTime:yyyy-MM-dd HH:mm}.",
                IsRead = false,
                From = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();

            return RedirectToAction(nameof(Payment), new { bookingId = booking.Id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Payment(Guid bookingId)
    {
        var vm = await BuildPaymentViewModel(bookingId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayNow(Guid bookingId)
    {
        var booking = await Context.Bookings
            .AsTracking()
            .Include(b => b.Service)
                .ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.BookingPayments!)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == GetCurrentUserId() && b.To == null);
        if (booking == null) return NotFound();
        if (booking.BookingPayments?.Any() == true)
        {
            return RedirectToAction(nameof(Confirmation), new { bookingId });
        }

        var payment = new App.Domain.Payment
        {
            UserId = booking.UserId,
            Amount = booking.Service?.Price ?? 0,
            Currency = "EUR",
            Status = App.Domain.Payment.StatusPaid,
            PaymentMethod = "MockCard",
            TransactionId = Guid.NewGuid().ToString("N"),
            PaidAt = DateTime.UtcNow,
            From = DateTime.UtcNow
        };
        Context.Payments.Add(payment);
        await Context.SaveChangesAsync();

        Context.BookingPayments.Add(new BookingPayment
        {
            BookingId = booking.Id,
            PaymentId = payment.Id,
            Amount = payment.Amount
        });

        var oldStatus = booking.Status;
        booking.Status = Booking.StatusConfirmed;
        Context.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = oldStatus,
            NewStatus = Booking.StatusConfirmed,
            UserId = booking.UserId,
            ChangedAt = DateTime.UtcNow,
            Reason = "Payment completed",
            From = DateTime.UtcNow
        });

        Context.Notifications.Add(new Notification
        {
            UserId = booking.UserId,
            BookingId = booking.Id,
            Type = Notification.TypeBookingConfirmed,
            Title = "Booking confirmed",
            Body = "Your booking has been confirmed.",
            IsRead = false,
            From = DateTime.UtcNow
        });

        await Context.SaveChangesAsync();
        SetSuccessMessage(T("BookingConfirmedPaid"));
        return RedirectToAction(nameof(Confirmation), new { bookingId });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(Guid bookingId)
    {
        var booking = await Context.Bookings
            .Include(b => b.Service)
                .ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Salon)
            .Include(b => b.BookingPayments!)
                .ThenInclude(bp => bp.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == GetCurrentUserId() && b.To == null);
        if (booking == null) return NotFound();

        return View(new BookingConfirmationViewModel
        {
            BookingId = booking.Id,
            ServiceName = booking.Service?.Name.Translate() ?? string.Empty,
            ProviderName = booking.Service?.BeautyProvider?.ProfileName ?? string.Empty,
            SalonName = booking.Salon?.Name.Translate(),
            AppointmentDateTime = booking.AppointmentDateTime,
            TotalPrice = booking.BookingPayments?.Sum(bp => bp.Amount) ?? booking.Service?.Price ?? 0,
            Status = booking.BookingPayments?.Any(bp => bp.Payment?.Status == App.Domain.Payment.StatusPaid) == true ? "Paid" : booking.Status
        });
    }

    [HttpGet]
    public async Task<IActionResult> AddToCalendar(Guid bookingId)
    {
        var booking = await Context.Bookings
            .Include(b => b.Service)
                .ThenInclude(s => s!.BeautyProvider)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == GetCurrentUserId() && b.To == null);
        if (booking == null) return NotFound();

        var start = booking.AppointmentDateTime.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
        var end = booking.AppointmentDateTime.AddMinutes(booking.Service?.DurationMinutes ?? 60).ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
        var body = new StringBuilder()
            .AppendLine("BEGIN:VCALENDAR")
            .AppendLine("VERSION:2.0")
            .AppendLine("BEGIN:VEVENT")
            .AppendLine($"UID:{booking.Id}")
            .AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}")
            .AppendLine($"DTSTART:{start}")
            .AppendLine($"DTEND:{end}")
            .AppendLine($"SUMMARY:{booking.Service?.Name.Translate()} with {booking.Service?.BeautyProvider?.ProfileName}")
            .AppendLine("END:VEVENT")
            .AppendLine("END:VCALENDAR")
            .ToString();

        return File(Encoding.UTF8.GetBytes(body), "text/calendar", $"booking-{booking.Id}.ics");
    }

    private async Task<BookingSummaryViewModel?> BuildSummaryViewModel(Guid serviceId, Guid availabilityId, Guid? salonId, string? notes, Guid? bookingId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow);
        var service = await Context.Services
            .Include(s => s.BeautyProvider)
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.To == null && s.IsActive);
        var availability = await Context.Availabilities
            .FirstOrDefaultAsync(a =>
                a.Id == availabilityId &&
                a.To == null &&
                !a.IsBooked &&
                (a.Date > today || (a.Date == today && a.StartTime > nowTime)));
        if (service == null || availability == null || availability.BeautyProviderId != service.BeautyProviderId)
        {
            return null;
        }

        var salonName = salonId.HasValue
            ? await Context.Salons.Where(s => s.Id == salonId && s.To == null).Select(s => s.Name.Translate()).FirstOrDefaultAsync()
            : null;

        return new BookingSummaryViewModel
        {
            BookingId = bookingId,
            ServiceId = serviceId,
            AvailabilityId = availabilityId,
            SalonId = salonId,
            ServiceName = service.Name.Translate() ?? string.Empty,
            ProviderName = service.BeautyProvider?.ProfileName ?? string.Empty,
            SalonName = salonName,
            AppointmentDateTime = availability.Date.ToDateTime(availability.StartTime, DateTimeKind.Utc),
            TotalPrice = service.Price,
            DurationMinutes = service.DurationMinutes,
            Notes = notes
        };
    }

    private async Task<BookingPaymentViewModel?> BuildPaymentViewModel(Guid bookingId)
    {
        var booking = await Context.Bookings
            .Include(b => b.Service)
                .ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Salon)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == GetCurrentUserId() && b.To == null);
        if (booking == null) return null;

        return new BookingPaymentViewModel
        {
            BookingId = booking.Id,
            ServiceName = booking.Service?.Name.Translate() ?? string.Empty,
            ProviderName = booking.Service?.BeautyProvider?.ProfileName ?? string.Empty,
            SalonName = booking.Salon?.Name.Translate(),
            AppointmentDateTime = booking.AppointmentDateTime,
            TotalPrice = booking.Service?.Price ?? 0
        };
    }

    private static bool TryParseDate(string value, out DateOnly date)
    {
        var formats = new[] { "yyyy-MM-dd", "dd.MM.yyyy", "d.M.yyyy" };
        return DateOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ||
               DateOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
    }
}
