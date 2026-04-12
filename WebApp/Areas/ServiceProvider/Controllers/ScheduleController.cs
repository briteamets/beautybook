using App.DAL.EF;
using App.Domain;
using App.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApp.Areas.ServiceProvider.ViewModels;
using WebApp;

namespace WebApp.Areas.ServiceProvider.Controllers;

public class ScheduleController : ServiceProviderControllerBase
{
    public ScheduleController(AppDbContext context, IStringLocalizer<SharedUiResource> localizer) : base(context, localizer)
    {
    }

    public async Task<IActionResult> Index()
    {
        var model = await BuildViewModelAsync();
        if (model == null) return RedirectToAction("Index", "Dashboard");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ProviderScheduleIndexViewModel model)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null)
        {
            Error("NoProviderProfile");
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var day in model.Days)
        {
            if (day.IsWorkingDay && day.StartTime >= day.EndTime)
            {
                ModelState.AddModelError(string.Empty, T("EndTimeAfterStartTime"));
            }
        }

        if (!ModelState.IsValid)
        {
            var vm = await BuildViewModelAsync(model.Days);
            return View("Index", vm);
        }

        foreach (var day in model.Days)
        {
            var schedule = await Context.ProviderSchedules
                .AsTracking()
                .FirstOrDefaultAsync(s => s.ProviderId == provider.Id && s.DayOfWeek == day.DayOfWeek);

            if (schedule == null)
            {
                schedule = new ProviderSchedule
                {
                    ProviderId = provider.Id,
                    DayOfWeek = day.DayOfWeek
                };
                Context.ProviderSchedules.Add(schedule);
            }

            schedule.IsWorkingDay = day.IsWorkingDay;
            schedule.StartTime = day.StartTime;
            schedule.EndTime = day.EndTime;
        }

        await Context.SaveChangesAsync();
        Success("ScheduleSaved");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBreak([Bind(Prefix = "NewBreak")] ProviderBreakFormViewModel model)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null)
        {
            Error("NoProviderProfile");
            return RedirectToAction("Index", "Dashboard");
        }

        if (model.BreakStart >= model.BreakEnd)
        {
            ModelState.AddModelError("NewBreak.BreakEnd", T("EndTimeAfterStartTime"));
        }

        if (!ModelState.IsValid)
        {
            var vm = await BuildViewModelAsync(newBreak: model);
            return View("Index", vm);
        }

        Context.ProviderBreaks.Add(new ProviderBreak
        {
            ProviderId = provider.Id,
            DayOfWeek = model.DayOfWeek,
            BreakStart = model.BreakStart,
            BreakEnd = model.BreakEnd
        });
        await Context.SaveChangesAsync();
        Success("BreakAdded");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveBreak(Guid id)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null) return RedirectToAction("Index", "Dashboard");

        var providerBreak = await Context.ProviderBreaks
            .AsTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.ProviderId == provider.Id);
        if (providerBreak == null) return NotFound();

        Context.ProviderBreaks.Remove(providerBreak);
        await Context.SaveChangesAsync();
        Success("BreakRemoved");
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProviderScheduleIndexViewModel?> BuildViewModelAsync(
        ICollection<ProviderScheduleDayViewModel>? postedDays = null,
        ProviderBreakFormViewModel? newBreak = null)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null)
        {
            Error("NoProviderProfile");
            return null;
        }

        var schedules = await Context.ProviderSchedules
            .Where(s => s.ProviderId == provider.Id)
            .ToListAsync();
        var breaks = await Context.ProviderBreaks
            .Where(b => b.ProviderId == provider.Id)
            .OrderBy(b => b.DayOfWeek)
            .ThenBy(b => b.BreakStart)
            .ToListAsync();

        var days = Enum.GetValues<DayOfWeek>()
            .Select(day =>
            {
                var posted = postedDays?.FirstOrDefault(d => d.DayOfWeek == day);
                var schedule = schedules.FirstOrDefault(s => s.DayOfWeek == day);

                return new ProviderScheduleDayViewModel
                {
                    DayOfWeek = day,
                    IsWorkingDay = posted?.IsWorkingDay ?? schedule?.IsWorkingDay ?? day is >= DayOfWeek.Monday and <= DayOfWeek.Friday,
                    StartTime = posted?.StartTime ?? schedule?.StartTime ?? new TimeSpan(9, 0, 0),
                    EndTime = posted?.EndTime ?? schedule?.EndTime ?? new TimeSpan(17, 0, 0),
                    Breaks = breaks
                        .Where(b => b.DayOfWeek == day)
                        .Select(b => new ProviderBreakListItemViewModel
                        {
                            Id = b.Id,
                            DayOfWeek = b.DayOfWeek,
                            BreakStart = b.BreakStart,
                            BreakEnd = b.BreakEnd
                        })
                        .ToList()
                };
            })
            .ToList();

        return new ProviderScheduleIndexViewModel
        {
            Days = days,
            NewBreak = newBreak ?? new ProviderBreakFormViewModel()
        };
    }
}
