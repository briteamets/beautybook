using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

public class SalonsController : Controller
{
    private readonly SalonsApiService _salonsApiService;
    private readonly ReviewsApiService _reviewsApiService;

    public SalonsController(SalonsApiService salonsApiService, ReviewsApiService reviewsApiService)
    {
        _salonsApiService = salonsApiService;
        _reviewsApiService = reviewsApiService;
    }

    public async Task<IActionResult> Index()
    {
        ICollection<App.DTO.v1.SalonResponse> salons;
        try
        {
            salons = await _salonsApiService.GetAllAsync();
        }
        catch (HttpRequestException)
        {
            salons = [];
        }

        return View(new SalonsIndexViewModel
        {
            Salons = salons.Select(s => new SalonListItemViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                ProfilePictureUrl = s.ProfilePictureUrl,
                StaffCount = s.StaffMembers.Count
            }).ToList()
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        App.DTO.v1.SalonResponse? salon;
        try
        {
            salon = await _salonsApiService.GetAsync(id);
        }
        catch (HttpRequestException)
        {
            return NotFound();
        }

        if (salon == null) return NotFound();

        var reviews = new List<ReviewListItemViewModel>();
        foreach (var member in salon.StaffMembers)
        {
            var staffReviews = await _reviewsApiService.GetByProviderIdAsync(member.ProviderId);
            reviews.AddRange(staffReviews.Select(r => new ReviewListItemViewModel
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                UserName = r.UserName,
                CreatedAt = r.CreatedAt,
                ProviderName = member.ProviderName
            }));
        }

        return View(new SalonDetailsViewModel
        {
            Id = salon.Id,
            Name = salon.Name,
            Description = salon.Description,
            Address = salon.Address,
            Lat = salon.Lat,
            Lng = salon.Lng,
            ProfilePictureUrl = salon.ProfilePictureUrl,
            Staff = salon.StaffMembers.Select(s => new SalonStaffViewModel
            {
                ProviderId = s.ProviderId,
                ProviderName = s.ProviderName,
                Role = s.Role,
                ProfilePictureUrl = s.ProfilePictureUrl
            }).ToList(),
            Reviews = reviews.OrderByDescending(r => r.CreatedAt).ToList()
        });
    }
}
