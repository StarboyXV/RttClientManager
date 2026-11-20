# RTT Client Manager

A 3-tier Client Management web application built with ASP.NET Core (.NET 10), REST Web API, raw ADO.NET (no Entity Framework or stored procedures), and SQL Server.

## Solution Architecture
- **Rtt.ClientManager.Common**: Shared DTOs, paged response wrappers, and data contracts.
- **Rtt.ClientManager.Data**: Raw ADO.NET (`Microsoft.Data.SqlClient`) repositories using parameterized SQL queries, explicit database transactions, and `OFFSET/FETCH` pagination.
- **Rtt.ClientManager.Bussiness**: Domain validation, orchestration services, and streaming CSV export logic.
- **Rtt.ClientManager.Api**: RESTful ASP.NET Core Web API exposing `/api/client` and `/api/lookups` endpoints.
- **Rtt.ClientManager.Web**: ASP.NET Core MVC presentation layer communicating exclusively with the REST API via a typed `HttpClient`.

## Prerequisites
- .NET 10 SDK / Visual Studio 2026 (or 2022+)
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) or SQL Server Express/Developer

## Setup & Running Instructions
1. **Database Setup**:
   - Open SQL Server Management Studio (SSMS) or Visual Studio's **SQL Server Object Explorer**.
   - Connect to `(localdb)\MSSQLLocalDB` (or your local SQL Server instance).
   - Open and execute `Database/01_CreateSchema.sql` to create `RttClientManagerDb`, tables, indexes, and seed lookup values.
2. **Verify Connection String**:
   - In `Rtt.ClientManager/Rtt.ClientManager.Api/appsettings.json`, confirm `ConnectionStrings:DefaultConnection` points to your SQL Server instance (configured by default for `Server=(localdb)\\MSSQLLocalDB;Database=RttClientManagerDb;Trusted_Connection=True;TrustServerCertificate=True;`).
3. **Configure Multiple Startup Projects & Run**:
   - Open `Rtt.ClientManager/Rtt.ClientManager.sln` in Visual Studio.
   - Right-click the Solution -> **Configure Startup Projects...** -> select **Multiple startup projects** and set both **`Rtt.ClientManager.Api`** and **`Rtt.ClientManager.Web`** to **Start**.
   - Press **`F5`** to launch both the REST API (`https://localhost:7029`) and the MVC Web UI (`https://localhost:7266`).
