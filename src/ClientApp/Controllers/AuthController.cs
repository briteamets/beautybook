using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

// võtab loginvormi vastu ja saadab AuthApiServicele
namespace ClientApp.Controllers;

public class AuthController : Controller
{
    private readonly AuthApiService _authApiService;

    public AuthController(AuthApiService authApiService)
    {
        _authApiService = authApiService;
    }

    // kasutaja avab login lehe ja pärast sisselogimist suunatakse ta õigele lehele tagasi
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // kontrollib, et POST tuleb päriselt sellelt lehelt.
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model); // kui email või parool on tühi (ModelState validatsioon),
                                                     // tagastatakse vorm kohe veateatega.

        try
        {
            await _authApiService.LoginAsync(model.Email, model.Password); // kui kõik ok, delegeeritakse tegelik töö AuthApiService-le
            return LocalRedirect(string.IsNullOrWhiteSpace(model.ReturnUrl) ? "/" : model.ReturnUrl); // eduka sisselogimise järel suunatakse returnUrl-ile või avalehele.
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Login failed."); // kui LoginAsync viskab erandi
                                                                     // (vale parool, server ei vasta jne), kuvatakse vormi veateade.
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authApiService.LogoutAsync();
        return RedirectToAction(nameof(Login));
    }
}
