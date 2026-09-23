using FlowDesk.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Api.Controllers;

[ApiController]
[Route("api/meta")]
[Authorize]
public class MetaController : ControllerBase
{
    private readonly AppDbContext _db;

    public MetaController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var categories = await _db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        var customers = await _db.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        var users = await _db.Users
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.FirstName)
            .Select(x => new
            {
                x.Id,
                Name = x.FirstName + " " + x.LastName
            })
            .ToListAsync();

        return Ok(new { categories, customers, users });
    }
}
