---
title: Performance
version: 1.0
technology: [web-api, backend-service]
area: [performance]
company: acme
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Caching
- Read-heavy responses SHOULD be cached in the shared distributed cache.
- MUST connect through the configuration key `Cache:ConnectionString`. MUST NOT hard-code cache hosts.

Why: hosts differ between dev, test and prod, and configuration is how each environment says which to use.

### Example
```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration["Cache:ConnectionString"]);
```
