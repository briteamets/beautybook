using System.Net;
using App.Contracts;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/services")]
public class ServicesController : BaseApiController
{
    private readonly IServiceManagementService _serviceManagement;

    public ServicesController(IServiceManagementService serviceManagement)
    {
        _serviceManagement = serviceManagement;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceResponse>>> GetServices(
        [FromQuery] Guid? providerId,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? serviceTypeId)
    {
        var services = await _serviceManagement.GetPublicAsync(providerId, categoryId, serviceTypeId);
        return Ok(services.Select(s => s.ToResponse()));
    }

    [HttpGet("mine")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IEnumerable<ServiceResponse>>> GetMyServices()
    {
        var services = await _serviceManagement.GetByProviderUserIdAsync(GetCurrentUserId());
        return Ok(services.Select(s => s.ToResponse()));
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ServiceResponse>> PostService([FromBody] ServiceCreateRequest request)
    {
        try
        {
            var service = await _serviceManagement.CreateAsync(GetCurrentUserId(), request);
            return CreatedAtAction(nameof(GetServices), new { version = "1.0" }, service.ToResponse());
        }
        catch (KeyNotFoundException ex) { return ApiError(HttpStatusCode.NotFound, ex.Message); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return ApiError(HttpStatusCode.BadRequest, ex.Message); }
    }

    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ServiceResponse>> PutService(Guid id, [FromBody] ServiceUpdateRequest request)
    {
        try
        {
            var service = await _serviceManagement.UpdateAsync(id, GetCurrentUserId(), request);
            return Ok(service.ToResponse());
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult> DeleteService(Guid id)
    {
        try
        {
            await _serviceManagement.SoftDeleteAsync(id, GetCurrentUserId());
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
