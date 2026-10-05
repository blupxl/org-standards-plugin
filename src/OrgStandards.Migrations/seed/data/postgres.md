---
title: PostgreSQL
version: 1.0
uses: postgres
kind: [data-access, backend]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. They reach only
     components that use PostgreSQL. -->

## Postgres naming
- Tables, columns and indexes MUST be named in lower snake case (`order_lines`, `created_at`), so
  they never need quoting.

Why: PostgreSQL folds unquoted names to lower case; a name created as `"OrderLines"` must be quoted
in every query forever.

### Example (.NET, EF Core)
```csharp
options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();   // EFCore.NamingConventions
```

## Postgres types
- Points in time MUST be `timestamptz`, not `timestamp`.
- Text SHOULD be `text`; use `varchar(n)` only where the length limit is a business rule.
- Semi-structured data MUST be `jsonb`, not `json`.

Why: `timestamp` drops the time zone; `jsonb` can be indexed and queried, `json` can't.
