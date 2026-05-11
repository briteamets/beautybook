using App.DAL.EF;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/service-types")]
public class ServiceTypesApiController : BaseApiController
{
    private readonly AppDbContext _context;

    public ServiceTypesApiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceTypeResponse>>> GetServiceTypes([FromQuery] Guid? categoryId)
    {
        var query = _context.ServiceTypes
            .Where(t => t.IsActive)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(t => t.ServiceCategoryId == categoryId.Value);

        var types = await query.OrderBy(t => t.Name).ToListAsync();
        return Ok(types.Select(t => t.ToResponse()));
    }
}
