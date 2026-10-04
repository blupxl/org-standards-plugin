---
title: Caching
version: 1.0
kind: [backend]
concern: [caching, performance]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Caching
- Read-heavy responses SHOULD be cached in the shared distributed cache.
- MUST connect through the configuration key `Cache:ConnectionString`. MUST NOT hard-code cache hosts.

Why: hosts differ between dev, test and prod, and configuration is how each environment says which to use.

### Example (.NET)
```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration["Cache:ConnectionString"]);
```

## Cache keys
- Keys MUST follow `<service>:<entity>:<id>:v<schema>`, for example `orders:customer:42:v1`.
- MUST bump the `v<schema>` part when the cached shape changes, rather than flushing the cache.
- Every entry MUST have an expiry. SHOULD NOT exceed 24 hours without the platform team's agreement.

Why: services share one cache. A prefix per service prevents collisions, and the schema version
lets old and new code run side by side during a deployment.
