using FlowDesk.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var users = await _db.Users
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.FirstName)
            .Select(x => new
            {
                x.Id,
                Name = x.FirstName + " " + x.LastName,
                x.Email,
                Role = x.Role.ToString()
            })
            .ToListAsync();

        return Ok(users);
    }
}
