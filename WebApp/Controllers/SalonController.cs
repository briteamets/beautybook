using App.DAL.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public class SalonController : ClientControllerBase
{
    public SalonController(AppDbContext context) : base(context)
    {
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, Guid? staffId = null, DateOnly? selectedDate = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow);
        var salon = await Context.Salons
            .Where(s => s.Id == id && s.To == null)
            .Include(s => s.Staff!.Where(st => st.To == null))
                .ThenInclude(st => st.BeautyProvider!)
                    .ThenInclude(p => p!.Services!.Where(service => service.To == null && service.IsActive))
            .Include(s => s.Staff!)
                .ThenInclude(st => st.BeautyProvider!)
                    .ThenInclude(p => p!.Availabilities!.Where(a =>
                        a.To == null &&
                        !a.IsBooked &&
                        (a.Date > today || (a.Date == today && a.StartTime > nowTime))))
            .Include(s => s.Staff!)
                .ThenInclude(st => st.BeautyProvider!)
                    .ThenInclude(p => p!.Services!)
                        .ThenInclude(service => service.Bookings!)
                            .ThenInclude(b => b.Review)
            .FirstOrDefaultAsync();

        if (salon == null) return NotFound();

        var allDates = salon.Staff?
            .SelectMany(st => st.BeautyProvider?.Availabilities ?? [])
            .Where(a => !a.IsBooked)
            .Select(a => a.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList() ?? [];

        var effectiveDate = selectedDate ?? allDates.FirstOrDefault();
        if (effectiveDate == default) effectiveDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var vm = new SalonDetailsViewModel
        {
            Id = salon.Id,
            Name = salon.Name.Translate() ?? string.Empty,
            Description = salon.Description?.Translate(),
            Address = salon.Address,
            ProfilePictureUrl = salon.ProfilePictureUrl,
            SelectedDate = effectiveDate,
            SelectedStaffId = staffId,
            AvailableDates = allDates,
            StaffMembers = salon.Staff?
                .Select(st =>
                {
                    var reviews = st.BeautyProvider?.Services?
                        .SelectMany(service => service.Bookings ?? [])
                        .Where(b => b.Review != null)
                        .Select(b => b.Review!)
                        .ToList() ?? [];

                    return new SalonStaffBookingViewModel
                    {
                        ProviderId = st.BeautyProviderId,
                        ProviderName = st.BeautyProvider?.ProfileName ?? string.Empty,
                        Role = st.Role,
                        ProfilePictureUrl = st.BeautyProvider?.ProfilePictureUrl,
                        AverageRating = reviews.Count == 0 ? 0 : Math.Round((decimal)reviews.Average(r => r.Rating), 2),
                        Services = st.BeautyProvider?.Services?
                            .OrderBy(s => s.Name.Translate())
                            .Select(s => new ServiceCardViewModel
                            {
                                Id = s.Id,
                                Name = s.Name.Translate() ?? string.Empty,
                                Description = s.Description?.Translate(),
                                Price = s.Price,
                                DurationMinutes = s.DurationMinutes
                            }).ToList() ?? [],
                        Slots = st.BeautyProvider?.Availabilities?
                            .Where(a => a.Date == effectiveDate && !a.IsBooked)
                            .OrderBy(a => a.StartTime)
                            .Select(a => new AvailabilitySlotViewModel
                            {
                                Id = a.Id,
                                StartTime = a.StartTime.ToString("HH:mm"),
                                EndTime = a.EndTime.ToString("HH:mm")
                            }).ToList() ?? []
                    };
                }).ToList() ?? []
        };

        var salonReviews = vm.StaffMembers.Select(s => s.AverageRating).Where(r => r > 0).ToList();
        vm.AverageRating = salonReviews.Count == 0 ? 0 : Math.Round(salonReviews.Average(), 2);

        return View(vm);
    }
}
