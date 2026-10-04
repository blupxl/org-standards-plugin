---
title: .NET services
version: 1.1
runtime: dotnet
kind: [backend, api, configuration, data-access, testing]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. How the general
     platform standards are met in .NET; they reach only projects that name the dotnet runtime. -->

## Problem details in .NET
<!-- tags: { kind: [api], implements: [Errors] } -->
- Services MUST register `builder.Services.AddProblemDetails()` and the exception handler
  (`app.UseExceptionHandler()`), so every error, including unhandled ones, is a Problem Details
  response.

Why: one registration covers every endpoint; a handler written per endpoint misses some.

## Settings in .NET
<!-- tags: { kind: [configuration, backend], implements: [Settings] } -->
- Settings MUST be bound to options classes (`builder.Services.AddOptions<T>().Bind(...)`) and
  validated with `.ValidateDataAnnotations().ValidateOnStart()`.
- Code MUST NOT read configuration keys as strings outside `Program.cs`.

Why: `ValidateOnStart` is what makes a bad setting fail the deployment instead of a request.

### Example
```csharp
builder.Services.AddOptions<PricingOptions>()
    .Bind(builder.Configuration.GetSection("Pricing"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

## EF Core
<!-- tags: { kind: [data-access], implements: [Migrations, Queries] } -->
- Schema changes MUST be EF Core migrations. MUST NOT use `Database.EnsureCreated()` outside tests.
- Read-only queries MUST use `AsNoTracking()`.
- Raw SQL MUST go through `FromSql` with interpolated parameters.

Why: these are how the general data-access standard is met with EF Core.

## Tests in .NET
<!-- tags: { kind: [testing], implements: [Unit tests, Integration tests] } -->
- Unit tests MUST use xUnit, one test class per class under test.
- Services built with Aspire MUST have integration tests that start the AppHost with
  `Aspire.Hosting.Testing`, marked `[Trait("Category", "Integration")]`.

Why: one test stack across services; anyone can run and read anyone's tests.

## Resilience handler in .NET
<!-- tags: { kind: [backend, api], concern: [resilience], implements: [Retries] } -->
- Outbound HTTP clients MUST add `AddStandardResilienceHandler()`
  (Microsoft.Extensions.Http.Resilience).

Why: it is the platform's standard resilience library for .NET: backoff, jitter, timeouts and a
circuit breaker in one call.

## Blocking calls in .NET
<!-- tags: { kind: [backend, api], concern: [performance], implements: [Async I/O] } -->
- MUST NOT block on async code with `.Result`, `.Wait()` or `GetAwaiter().GetResult()`.
- MUST pass the request's `CancellationToken` to every async call.

Why: blocking on async code under load starves the thread pool and stalls every request.

## Service defaults in .NET
<!-- tags: { kind: [backend, api], concern: [observability], implements: [Tracing, Health endpoints] } -->
- Services MUST call `builder.AddServiceDefaults()` from the shared ServiceDefaults project, which
  sets up OpenTelemetry, health endpoints and resilience, instead of their own setup.
- Custom operations SHOULD use an `ActivitySource` named `<Company>.<Service>`.

Why: the same telemetry and health endpoints everywhere, configured in one place.
