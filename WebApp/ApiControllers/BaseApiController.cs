using System.Net;
using System.Security.Claims;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected Guid GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                          User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException("Current user id claim is missing or invalid.");
        }

        return userId;
    }

    protected bool IsCurrentUser(Guid userId)
    {
        try
        {
            return GetCurrentUserId() == userId;
        }
        catch
        {
            return false;
        }
    }

    protected ActionResult ApiError(HttpStatusCode statusCode, string message)
    {
        return StatusCode((int) statusCode, new RestApiErrorResponse
        {
            Status = statusCode,
            Error = message
        });
    }
}
