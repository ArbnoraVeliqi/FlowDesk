using FlowDesk.Api.Data;
using FlowDesk.Api.DTOs;
using FlowDesk.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(x =>
                x.Name.Contains(search) ||
                x.Email.Contains(search) ||
                (x.Company ?? string.Empty).Contains(search));
        }

        var customers = await query.OrderByDescending(x => x.Id).ToListAsync();
        return Ok(customers);
    }

    [HttpPost]
    public async Task<IActionResult> Post(CustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Name and email are required." });

        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone,
            Company = request.Company
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return Ok(customer);
    }
}
