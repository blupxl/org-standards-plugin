---
title: Performance
version: 2.1
kind: [backend, api]
concern: [performance]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Async I/O
- Database, HTTP and file calls MUST be non-blocking end to end.
- The request's cancellation signal MUST be passed to every outbound call, so a cancelled request
  stops the work it started.

Why: a blocked thread can't serve other requests, and work for a cancelled request still costs the
services it calls.

## Response compression
- JSON responses larger than 1 KB SHOULD be compressed (Brotli, then gzip), using the response
  compression middleware.
- MUST NOT compress responses that mix secrets with attacker-controlled content over HTTPS.

Why: JSON compresses well, and transfer time dominates on mobile networks. The second rule avoids
compression side-channel attacks such as BREACH.
