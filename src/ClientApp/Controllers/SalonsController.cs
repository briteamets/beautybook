using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

public class SalonsController : Controller
{
    private readonly SalonsApiService _salonsApiService;

    public SalonsController(SalonsApiService salonsApiService)
    {
        _salonsApiService = salonsApiService;
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
            }).ToList()
        });
    }
}
