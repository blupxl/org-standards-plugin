---
title: Service recipes
version: 1.4
template: recipe
runtime: dotnet
kind: [api, backend]
---
<!-- PLACEHOLDER recipes that exercise the format. Replace with real ones. Approved ways to start a
     .NET service and add common capabilities; each meets the standards it names. -->

## Recipe: Minimal API service
<!-- tags: { kind: [api, backend], recommended: [yes] } -->
The platform team's default for a new HTTP API. Following it meets Routes and versioning, Errors,
API description, Model documentation, Resource locations, Naming in .NET, Problem details in .NET, OpenAPI
in .NET, Resource locations in .NET, Service defaults in .NET, Health endpoints, Tracing and Settings.

- MUST be an Aspire project: an AppHost, the shared ServiceDefaults project, and the API calling
  `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`.
- MUST map every endpoint inside a versioned route group (`app.MapGroup("/v1")`), named in plural
  kebab case, and give each a name, a summary and a tag.
- MUST name requests `<Operation>Request` and responses `<Thing>Response`, never `Dto`, with whole
  words throughout, as in Naming in .NET.
- MUST document every model as in Model documentation in .NET: XML summaries, a realistic
  `<example>`, and annotations the description shows.
- MUST return resources with a `self` link built by `LinkGenerator`, and set Problem Details
  `instance`, as in Resource locations in .NET.
- MUST register Problem Details and the exception handler, and the OpenAPI document and Scalar
  at their default addresses (`/openapi/v1.json`, `/scalar/v1`), switched on by the
  `OpenApi:Enabled` setting, and link the explorer in the AppHost's dashboard.
- The ServiceDefaults template maps its health endpoints only in Development and reads
  `OTEL_EXPORTER_OTLP_ENDPOINT` as a string. Both MUST be changed: map the endpoints whenever
  `Health:Enabled` is set, and let the OpenTelemetry exporter read its own setting.

Why: one shape for every API, already meeting the standards that every API is checked against.

### Packages
| Project | Package |
|---|---|
| The API | `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`; and `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in it and its model projects |
| The AppHost | the Aspire AppHost SDK |

### Wiring
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Instance ??= context.HttpContext.Request.GetEncodedUrl());
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();
app.MapDefaultEndpoints();

if (app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var orders = app.MapGroup("/v1/orders").WithTags("Orders");
orders.MapGet("/{id:guid}", GetOrder).WithName("GetOrder").WithSummary("Gets one order");
orders.MapPost("/", PlaceOrder).WithName("PlaceOrder").WithSummary("Places an order");

// AppHost: the explorer's link in the dashboard
builder.AddProject<Projects.Orders_Api>("orders-api").WithUrl("/scalar/v1", "API reference");

app.Run();
```

## Recipe: Controller-based API
<!-- tags: { kind: [api, backend] } -->
An approved alternative for teams moving existing controller code. It meets the same standards as
the Minimal API service.

- Everything in the Minimal API service recipe applies, with controllers in place of endpoints.
- Routes MUST carry the version (`[Route("v1/orders")]`), and controllers MUST be `[ApiController]`
  so invalid models return Problem Details automatically.

Why: controllers are fine; what matters is that the contract and the plumbing are the same.

### Wiring
```csharp
builder.Services.AddControllers();
// ... the rest as in the Minimal API service recipe ...
app.MapControllers();
```

## Recipe: Typed HTTP client
<!-- tags: { kind: [backend], concern: [resilience], recommended: [yes] } -->
How a service calls another over HTTP. Following it meets Timeouts, Retries and Resilience handler
in .NET.

- MUST be a typed client registered with `AddHttpClient<T>`, its base address from configuration.
- MUST add the standard resilience handler; for a request that isn't idempotent (a POST without
  an idempotency key), MUST turn its retries off.

Why: one place for the address and the resilience policy; no hand-written retries.

### Wiring
```csharp
builder.Services.AddHttpClient<PricingClient>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Services:Pricing"]!))
    .AddStandardResilienceHandler();
```

## Recipe: Distributed cache with Redis
<!-- tags: { kind: [backend], concern: [caching, performance], adds: [redis], recommended: [yes] } -->
How a service caches data shared across instances. Following it meets Caching, Cache keys and
Redis usage.

- MUST register `IDistributedCache` backed by Redis, connected through `Cache:ConnectionString`.
- MUST build keys as `<service>:<entity>:<id>:v<schema>` and set an expiry on every entry.

Why: the shared cache, wired the same way everywhere, so keys never collide.

### Packages
| Project | Package |
|---|---|
| The API | `Microsoft.Extensions.Caching.StackExchangeRedis` |

### Wiring
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration["Cache:ConnectionString"]);

// Use
await cache.SetStringAsync($"orders:customer:{customerId}:v1", customerJson,
    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
```
