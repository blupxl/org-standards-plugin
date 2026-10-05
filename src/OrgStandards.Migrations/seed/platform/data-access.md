---
title: Data access
version: 1.3
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
- Before relying on a paging query for large lists, SHOULD check its query plan (`EXPLAIN`) to see
  that the index is used as a range condition, not a filter after a scan.

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

## Concurrent changes
- A change that depends on the current state MUST be one conditional update
  (`UPDATE orders SET status = 'Shipped' WHERE id = @id AND status = 'Placed'`) or use a
  concurrency token or a row lock. MUST NOT read the row, change it in memory and save it without
  one of them.
- With client-generated ids and connection retries on, an insert SHOULD treat a duplicate-key error
  on that id as success when the stored row matches the request (same owner, same content), and
  return the stored row: the first attempt already inserted it. A row that doesn't match is a
  `409`.

Why: two requests that both read "Placed" both ship the order. A retry whose first attempt
committed, but lost its reply, fails on its own row; the match check keeps another caller's id
from returning their order.

### Example (.NET)
```csharp
// In the PATCH handler. One statement: only a request that finds the order still Placed ships it.
var changed = await database.Orders
    .Where(order => order.Id == id && order.Status == OrderStatus.Placed)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(order => order.Status, OrderStatus.Shipped)
        .SetProperty(order => order.ShippedAt, timeProvider.GetUtcNow()), cancellationToken);
// changed == 0: the order doesn't exist (404) or isn't Placed any more (409).
```

### Example (.NET, PostgreSQL)
```csharp
// In the POST handler, with client-generated ids and retries on. pk_orders is the primary key's
// name with snake_case naming; check your own constraint's name (EF Core's default is PK_Orders).
try
{
    database.Orders.Add(order);
    await database.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateException exception) when (exception.InnerException is PostgresException
    { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "pk_orders" })
{
    database.ChangeTracker.Clear();   // forget the order that failed to insert
    var stored = await database.Orders.AsNoTracking()
        .FirstAsync(candidate => candidate.Id == order.Id, cancellationToken);
    if (stored.OwnerSubject != order.OwnerSubject || stored.OwnerIssuer != order.OwnerIssuer
        || stored.Sku != order.Sku || stored.Quantity != order.Quantity)
    {
        return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict,
            title: "Another order already has this id");
    }

    order = stored;   // the first attempt's order
}
```
