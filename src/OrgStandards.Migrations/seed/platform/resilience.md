---
title: Resilience
version: 1.2
kind: [backend, api]
concern: [resilience]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Timeouts
- Outbound HTTP calls MUST set a timeout of 30 seconds or less.
- SHOULD use the standard resilience handler rather than hand-written retries.

Why: an unbounded call ties up a request thread until the caller gives up.

## Retries
- Outbound HTTP clients MUST use the platform's standard resilience library (backoff, jitter and a
  circuit breaker) instead of retry loops written by hand.
- MUST retry only idempotent requests (GET, PUT, DELETE), or requests that carry an idempotency key.
- MUST NOT retry `4xx` responses other than `408` and `429`.

Why: hand-written retries multiply load on a struggling dependency.

### Example (.NET)
```csharp
builder.Services.AddHttpClient<PricingClient>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Services:Pricing"]!))
    .AddStandardResilienceHandler();
```
