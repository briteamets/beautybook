using System.ComponentModel.DataAnnotations;
using App.Domain.Identity;
using App.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace WebApp.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IStringLocalizer<SharedUiResource> _localizer;

    public RegisterModel(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ILogger<RegisterModel> logger,
        IStringLocalizer<SharedUiResource> localizer)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _localizer = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        public bool IsProviderApplicant { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new AppUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            IsProviderApplicant = Input.IsProviderApplicant,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Client");
            _logger.LogInformation("User created a new account with password.");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    public string T(string key) => _localizer[key];
}
