---
title: Observability
version: 1.3
kind: [backend]
concern: [observability]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Logging
- MUST log in structured form (message templates, not string concatenation).
- MUST include the correlation id on every entry.

Why: structured entries can be searched and counted; the correlation id ties one request's entries
together across services.

## Health endpoints
- Services MUST expose `/health` (readiness) and `/alive` (liveness).

Why: the platform restarts a service that isn't alive and stops sending traffic to one that isn't
ready; without both, a stuck service keeps receiving requests.

## Tracing
- Services MUST export traces with OpenTelemetry, set up once per service in the platform's
  shared configuration, not their own exporter setup.
- Custom operations SHOULD be wrapped in a span from a tracer named `<Company>.<Service>`, so they
  appear inside the request's trace.

Why: one request crosses several services; a trace is the only place its whole path is visible.
