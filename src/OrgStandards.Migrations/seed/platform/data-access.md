---
title: Data access
version: 1.2
kind: [data-access, backend]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Migrations
- Schema changes MUST be versioned migrations, applied by the service's migration step at
  deployment. MUST NOT create or change the schema implicitly when the service starts.
- MUST NOT edit a migration after it has been applied anywhere shared.

Why: migrations are the schema's history; a schema created on startup bypasses it and can't be
migrated later.

## Queries
- MUST load related data in the same query (a join or a projection), not one query per row.
- SQL MUST be parameterized. MUST NOT concatenate input into SQL.
- Read-only queries SHOULD skip change tracking, where the data-access library tracks by default.

Why: per-row queries are the most common cause of slow endpoints, and concatenated SQL is
injection.

### Example (.NET)
```csharp
var orders = await database.Orders
    .AsNoTracking()
    .Where(order => order.CustomerId == customerId)
    .Select(order => new OrderSummary(order.Id, order.Total, order.Lines.Count))
    .ToListAsync(cancellationToken);
```

## Connections
- Connection strings MUST come from configuration (`ConnectionStrings:<name>`), set per
  environment.
- MUST NOT keep a database session or context beyond one request or unit of work.

Why: a long-lived session grows without bound and serves stale data.
