using App.DAL.EF;
using App.DAL.EF.Extensions;
using App.Domain;
using App.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApp.Areas.ServiceProvider.ViewModels;
using WebApp;

namespace WebApp.Areas.ServiceProvider.Controllers;

public class DashboardController : ServiceProviderControllerBase
{
    public DashboardController(AppDbContext context, IStringLocalizer<SharedUiResource> localizer) : base(context, localizer)
    {
    }

    public async Task<IActionResult> Index()
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null)
        {
            Error("NoProviderProfile");
            return RedirectToAction("Browse", "Home", new { area = "" });
        }

        var model = new DashboardViewModel
        {
            ProviderName = provider.ProfileName,
            ActiveServicesCount = await Context.Services.CountAsync(s =>
                s.BeautyProviderId == provider.Id && s.IsActive && s.To == null),
            PendingBookingsCount = await Context.Bookings
                .WhereActivelyPending()
                .CountAsync(b => b.Service!.BeautyProviderId == provider.Id),
            CompletedBookingsCount = await Context.Bookings.CountAsync(b =>
                b.Service!.BeautyProviderId == provider.Id && b.Status == Booking.StatusCompleted)
        };

        return View(model);
    }
}
