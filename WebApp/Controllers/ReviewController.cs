using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels;

namespace WebApp.Controllers;

[Authorize]
public class ReviewController : ClientControllerBase
{
    public ReviewController(AppDbContext context) : base(context)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid bookingId)
    {
        var vm = await BuildViewModel(bookingId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewCreateViewModel vm)
    {
        var hydrated = await BuildViewModel(vm.BookingId);
        if (hydrated == null) return NotFound();
        hydrated.Rating = vm.Rating;
        hydrated.Comment = vm.Comment;

        if (!ModelState.IsValid) return View(hydrated);

        var booking = await Context.Bookings
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == vm.BookingId && b.UserId == GetCurrentUserId() && b.To == null);
        if (booking == null) return NotFound();
        if (booking.Status != Booking.StatusCompleted)
        {
            SetErrorMessage(T("OnlyCompletedBookingsReviewed"));
            return RedirectToAction("Index", "MyBookings", new { tab = "Past" });
        }
        if (booking.Review != null)
        {
            SetErrorMessage(T("ReviewAlreadyExists"));
            return RedirectToAction("Index", "MyBookings", new { tab = "Past" });
        }

        Context.Reviews.Add(new Review
        {
            BookingId = booking.Id,
            UserId = booking.UserId,
            Rating = vm.Rating,
            Comment = vm.Comment,
            From = DateTime.UtcNow
        });
        await Context.SaveChangesAsync();

        SetSuccessMessage(T("ReviewSubmitted"));
        return RedirectToAction("Index", "MyBookings", new { tab = "Past" });
    }

    private async Task<ReviewCreateViewModel?> BuildViewModel(Guid bookingId)
    {
        var booking = await Context.Bookings
            .Include(b => b.Service)
                .ThenInclude(s => s!.BeautyProvider)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == GetCurrentUserId() && b.To == null);
        if (booking == null) return null;

        return new ReviewCreateViewModel
        {
            BookingId = bookingId,
            ProviderName = booking.Service?.BeautyProvider?.ProfileName ?? string.Empty,
            ProviderAddress = booking.Service?.BeautyProvider?.Address ?? string.Empty,
            ServiceName = booking.Service?.Name.Translate() ?? string.Empty,
            AppointmentDateTime = booking.AppointmentDateTime
        };
    }
}
