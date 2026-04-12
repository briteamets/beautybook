using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

public class AuthController : Controller
{
    private readonly AuthApiService _authApiService;

    public AuthController(AuthApiService authApiService)
    {
        _authApiService = authApiService;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _authApiService.LoginAsync(model.Email, model.Password);
            return LocalRedirect(string.IsNullOrWhiteSpace(model.ReturnUrl) ? "/" : model.ReturnUrl);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Login failed.");
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
