using App.Domain;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class BeautyProvidersController : AdminControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public BeautyProvidersController(App.DAL.EF.AppDbContext context, UserManager<AppUser> userManager) : base(context)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        page = Math.Max(1, page);
        var providerUserIds = (await _userManager.GetUsersInRoleAsync("ServiceProvider"))
            .Select(u => u.Id)
            .ToList();

        var query = Context.BeautyProviders
            .Include(p => p.User)
            .Include(p => p.Services!)
            .ThenInclude(s => s.Bookings)
            .Where(p => providerUserIds.Contains(p.UserId))
            .OrderBy(p => p.ProfileName);

        var totalItems = await query.CountAsync();
        var providers = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        return View(new PagedViewModel<ProviderIndexItem>
        {
            Items = providers.Select(p => new ProviderIndexItem
            {
                Id = p.Id,
                Name = p.ProfileName,
                Email = p.User?.Email ?? string.Empty,
                IsActive = p.To == null && (p.User?.IsActive ?? true),
                ServiceCount = p.Services?.Count ?? 0,
                BookingCount = p.Services?.Sum(s => s.Bookings?.Count ?? 0) ?? 0
            }).ToList(),
            PageNumber = page,
            TotalItems = totalItems,
            TotalPages = (int) Math.Ceiling(totalItems / (double) PageSize)
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var provider = await Context.BeautyProviders
            .Include(p => p.User)
            .Include(p => p.Services!)
            .ThenInclude(s => s.ServiceType)
            .Include(p => p.Services!)
            .ThenInclude(s => s.ServiceCategory)
            .Include(p => p.Services!)
            .ThenInclude(s => s.Bookings!)
            .ThenInclude(b => b.User)
            .Include(p => p.Schedules!)
            .Include(p => p.Availabilities!)
            .Include(p => p.SalonStaff!)
            .ThenInclude(ss => ss.Salon)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (provider == null) return NotFound();

        var serviceIds = provider.Services?.Select(s => s.Id).ToList() ?? new List<Guid>();
        var reviews = await Context.Reviews
            .Where(r => serviceIds.Contains(r.Booking!.ServiceId))
            .ToListAsync();

        var recentBookings = provider.Services?
            .SelectMany(s => s.Bookings ?? [])
            .OrderByDescending(b => b.AppointmentDateTime)
            .Take(10)
            .Select(b => new ProviderBookingItem
            {
                ClientName = b.User?.UserName ?? b.User?.Email ?? string.Empty,
                AppointmentDateTime = b.AppointmentDateTime,
                Status = b.Status
            })
            .ToList() ?? new List<ProviderBookingItem>();

        return View(new ProviderDetailsViewModel
        {
            Id = provider.Id,
            ProfileName = provider.ProfileName,
            Email = provider.User?.Email,
            Address = provider.Address,
            Bio = provider.Bio,
            Latitude = provider.Latitude,
            Longitude = provider.Longitude,
            From = provider.From,
            To = provider.To,
            IsSuspended = provider.To != null || provider.User?.IsActive == false,
            AverageRating = reviews.Count == 0 ? null : reviews.Average(r => r.Rating),
            Services = provider.Services?.Select(s => new ProviderServiceItem
            {
                ServiceType = s.ServiceType?.Name.Translate() ?? s.Name.Translate() ?? string.Empty,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                IsActive = s.IsActive
            }).ToList() ?? new List<ProviderServiceItem>(),
            Schedules = provider.Schedules?.OrderBy(s => s.DayOfWeek).Select(s => new ProviderScheduleItem
            {
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsWorkingDay = s.IsWorkingDay
            }).ToList() ?? new List<ProviderScheduleItem>(),
            RecentBookings = recentBookings
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id)
    {
        var provider = await Context.BeautyProviders.AsTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (provider == null) return NotFound();
        provider.To = null;
        var user = await Context.Users.AsTracking().FirstOrDefaultAsync(u => u.Id == provider.UserId);
        if (user != null) user.IsActive = true;
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("ProviderActivated"));
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var provider = await Context.BeautyProviders.AsTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (provider == null) return NotFound();
        provider.To = DateTime.UtcNow;
        var user = await Context.Users.AsTracking().FirstOrDefaultAsync(u => u.Id == provider.UserId);
        if (user != null) user.IsActive = false;
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("ProviderSuspended"));
        return RedirectToAction(nameof(Details), new { id });
    }
}
