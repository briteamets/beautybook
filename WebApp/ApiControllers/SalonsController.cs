using App.DAL.EF;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/salons")]
public class SalonsController : BaseApiController
{
    private readonly AppDbContext _context;

    public SalonsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalonResponse>>> GetSalons()
    {
        var salons = await _context.Salons
            .Where(s => s.To == null)
            .Include(s => s.Staff!.Where(st => st.To == null))
            .ThenInclude(st => st.BeautyProvider)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return Ok(salons.Select(s => s.ToResponse()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalonResponse>> GetSalon(Guid id)
    {
        var salon = await _context.Salons
            .Where(s => s.Id == id && s.To == null)
            .Include(s => s.Staff!.Where(st => st.To == null))
            .ThenInclude(st => st.BeautyProvider)
            .FirstOrDefaultAsync();
        if (salon == null) return NotFound();

        return Ok(salon.ToResponse());
    }
}
