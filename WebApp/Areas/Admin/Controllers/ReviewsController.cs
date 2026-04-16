using App.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class ReviewsController : AdminControllerBase
{
    public ReviewsController(App.DAL.EF.AppDbContext context) : base(context)
    {
    }

    public async Task<IActionResult> Index(int? minRating, int? maxRating, Guid? providerId, int page = 1)
    {
        page = Math.Max(1, page);
        var query = Context.Reviews
            .Include(r => r.User)
            .Include(r => r.Booking)
            .ThenInclude(b => b!.Service)
            .ThenInclude(s => s!.BeautyProvider)
            .AsQueryable();

        if (minRating.HasValue)
            query = query.Where(r => r.Rating >= minRating.Value);
        if (maxRating.HasValue)
            query = query.Where(r => r.Rating <= maxRating.Value);
        if (providerId.HasValue)
            query = query.Where(r => r.Booking!.Service!.BeautyProviderId == providerId.Value);

        query = query.OrderByDescending(r => r.From);

        var totalItems = await query.CountAsync();
        var items = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        var providers = await Context.BeautyProviders
            .OrderBy(p => p.ProfileName)
            .Select(p => new SelectListItem(p.ProfileName, p.Id.ToString(), p.Id == providerId))
            .ToListAsync();

        return View(new ReviewIndexViewModel
        {
            Filter = new ReviewIndexFilterViewModel
            {
                MinRating = minRating,
                MaxRating = maxRating,
                ProviderId = providerId
            },
            Providers = providers,
            Page = new PagedViewModel<ReviewIndexItem>
            {
                Items = items.Select(r => new ReviewIndexItem
                {
                    Id = r.Id,
                    UserEmail = r.User?.Email ?? string.Empty,
                    ServiceName = r.Booking?.Service?.Name.Translate() ?? string.Empty,
                    ProviderName = r.Booking?.Service?.BeautyProvider?.ProfileName ?? string.Empty,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.From
                }).ToList(),
                PageNumber = page,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize)
            }
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var review = await Context.Reviews
            .Include(r => r.User)
            .Include(r => r.Booking)
            .ThenInclude(b => b!.Service)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (review == null) return NotFound();
        return View(review);
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var review = await Context.Reviews
            .Include(r => r.User)
            .Include(r => r.Booking)
            .ThenInclude(b => b!.Service)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (review == null) return NotFound();
        return View(review);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var review = await Context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
        if (review == null) return NotFound();
        Context.Reviews.Remove(review);
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("ReviewDeleted"));
        return RedirectToAction(nameof(Index));
    }
}
