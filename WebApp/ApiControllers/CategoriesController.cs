using App.DAL.EF;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/categories")]
public class CategoriesController : BaseApiController
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceCategoryResponse>>> GetCategories()
    {
        var categories = await _context.ServiceCategories.OrderBy(c => c.Name).ToListAsync();
        return Ok(categories.Select(c => c.ToResponse()));
    }
}
