---
title: SQL Server
version: 1.0
uses: sqlserver
kind: [data-access, backend]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. They reach only
     components that use SQL Server or Azure SQL. -->

## SQL Server schemas
- Tables MUST live in a schema named for their area (`sales.Orders`, `billing.Invoices`), not in
  `dbo`, and queries MUST name the schema.

Why: schemas group tables by owner and let permissions be granted per area.

## SQL Server types
- Points in time MUST be `datetimeoffset` (or `datetime2` for values with no time zone), not
  `datetime`.
- User-entered text MUST be `nvarchar`, not `varchar`.

Why: `datetime` is imprecise and has no time zone; `varchar` loses characters outside the code page.

## SQL Server connections
- Connections MUST be encrypted (`Encrypt=True`) and MUST NOT set `TrustServerCertificate=True`
  outside local development.

Why: an untrusted certificate makes the encryption easy to intercept.
