---
title: Data access recipes
version: 1.0
template: recipe
runtime: dotnet
kind: [data-access, backend]
---
<!-- PLACEHOLDER recipes that exercise the format. Replace with real ones. A recipe is an approved
     way to add a capability to a .NET service; it comes back only when a request asks for
     template: recipe. uses says which projects it's for; adds says what following it adds to the
     component's uses. The owners mark one option per database as recommended. -->

## Recipe: EF Core with PostgreSQL
<!-- tags: { uses: [postgres], adds: [ef-core], recommended: [yes] } -->
The data team's default for a .NET service on PostgreSQL. Following it meets Migrations, Queries,
Connections, EF Core and Postgres naming.

- MUST register the context through Aspire's integration (`AddNpgsqlDbContext`), with snake-case
  naming, and take the connection from `ConnectionStrings:<database>`.
- MUST keep migrations in the data project and apply them from a separate migration step at
  deployment, never when the API starts.

Why: one way to reach Postgres across services, with naming and migrations already settled.

### Packages
| Project | Package |
|---|---|
| The API | `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions` |
| The data project | `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design` |
| The AppHost | `Aspire.Hosting.PostgreSQL` |

### Wiring
```csharp
// AppHost
var orders = builder.AddPostgres("postgres").AddDatabase("ordersdb");
builder.AddProject<Projects.Orders_Api>("api").WithReference(orders).WaitFor(orders);

// API: Program.cs
builder.AddNpgsqlDbContext<OrdersDbContext>("ordersdb",
    configureDbContextOptions: options => options.UseSnakeCaseNamingConvention());
```

### Migrations
```bash
dotnet ef migrations add AddOrders --project src/Orders.Data --startup-project src/Orders.Api
```
Apply them from a worker the AppHost runs before the API (`WaitForCompletion`), calling
`Database.MigrateAsync()`.

## Recipe: Dapper with PostgreSQL
<!-- tags: { uses: [postgres], adds: [dapper] } -->
An approved alternative for services that need hand-written SQL. Following it meets Queries,
Connections and Postgres naming; schema changes still need Migrations.

- MUST open connections from one registered `NpgsqlDataSource` (`AddNpgsqlDataSource`), never by
  building connection strings in code.
- MUST pass parameters, never concatenated values, and the request's cancellation token, through
  `CommandDefinition`.
- Schema changes MUST still be versioned migrations, applied by a migration step.

Why: Dapper leaves SQL to you; these rules keep it safe and pooled.

### Packages
| Project | Package |
|---|---|
| The API | `Aspire.Npgsql`, `Dapper` |
| The AppHost | `Aspire.Hosting.PostgreSQL` |

### Wiring
```csharp
// API: Program.cs
builder.AddNpgsqlDataSource("ordersdb");

// A query
await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
var orders = await connection.QueryAsync<OrderSummary>(new CommandDefinition(
    "select id, total from orders where customer_id = @customerId",
    new { customerId }, cancellationToken: cancellationToken));
```

## Recipe: EF Core with SQL Server
<!-- tags: { uses: [sqlserver], adds: [ef-core], recommended: [yes] } -->
The data team's default for a .NET service on SQL Server or Azure SQL. Following it meets
Migrations, Queries, Connections, EF Core and the SQL Server standards.

- MUST register the context through Aspire's integration (`AddSqlServerDbContext`) and take the
  connection from `ConnectionStrings:<database>`.
- MUST give each area its own schema (`modelBuilder.HasDefaultSchema("sales")`).
- MUST apply migrations from a separate migration step at deployment, never when the API starts.

Why: one way to reach SQL Server across services, with schemas and migrations already settled.

### Packages
| Project | Package |
|---|---|
| The API | `Aspire.Microsoft.EntityFrameworkCore.SqlServer` |
| The data project | `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design` |
| The AppHost | `Aspire.Hosting.SqlServer` |

### Wiring
```csharp
// AppHost
var orders = builder.AddSqlServer("sql").AddDatabase("ordersdb");
builder.AddProject<Projects.Orders_Api>("api").WithReference(orders).WaitFor(orders);

// API: Program.cs
builder.AddSqlServerDbContext<OrdersDbContext>("ordersdb");
```
