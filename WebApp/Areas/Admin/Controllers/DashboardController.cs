using App.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Domain.Identity;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class DashboardController : AdminControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public DashboardController(App.DAL.EF.AppDbContext context, UserManager<AppUser> userManager) : base(context)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var clientCount = await CountUsersInRoles("Client", "User");
        var providerCount = await CountUsersInRoles("ServiceProvider");
        var bookingsToday = await Context.Bookings.CountAsync(b => b.BookingDate == today);

        var activity = new List<AdminActivityFeedItem>();

        var recentUsers = await Context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(20)
            .ToListAsync();

        foreach (var user in recentUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            activity.Add(new AdminActivityFeedItem
            {
                ActivityType = "UserRegistration",
                PrimaryText = user.UserName ?? user.Email ?? string.Empty,
                SecondaryText = string.Join(", ", roles),
                Timestamp = user.CreatedAt
            });
        }

        var recentBookings = await Context.Bookings
            .Include(b => b.User)
            .Include(b => b.Service)
            .ThenInclude(s => s!.BeautyProvider)
            .OrderByDescending(b => b.BookedAt)
            .Take(20)
            .ToListAsync();

        activity.AddRange(recentBookings.Select(b => new AdminActivityFeedItem
        {
            ActivityType = "BookingCreated",
            PrimaryText = b.User?.Email ?? string.Empty,
            SecondaryText = $"{b.Service?.BeautyProvider?.ProfileName} - {b.Service?.Name.Translate()}",
            Timestamp = b.BookedAt
        }));

        var cancellations = await Context.BookingHistories
            .Include(h => h.User)
            .Include(h => h.Booking)
            .ThenInclude(b => b!.Service)
            .Where(h => h.NewStatus == Booking.StatusCancelled)
            .OrderByDescending(h => h.ChangedAt)
            .Take(20)
            .ToListAsync();

        activity.AddRange(cancellations.Select(h => new AdminActivityFeedItem
        {
            ActivityType = "BookingCancelled",
            PrimaryText = h.User?.Email ?? string.Empty,
            SecondaryText = h.Booking?.Service?.Name.Translate() ?? string.Empty,
            Timestamp = h.ChangedAt
        }));

        return View(new AdminDashboardViewModel
        {
            TotalClientUsers = clientCount,
            TotalServiceProviders = providerCount,
            TotalBookingsToday = bookingsToday,
            RecentActivity = activity.OrderByDescending(a => a.Timestamp).Take(20).ToList()
        });
    }

    private async Task<int> CountUsersInRoles(params string[] roles)
    {
        var ids = new HashSet<Guid>();
        foreach (var role in roles)
        {
            if (!await Context.Roles.AnyAsync(r => r.Name == role)) continue;
            foreach (var user in await _userManager.GetUsersInRoleAsync(role))
            {
                ids.Add(user.Id);
            }
        }

        return ids.Count;
    }
}
