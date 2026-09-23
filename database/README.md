# Database
FlowDesk uses SQL Server and Entity Framework Core. On first run the API creates the `FlowDesk` database and seeds demo users, customers, categories and tickets through `Database.EnsureCreated()`.

Default connection: `Server=localhost;Database=FlowDesk;Trusted_Connection=True;TrustServerCertificate=True`.
Change `backend/FlowDesk.Api/appsettings.json` if your SQL Server instance has a different name.
