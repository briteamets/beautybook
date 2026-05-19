using System.Net;
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
[Route("/api/v{version:apiVersion}/payments")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PaymentsController : BaseApiController
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentResponse>>> GetPayments()
    {
        var userId = GetCurrentUserId();
        var payments = await _context.Payments
            .Where(p => p.UserId == userId && p.To == null)
            .OrderByDescending(p => p.From)
            .ToListAsync();

        return Ok(payments.Select(p => p.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> PostPayment([FromBody] PaymentCreateRequest request)
    {
        var userId = GetCurrentUserId();
        if (request.BookingIds.Count == 0) return ApiError(HttpStatusCode.BadRequest, "At least one booking id is required.");

        var bookings = await _context.Bookings
            .AsTracking()
            .Include(b => b.Service)
            .Include(b => b.BookingPayments!)
            .Where(b => request.BookingIds.Contains(b.Id) && b.To == null)
            .ToListAsync();

        if (bookings.Count != request.BookingIds.Distinct().Count())
        {
            return ApiError(HttpStatusCode.BadRequest, "One or more bookings were not found.");
        }

        if (bookings.Any(b => b.UserId != userId))
        {
            return Forbid();
        }

        if (bookings.Any(b => b.BookingPayments!.Any(bp => bp.PaymentId != Guid.Empty)))
        {
            return ApiError(HttpStatusCode.BadRequest, "Payment already exists for one or more bookings.");
        }

        var isCash = string.Equals(request.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase);
        var payment = new Payment
        {
            UserId = userId,
            Amount = bookings.Sum(b => b.Service?.Price ?? 0),
            Currency = "EUR",
            Status = isCash ? Payment.StatusPending : Payment.StatusPaid,
            PaymentMethod = request.PaymentMethod,
            TransactionId = Guid.NewGuid().ToString("N"),
            PaidAt = isCash ? null : DateTime.UtcNow,
            From = DateTime.UtcNow
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        foreach (var booking in bookings)
        {
            _context.BookingPayments.Add(new BookingPayment
            {
                BookingId = booking.Id,
                PaymentId = payment.Id,
                Amount = booking.Service?.Price ?? 0
            });

            _context.BookingHistories.Add(new BookingHistory
            {
                BookingId = booking.Id,
                OldStatus = booking.Status,
                NewStatus = booking.Status,
                UserId = userId,
                ChangedAt = DateTime.UtcNow,
                Reason = isCash ? "Cash payment registered — to be collected on site" : "Payment registered",
                From = DateTime.UtcNow
            });

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                BookingId = booking.Id,
                Type = Notification.TypeBookingRequested,
                Title = "Booking request received",
                Body = "Your booking request has been received and is awaiting provider confirmation.",
                IsRead = false,
                From = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(payment.ToResponse());
    }
}
