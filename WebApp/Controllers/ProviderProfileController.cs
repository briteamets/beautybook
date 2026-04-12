using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public class ProviderProfileController : ClientControllerBase
{
    public ProviderProfileController(AppDbContext context) : base(context)
    {
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, Guid? selectedServiceId = null, DateOnly? selectedDate = null, Guid? bookingId = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow);
        var provider = await Context.BeautyProviders
            .Where(p => p.Id == id && p.To == null)
            .Include(p => p.Services!.Where(s => s.To == null && s.IsActive))
                .ThenInclude(s => s.ServiceCategory)
            .Include(p => p.Services!)
                .ThenInclude(s => s.Bookings!)
                    .ThenInclude(b => b.Review)
            .Include(p => p.Availabilities!.Where(a =>
                a.To == null &&
                !a.IsBooked &&
                (a.Date > today || (a.Date == today && a.StartTime > nowTime))))
            .Include(p => p.Portfolio!)
                .ThenInclude(pp => pp.Service)
            .FirstOrDefaultAsync();

        if (provider == null) return NotFound();

        var reviews = provider.Services?
            .SelectMany(s => s.Bookings ?? [])
            .Where(b => b.Review != null)
            .Select(b => b.Review!)
            .OrderByDescending(r => r.From)
            .ToList() ?? [];

        var availableDates = provider.Availabilities?
            .Where(a => !a.IsBooked)
            .Select(a => a.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList() ?? [];

        var effectiveDate = selectedDate ?? availableDates.FirstOrDefault();
        if (effectiveDate == default) effectiveDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var isFavorite = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = GetCurrentUserId();
            isFavorite = await Context.FavoriteProviders.AnyAsync(f => f.UserId == userId && f.BeautyProviderId == provider.Id);
        }

        var vm = new ProviderProfileViewModel
        {
            Id = provider.Id,
            BookingId = bookingId,
            ProfileName = provider.ProfileName,
            ProfilePictureUrl = provider.ProfilePictureUrl,
            Bio = provider.Bio,
            Address = provider.Address,
            AverageRating = reviews.Count == 0 ? 0 : Math.Round((decimal)reviews.Average(r => r.Rating), 2),
            ReviewCount = reviews.Count,
            SelectedServiceId = selectedServiceId,
            SelectedDate = effectiveDate,
            IsFavorite = isFavorite,
            AvailableDates = availableDates,
            Services = provider.Services?
                .OrderBy(s => s.Name.Translate())
                .Select(s => new ServiceCardViewModel
                {
                    Id = s.Id,
                    Name = s.Name.Translate() ?? string.Empty,
                    Description = s.Description?.Translate(),
                    Price = s.Price,
                    DurationMinutes = s.DurationMinutes
                }).ToList() ?? [],
            Portfolio = provider.Portfolio?
                .OrderByDescending(p => p.From)
                .Select(p => new PortfolioImageViewModel
                {
                    ImageUrl = p.ImageUrl,
                    ServiceName = p.Service?.Name.Translate()
                }).ToList() ?? [],
            Reviews = reviews.Select(r => new ReviewItemViewModel
            {
                UserName = r.User?.Email ?? "User",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.From
            }).ToList(),
            TimeSlots = provider.Availabilities?
                .Where(a => a.Date == effectiveDate && !a.IsBooked)
                .OrderBy(a => a.StartTime)
                .Select(a => new AvailabilitySlotViewModel
                {
                    Id = a.Id,
                    StartTime = a.StartTime.ToString("HH:mm"),
                    EndTime = a.EndTime.ToString("HH:mm")
                }).ToList() ?? []
        };

        return View(vm);
    }
}
