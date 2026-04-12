using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Admin.Controllers;

public class HomeController : AdminControllerBase
{
    public HomeController(App.DAL.EF.AppDbContext context) : base(context)
    {
    }

    public IActionResult Index()
    {
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }
}
