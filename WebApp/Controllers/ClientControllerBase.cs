using System.Security.Claims;
using System.Text.Json;
using App.DAL.EF;
using App.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WebApp;

namespace WebApp.Controllers;

public abstract class ClientControllerBase : Controller
{
    protected readonly AppDbContext Context;

    protected ClientControllerBase(AppDbContext context)
    {
        Context = context;
    }

    protected Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userId!);
    }

    protected void SetSuccessMessage(string message)
    {
        TempData["SuccessMessage"] = message;
    }

    protected void SetErrorMessage(string message)
    {
        TempData["ErrorMessage"] = message;
    }

    protected string T(string key, params object[] arguments)
    {
        var localizer = HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedUiResource>>();
        return localizer[key, arguments];
    }

    protected void StoreTempData<T>(string key, T value)
    {
        TempData[key] = JsonSerializer.Serialize(value);
    }

    protected T? ReadTempData<T>(string key)
    {
        return TempData.TryGetValue(key, out var value) && value is string json
            ? JsonSerializer.Deserialize<T>(json)
            : default;
    }
}
