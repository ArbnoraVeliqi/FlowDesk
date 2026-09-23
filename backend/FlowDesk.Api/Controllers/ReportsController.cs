using FlowDesk.Api.Data;
using FlowDesk.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 7, 365);
        var now = DateTime.UtcNow;
        var from = now.Date.AddDays(-(days - 1));
        var tickets = await db.Tickets.AsNoTracking().Include(x => x.AssignedUser).Include(x => x.Category).ToListAsync();
        var period = tickets.Where(x => x.CreatedAt >= from).ToList();
        var completed = period.Where(x => x.Status is TicketStatus.Resolved or TicketStatus.Closed).ToList();
        var overdue = period.Count(x => x.DueAt < now && x.Status is not TicketStatus.Resolved and not TicketStatus.Closed);
        var slaMet = completed.Count(x => !x.DueAt.HasValue || x.UpdatedAt <= x.DueAt.Value);
        var avgHours = completed.Count == 0 ? 0 : Math.Round(completed.Average(x => Math.Max(0, (x.UpdatedAt - x.CreatedAt).TotalHours)), 1);

        var daily = Enumerable.Range(0, days).Select(i => from.AddDays(i)).Select(day => new {
            date = day.ToString("yyyy-MM-dd"), label = day.ToString("dd MMM"),
            created = period.Count(x => x.CreatedAt.Date == day.Date),
            resolved = period.Count(x => (x.Status is TicketStatus.Resolved or TicketStatus.Closed) && x.UpdatedAt.Date == day.Date)
        });
        var byStatus = Enum.GetValues<TicketStatus>().Select(s => new { name=s.ToString(), value=period.Count(x=>x.Status==s) });
        var byPriority = Enum.GetValues<TicketPriority>().Select(p => new { name=p.ToString(), value=period.Count(x=>x.Priority==p) });
        var byCategory = period.GroupBy(x => x.Category?.Name ?? "Uncategorized").Select(g => new { name=g.Key, value=g.Count() }).OrderByDescending(x=>x.value);

        var agents = await db.Users.AsNoTracking().Where(x => x.IsActive).ToListAsync();
        var performance = agents.Select(u => {
            var assigned = period.Where(x => x.AssignedUserId == u.Id).ToList();
            var resolved = assigned.Where(x => x.Status is TicketStatus.Resolved or TicketStatus.Closed).ToList();
            var met = resolved.Count(x => !x.DueAt.HasValue || x.UpdatedAt <= x.DueAt.Value);
            return new {
                u.Id, name=u.FirstName+" "+u.LastName, role=u.Role.ToString(), assigned=assigned.Count,
                active=assigned.Count(x=>x.Status is not TicketStatus.Resolved and not TicketStatus.Closed), resolved=resolved.Count,
                overdue=assigned.Count(x=>x.DueAt < now && x.Status is not TicketStatus.Resolved and not TicketStatus.Closed),
                avgResolutionHours=resolved.Count==0?0:Math.Round(resolved.Average(x=>Math.Max(0,(x.UpdatedAt-x.CreatedAt).TotalHours)),1),
                slaPercent=resolved.Count==0?0:Math.Round(met*100d/resolved.Count,1)
            };
        }).OrderByDescending(x=>x.resolved).ToList();

        return Ok(new { days, from, to=now, created=period.Count, resolved=completed.Count, active=period.Count-completed.Count,
            overdue, avgResolutionHours=avgHours, resolutionRate=period.Count==0?0:Math.Round(completed.Count*100d/period.Count,1),
            slaPercent=completed.Count==0?0:Math.Round(slaMet*100d/completed.Count,1), daily, byStatus, byPriority, byCategory, performance });
    }

    [HttpGet("processes")]
    public async Task<IActionResult> Processes()
    {
        var now=DateTime.UtcNow;
        var tickets=await db.Tickets.AsNoTracking().Include(x=>x.AssignedUser).Include(x=>x.Customer).ToListAsync();
        var stages=Enum.GetValues<TicketStatus>().Select(s=>new{name=s.ToString(),value=tickets.Count(x=>x.Status==s)});
        var attention = tickets
            .Where(x => x.Status is not TicketStatus.Resolved and not TicketStatus.Closed)
            .OrderBy(x => x.DueAt)
            .Take(10)
            .Select(x => new
            {
                x.Id,
                x.Reference,
                x.Title,
                Status = x.Status.ToString(),
                Priority = x.Priority.ToString(),
                Customer = x.Customer!.Name,
                Assignee = x.AssignedUser == null
                    ? null
                    : x.AssignedUser.FirstName + " " + x.AssignedUser.LastName,
                x.DueAt,
                Overdue = x.DueAt < now
            });
        return Ok(new { stages, backlog=tickets.Count(x=>x.Status is not TicketStatus.Resolved and not TicketStatus.Closed),
            unassigned=tickets.Count(x=>x.AssignedUserId==null && x.Status is not TicketStatus.Resolved and not TicketStatus.Closed),
            overdue=tickets.Count(x=>x.DueAt<now && x.Status is not TicketStatus.Resolved and not TicketStatus.Closed), attention });
    }
}
