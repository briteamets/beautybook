using App.DAL.EF;
using App.Resources;
using Base.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WebApp;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public abstract class AdminControllerBase : Controller
{
    protected readonly AppDbContext Context;
    protected const int PageSize = 20;

    protected AdminControllerBase(AppDbContext context)
    {
        Context = context;
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

    protected static LangStr BuildLangStr(string? en, string? et)
    {
        var langStr = new LangStr();
        langStr["en"] = en?.Trim() ?? string.Empty;
        langStr["et"] = et?.Trim() ?? string.Empty;
        return langStr;
    }
}
