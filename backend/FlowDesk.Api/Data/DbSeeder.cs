using FlowDesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        var admin = new User
        {
            FirstName = "Arta",
            LastName = "Berisha",
            Email = "admin@flowdesk.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!"),
            Role = UserRole.Admin
        };

        var agent = new User
        {
            FirstName = "Leon",
            LastName = "Krasniqi",
            Email = "agent@flowdesk.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!"),
            Role = UserRole.Agent
        };

        db.Users.AddRange(admin, agent);
        db.Categories.AddRange(
            new Category { Name = "Technical", Description = "Product and technical issues" },
            new Category { Name = "Billing", Description = "Payments and invoices" },
            new Category { Name = "General", Description = "General questions" });

        db.Customers.AddRange(
            new Customer { Name = "Elira Hoxha", Email = "elira@example.com", Company = "Northstar Studio" },
            new Customer { Name = "Dren Gashi", Email = "dren@example.com", Company = "Atlas Commerce" });

        await db.SaveChangesAsync();

        var customers = await db.Customers.OrderBy(x => x.Id).ToListAsync();
        var categories = await db.Categories.OrderBy(x => x.Id).ToListAsync();

        db.Tickets.AddRange(
            new Ticket
            {
                Reference = "TCK-2026-0001",
                Title = "Unable to access team workspace",
                Description = "The customer receives an authorization error after signing in.",
                Priority = TicketPriority.High,
                Status = TicketStatus.InProgress,
                CustomerId = customers[0].Id,
                AssignedUserId = agent.Id,
                CategoryId = categories[0].Id,
                DueAt = DateTime.UtcNow.AddDays(1)
            },
            new Ticket
            {
                Reference = "TCK-2026-0002",
                Title = "Invoice contains an outdated company name",
                Description = "Customer requested an invoice correction for the latest billing cycle.",
                Priority = TicketPriority.Normal,
                Status = TicketStatus.New,
                CustomerId = customers[1].Id,
                CategoryId = categories[1].Id,
                DueAt = DateTime.UtcNow.AddDays(3)
            });

        await db.SaveChangesAsync();
    }
}
