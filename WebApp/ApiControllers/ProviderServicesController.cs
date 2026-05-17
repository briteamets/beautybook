using App.BLL.Services;
using App.Contracts;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/provider-services")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProviderServicesController : BaseApiController
{
    private readonly ISlotGeneratorService _slotGeneratorService;

    public ProviderServicesController(ISlotGeneratorService slotGeneratorService)
    {
        _slotGeneratorService = slotGeneratorService;
    }

    [HttpGet("{providerServiceId:guid}/slots")]
    public async Task<ActionResult<IEnumerable<AvailableSlotResponse>>> GetSlots(Guid providerServiceId, [FromQuery] DateOnly date)
    {
        if (date < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Ok(Array.Empty<AvailableSlotResponse>());
        }

        var slots = await _slotGeneratorService.GenerateSlotsAsync(providerServiceId, date);
        return Ok(slots
            .Where(s => s.IsAvailable)
            .Select(s => new AvailableSlotResponse
            {
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }));
    }
}
