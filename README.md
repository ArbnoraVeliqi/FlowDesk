# FlowDesk

FlowDesk is a full-stack support ticket management application built for managing customer requests and day-to-day support work.

The application allows users to create and manage tickets, assign them to team members, track deadlines and statuses, add comments, and keep a history of ticket activity. It also includes dashboards and reports for monitoring workload and team performance.

## Features

* Ticket creation and management
* Ticket assignment and status tracking
* Customer management
* Comments and internal notes
* Ticket activity history
* Due dates and overdue ticket tracking
* Team workload and performance reports
* Dashboard with support metrics
* CSV report export
* User authentication

## Technologies

**Frontend:** React, TypeScript, Vite
**Backend:** ASP.NET Core, C#, Entity Framework Core
**Database:** SQL Server
**Authentication:** JWT, BCrypt

## Running the project

Make sure you have .NET 10, SQL Server, Node.js and npm installed.

Update the SQL Server connection string in:

`backend/FlowDesk.Api/appsettings.json`

Then run `FlowDesk.Api` from Visual Studio.

The database and demo data are created automatically the first time the application runs.

### Demo login

Email: `admin@flowdesk.dev`
Password: `Demo123!`

## Project structure

The React frontend is located in the `frontend` folder and the ASP.NET Core API in `backend/FlowDesk.Api`.

The frontend communicates with the API through REST endpoints, while Entity Framework Core is used for database access.
