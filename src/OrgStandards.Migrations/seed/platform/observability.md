---
title: Observability
version: 1.0
technology: [web-api, backend-service]
area: [observability, operations]
company: acme
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Logging
- MUST log in structured form (message templates, not string concatenation).
- MUST include the correlation id on every entry.

## Health endpoints
- Services MUST expose `/health` (readiness) and `/alive` (liveness).
