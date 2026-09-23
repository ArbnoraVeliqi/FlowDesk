# FlowDesk

FlowDesk is a support ticket and operations management application for small support teams. I built it to cover the day-to-day flow of receiving customer issues, assigning work, tracking deadlines and reviewing team performance from one place.

The application uses a React frontend and an ASP.NET Core API backed by SQL Server. The frontend is also built into the API project for a simple single-app deployment.

## What it does

- Create, search and filter support tickets
- Assign tickets to team members and track status changes
- Manage customers and ticket categories
- Add customer replies and internal notes
- Keep an activity history for each ticket
- Track due dates, overdue work and SLA-style metrics
- View team workload and agent performance
- Review ticket volume, priorities, categories and resolution trends
- Monitor backlog and tickets that need attention
- Export performance data to CSV
- Authenticate users with JWT-based login

## Tech stack

**Frontend**
- React
- TypeScript
- Vite
- React Router

**Backend**
- ASP.NET Core / .NET 10
- C#
- Entity Framework Core
- JWT authentication
- BCrypt password hashing
- Swagger / OpenAPI

**Database**
- SQL Server

## Project structure

```text
FlowDesk/
├── backend/
│   └── FlowDesk.Api/
│       ├── Controllers/
│       ├── Data/
│       ├── DTOs/
│       ├── Models/
│       ├── Services/
│       ├── Program.cs
│       └── appsettings.json
├── frontend/
│   └── src/
│       ├── api/
│       ├── layouts/
│       ├── pages/
│       └── styles/
├── database/
└── FlowDesk.sln
```

## How it works

The React client communicates with the ASP.NET Core API over HTTP. Controllers handle requests and use Entity Framework Core through `AppDbContext` to read and update SQL Server data.

```text
React + TypeScript
        |
        | HTTP / JSON
        v
ASP.NET Core Web API
        |
        | Entity Framework Core
        v
     SQL Server
```

Authentication is handled with JWT tokens. After login, the client sends the token with protected API requests. Passwords are stored as BCrypt hashes rather than plain text.

## Database setup

For local development, FlowDesk creates the `FlowDesk` database automatically from the Entity Framework model when the application starts for the first time. A small `DbSeeder` adds demo users, categories, customers and tickets only when the user table is empty.

The default connection string expects a local SQL Server instance:

```text
Server=localhost;Database=FlowDesk;Trusted_Connection=True;TrustServerCertificate=True
```

If your SQL Server uses another instance name, update `backend/FlowDesk.Api/appsettings.json` before starting the application.

The current local/demo setup uses `EnsureCreatedAsync()` to keep first-time setup simple. For a production deployment I would use versioned EF Core migrations so schema changes can be applied safely between releases.

## Running locally

Requirements:
- .NET 10 SDK
- SQL Server
- Node.js and npm
- Visual Studio 2022 or newer is recommended on Windows

The easiest way to run the project is to open `FlowDesk.sln` and start `FlowDesk.Api`.

The API project builds the React frontend automatically. During a normal build it runs `npm install` and `npm run build`, then copies the generated frontend files into the API `wwwroot` directory.

You can also run the projects separately while developing:

```bash
cd backend/FlowDesk.Api
dotnet restore
dotnet run
```

```bash
cd frontend
npm install
npm run dev
```

Demo account:

```text
Email: admin@flowdesk.dev
Password: Demo123!
```

## Main API areas

```text
/api/auth       Login
/api/tickets    Ticket queue, details, comments, status and assignment
/api/customers  Customer records
/api/users      Active users and agents
/api/meta       Lookup data used by forms
/api/dashboard  Dashboard metrics
/api/reports    Reporting, performance and process metrics
```

Swagger is available in the Development environment when the API is running.

## Reporting and operations

The reporting section uses the same ticket data as the rest of the application. It can summarize a selected period and show created/resolved tickets, backlog, overdue work, resolution rate, SLA compliance, average resolution time and performance by team member.

The Processes view focuses on the operational side: how many tickets are in each workflow stage, how much work is unassigned, what is overdue and which tickets need attention next.

## Notes

This is an actively developed project. A few improvements I would make for a production environment are EF Core migrations, refresh-token handling, stricter role-based permissions, automated tests, external file storage for attachments, CI/CD and deployment-specific secret management.
