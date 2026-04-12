using ClientApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClientApp.Filters;

public class ClientAuthorizeAttribute : Attribute, IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Session.HasUsableToken())
        {
            context.Result = new RedirectToActionResult("Login", "Auth", new
            {
                returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString
            });
            return Task.CompletedTask;
        }

        return next();
    }
}
