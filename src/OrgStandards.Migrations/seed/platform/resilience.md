---
title: Resilience
version: 1.0
technology: [web-api, backend-service]
area: [resilience]
company: acme
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Timeouts
- Outbound HTTP calls MUST set a timeout of 30 seconds or less.
- SHOULD use the standard resilience handler rather than hand-written retries.

Why: an unbounded call ties up a request thread until the caller gives up.
