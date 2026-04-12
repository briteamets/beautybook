using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClientApp.Filters;

public class ClientApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not UnauthorizedAccessException) return;

        context.HttpContext.Session.Clear();
        context.Result = new RedirectToActionResult("Login", "Auth", null);
        context.ExceptionHandled = true;
    }
}
