using System.Security.Claims;
using FlowDesk.Api.Data;
using FlowDesk.Api.DTOs;
using FlowDesk.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TicketsController(AppDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? q,
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Tickets
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.AssignedUser)
            .Include(t => t.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(t =>
                t.Title.Contains(search) ||
                t.Reference.Contains(search) ||
                t.Customer!.Name.Contains(search));
        }

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        var total = await query.CountAsync();

        // Pagination is applied in memory for compatibility with older SQL Server
        // database compatibility levels that do not support OFFSET/FETCH.
        var allItems = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new
            {
                t.Id,
                t.Reference,
                t.Title,
                Priority = t.Priority.ToString(),
                Status = t.Status.ToString(),
                Customer = t.Customer!.Name,
                Assignee = t.AssignedUser == null
                    ? null
                    : t.AssignedUser.FirstName + " " + t.AssignedUser.LastName,
                Category = t.Category == null ? null : t.Category.Name,
                t.CreatedAt,
                t.UpdatedAt,
                t.DueAt
            })
            .ToListAsync();

        var items = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> One(int id)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.AssignedUser)
            .Include(x => x.Category)
            .Include(x => x.Comments)
                .ThenInclude(c => c.User)
            .Include(x => x.Activities)
                .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        return ticket == null ? NotFound() : Ok(ticket);
    }

    [HttpPost]
    public async Task<IActionResult> Post(TicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { message = "Title and description are required." });

        var nextNumber = await _db.Tickets.CountAsync() + 1;
        var ticket = new Ticket
        {
            Reference = $"TCK-{DateTime.UtcNow:yyyy}-{nextNumber:0000}",
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Priority = request.Priority,
            CustomerId = request.CustomerId,
            CategoryId = request.CategoryId,
            AssignedUserId = request.AssignedUserId,
            DueAt = CalculateDueDate(request.Priority)
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        _db.TicketActivities.Add(new TicketActivity
        {
            TicketId = ticket.Id,
            UserId = CurrentUserId(),
            Action = "Ticket created",
            Details = ticket.Title
        });

        await _db.SaveChangesAsync();
        return Ok(ticket);
    }

    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> Comment(int id, CommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { message = "Comment cannot be empty." });

        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket == null)
            return NotFound();

        var comment = new TicketComment
        {
            TicketId = id,
            UserId = CurrentUserId(),
            Body = request.Body.Trim(),
            IsInternal = request.IsInternal
        };

        _db.TicketComments.Add(comment);
        _db.TicketActivities.Add(new TicketActivity
        {
            TicketId = id,
            UserId = CurrentUserId(),
            Action = request.IsInternal ? "Internal note added" : "Comment added"
        });

        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(comment);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> Status(int id, StatusRequest request)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket == null)
            return NotFound();

        var oldStatus = ticket.Status;
        ticket.Status = request.Status;
        ticket.UpdatedAt = DateTime.UtcNow;

        _db.TicketActivities.Add(new TicketActivity
        {
            TicketId = id,
            UserId = CurrentUserId(),
            Action = "Status changed",
            Details = $"{oldStatus} → {request.Status}"
        });

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPatch("{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, AssignRequest request)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket == null)
            return NotFound();

        ticket.AssignedUserId = request.UserId;
        ticket.UpdatedAt = DateTime.UtcNow;

        _db.TicketActivities.Add(new TicketActivity
        {
            TicketId = id,
            UserId = CurrentUserId(),
            Action = "Assignment changed",
            Details = request.UserId?.ToString() ?? "Unassigned"
        });

        await _db.SaveChangesAsync();
        return Ok();
    }

    private static DateTime CalculateDueDate(TicketPriority priority)
    {
        var now = DateTime.UtcNow;

        return priority switch
        {
            TicketPriority.Critical => now.AddDays(1),
            TicketPriority.High => now.AddDays(2),
            TicketPriority.Low => now.AddDays(5),
            _ => now.AddDays(3)
        };
    }
}
