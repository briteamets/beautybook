using App.DAL.EF;
using App.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApp.Areas.SalonOwner.ViewModels;

namespace WebApp.Areas.SalonOwner.Controllers;

public class DashboardController : SalonOwnerControllerBase
{
    public DashboardController(AppDbContext context, IStringLocalizer<SharedUiResource> localizer) : base(context, localizer)
    {
    }

    public async Task<IActionResult> Index()
    {
        var ownedSalonIds = await GetOwnedSalonIdsAsync();
        var salons = await Context.Salons
            .Include(s => s.Staff!)
            .ThenInclude(st => st.BeautyProvider!)
            .ThenInclude(p => p.Services)
            .Include(s => s.Bookings)
            .Where(s => ownedSalonIds.Contains(s.Id))
            .ToListAsync();

        var now = DateTime.UtcNow;
        var model = new SalonOwnerDashboardViewModel
        {
            Salons = salons
                .Select(s => new SalonOwnerDashboardSalonViewModel
                {
                    Id = s.Id,
                    Name = Translate(s.Name),
                    Address = s.Address,
                    StaffCount = s.Staff?.Count(st => st.To == null) ?? 0,
                    ServiceCount = s.Staff?
                        .Where(st => st.To == null)
                        .SelectMany(st => st.BeautyProvider?.Services ?? Enumerable.Empty<App.Domain.Service>())
                        .Count(service => service.To == null) ?? 0,
                    UpcomingBookingCount = s.Bookings?
                        .Count(b => b.AppointmentDateTime >= now && b.Status != App.Domain.Booking.StatusCancelled) ?? 0
                })
                .OrderBy(s => s.Name)
                .ToList()
        };

        return View(model);
    }
}
