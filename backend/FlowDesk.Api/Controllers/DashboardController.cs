using FlowDesk.Api.Data;
using FlowDesk.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var now = DateTime.UtcNow;
        var startToday = now.Date;
        var startWindow = startToday.AddDays(-6);

        var tickets = await db.Tickets
            .AsNoTracking()
            .Include(x => x.AssignedUser)
            .Include(x => x.Customer)
            .ToListAsync();

        var open = tickets.Count(x => x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed);
        var unassigned = tickets.Count(x => x.AssignedUserId == null && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed);
        var overdue = tickets.Count(x => x.DueAt < now && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed);
        var resolvedToday = tickets.Count(x => x.Status == TicketStatus.Resolved && x.UpdatedAt >= startToday);
        var resolvedTotal = tickets.Count(x => x.Status == TicketStatus.Resolved || x.Status == TicketStatus.Closed);
        var resolutionRate = tickets.Count == 0 ? 0 : Math.Round(resolvedTotal * 100d / tickets.Count, 1);

        var byStatus = tickets
            .GroupBy(x => x.Status)
            .Select(g => new { name = g.Key.ToString(), value = g.Count() })
            .OrderByDescending(x => x.value)
            .ToList();

        var byPriority = Enum.GetValues<TicketPriority>()
            .Select(p => new { name = p.ToString(), value = tickets.Count(x => x.Priority == p) })
            .ToList();

        var recent = tickets
            .OrderByDescending(x => x.UpdatedAt)
            .Take(6)
            .Select(x => new
            {
                x.Id,
                x.Reference,
                x.Title,
                status = x.Status.ToString(),
                priority = x.Priority.ToString(),
                customer = x.Customer?.Name,
                assignee = x.AssignedUser == null ? null : x.AssignedUser.FirstName + " " + x.AssignedUser.LastName,
                x.UpdatedAt,
                x.DueAt
            })
            .ToList();

        var teamWorkload = tickets
            .Where(x => x.AssignedUser != null && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed)
            .GroupBy(x => new { x.AssignedUserId, x.AssignedUser!.FirstName, x.AssignedUser.LastName })
            .Select(g => new
            {
                id = g.Key.AssignedUserId,
                name = g.Key.FirstName + " " + g.Key.LastName,
                initials = (g.Key.FirstName.Length > 0 ? g.Key.FirstName[0].ToString() : "") + (g.Key.LastName.Length > 0 ? g.Key.LastName[0].ToString() : ""),
                open = g.Count(),
                urgent = g.Count(x => x.Priority == TicketPriority.High || x.Priority == TicketPriority.Critical)
            })
            .OrderByDescending(x => x.open)
            .Take(6)
            .ToList();

        var volume = Enumerable.Range(0, 7)
            .Select(offset => startWindow.AddDays(offset))
            .Select(day => new
            {
                date = day.ToString("yyyy-MM-dd"),
                label = day.ToString("ddd"),
                created = tickets.Count(x => x.CreatedAt.Date == day.Date),
                resolved = tickets.Count(x => (x.Status == TicketStatus.Resolved || x.Status == TicketStatus.Closed) && x.UpdatedAt.Date == day.Date)
            })
            .ToList();

        return Ok(new
        {
            open,
            unassigned,
            overdue,
            resolvedToday,
            resolutionRate,
            total = tickets.Count,
            byStatus,
            byPriority,
            recent,
            teamWorkload,
            volume
        });
    }
}
