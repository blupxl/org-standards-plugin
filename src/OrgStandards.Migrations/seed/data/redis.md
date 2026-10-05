---
title: Redis
version: 1.0
uses: redis
kind: [backend]
concern: [caching, performance]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. They reach only
     components that use Redis. -->

## Redis usage
- Redis MUST NOT be the system of record: everything in it MUST be rebuildable from the database.
- Application code MUST NOT use `KEYS`; use `SCAN` to iterate.
- Every key MUST have an expiry (see Cache keys).

Why: Redis may evict data under memory pressure, and `KEYS` blocks the whole server while it runs.
