using App.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class ReviewsController : AdminControllerBase
{
    public ReviewsController(App.DAL.EF.AppDbContext context) : base(context)
    {
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var query = Context.Reviews
            .Include(r => r.User)
            .Include(r => r.Booking)
            .ThenInclude(b => b!.Service)
            .OrderByDescending(r => r.From);

        var totalItems = await query.CountAsync();
        var items = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        return View(new PagedViewModel<Review>
        {
            Items = items,
            PageNumber = page,
            TotalItems = totalItems,
            TotalPages = (int) Math.Ceiling(totalItems / (double) PageSize)
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
