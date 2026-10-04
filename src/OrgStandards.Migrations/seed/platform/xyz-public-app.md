---
title: XYZ Public App services
version: 1.1
product: xyz-public-app
kind: [backend, api]
concern: [resilience, caching, performance]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Timeouts
- Outbound HTTP calls MUST set a timeout of 10 seconds or less.

Why: the public app's page budget is 2 seconds end to end; a 30-second dependency call breaks it.

## Caching
- Product catalog responses MUST be cached for 5 minutes in the shared distributed cache.
- MUST connect through the configuration key `Cache:ConnectionString`. MUST NOT hard-code cache hosts.

Why: the catalog changes a few times a day and is the app's heaviest read.
