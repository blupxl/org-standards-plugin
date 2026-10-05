---
title: .NET services
version: 1.6
runtime: dotnet
kind: [backend, api, configuration, data-access, testing]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. How the general
     platform standards are met in .NET; they reach only projects that name the dotnet runtime. -->

## Naming in .NET
- Names MUST follow Microsoft's C# identifier naming rules and conventions: PascalCase for
  namespaces, types, methods, properties, events and constants; camelCase for parameters and
  locals; `_camelCase` for private fields; interfaces start with `I`; async methods end in
  `Async` (except endpoint handlers, which match their endpoint's name).
- Names MUST say what the thing is, in whole words. MUST NOT abbreviate or contract
  (`customerAddress`, not `custAddr`; `httpContext`, not `ctx`; `database`, not `db`), and MUST NOT
  use single letters outside a loop counter. Long, explicit names are fine. Acronyms take
  Microsoft's casing (`Id`, `Http`, `Json`, `IO`).
- MUST NOT name a type after how it moves data: no `Dto` or `DataTransferObject` suffix, no
  `ToDto()`. An API's input is named for the operation, with `Request` (`PlaceOrderRequest`,
  `SendEmailRequest`); its output is named for what it returns, with `Response` (`OrderResponse`,
  `EmailResponse`), and maps with `ToResponse()`.

Why: code is read far more often than it's written. A name that says what it holds needs no
comment, and the same vocabulary in every service means nobody has to learn a team's shorthand.

### Examples
| Instead of | Write |
|---|---|
| `OrderDto`, `order.ToDto()` | `OrderResponse`, `order.ToResponse()` |
| `PlaceOrder` (an HTTP body), `OrderInput` | `PlaceOrderRequest` |
| `OrdersDbContext db`, `LinkGenerator lg`, `HttpContext ctx` | `OrdersDbContext database`, `LinkGenerator linkGenerator`, `HttpContext httpContext` |
| `orders.Where(o => o.Total > 0)` | `orders.Where(order => order.Total > 0)` |
| `IOrderRepo`, `OrderSvc`, `CfgHelper` | `IOrderRepository`, `OrderService`, a name for what it does |

Source: Microsoft Learn, "C# identifier naming rules and conventions" and the .NET Framework Design
Guidelines' general naming conventions.

## Problem details in .NET
<!-- tags: { kind: [api], implements: [Errors] } -->
- Services MUST register `builder.Services.AddProblemDetails()` and the exception handler
  (`app.UseExceptionHandler()`), so every error, including unhandled ones, is a Problem Details
  response.

Why: one registration covers every endpoint; a handler written per endpoint misses some.

## OpenAPI in .NET
<!-- tags: { kind: [api], concern: [documentation], implements: [API description] } -->
- APIs MUST generate their OpenAPI description with the built-in `Microsoft.AspNetCore.OpenApi`
  (`builder.Services.AddOpenApi()`, `app.MapOpenApi()`). MUST NOT add Swashbuckle.
- APIs MUST serve the Scalar API explorer (`Scalar.AspNetCore`, `app.MapScalarApiReference()`) at
  its default address, `/scalar/v1`, and the description at its default, `/openapi/v1.json`. MUST
  NOT change either route.
- Every endpoint MUST call `.WithName()`, `.WithSummary()` and `.WithTags()`, and return
  `TypedResults` (or declare `.Produces<T>()` and `.ProducesProblem()`) so every response appears
  in the description.
- The AppHost SHOULD link each API's explorer in the dashboard: `.WithUrl("/scalar/v1", "API
  reference")`.
- Both MUST be switched on by a setting (for example `OpenApi:Enabled`), not by checking the
  environment name (see the Environments standard).

Why: one explorer, at one address, across every API, built on the description ASP.NET Core
generates itself; Swashbuckle is no longer part of the templates.

### Example
```csharp
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();                 // /openapi/v1.json
    app.MapScalarApiReference();      // /scalar/v1
}

app.MapGroup("/v1/orders").WithTags("Orders")
    .MapGet("/{id:guid}", GetOrder)
    .WithName("GetOrder")
    .WithSummary("Gets one order");
```

## Model documentation in .NET
<!-- tags: { kind: [api], concern: [documentation], implements: [Model documentation] } -->
- The API project and every project that holds its models MUST set
  `<GenerateDocumentationFile>true</GenerateDocumentationFile>`, and every model and property MUST
  have an XML `<summary>`; ASP.NET Core puts them in the OpenAPI description.
- Every request and response model MUST have an `<example>` with realistic JSON. Endpoint
  parameters MAY carry `example="…"` on their `<param>`.
- Constraints MUST use the data annotations the OpenAPI generator reads: `[Required]`, `[Range]`,
  `[MinLength]`, `[MaxLength]`, `[RegularExpression]`, `[DefaultValue]`. `[StringLength]`,
  `[EmailAddress]` and `[Url]` validate but don't appear in the description, so a length or format
  MUST also be declared with one of the above.
- Models MUST document properties with their own XML comments, so prefer records with properties
  (`public required Guid Id { get; init; }`) over positional records, whose parameters can't carry
  a `<summary>` of their own.
- Nullable reference types MUST be on, and enumerations MUST use `JsonStringEnumConverter`.

Why: one set of annotations drives both validation (`AddValidation()`) and the description, so what
the explorer shows is what the API enforces.

### Example
```csharp
/// <summary>An order placed by a customer.</summary>
/// <example>
/// {"self":"https://api.acme.example/v1/orders/0f8fad5b-d9cb-469f-a165-70867728950e",
///  "id":"0f8fad5b-d9cb-469f-a165-70867728950e","customerId":"7c9e6679-7425-40de-944b-e07fc1f90ae7",
///  "status":"Placed","total":18.50}
/// </example>
public sealed record OrderResponse
{
    /// <summary>This order's own URL.</summary>
    public required string Self { get; init; }

    /// <summary>The order's id.</summary>
    public required Guid Id { get; init; }

    /// <summary>The customer who placed it.</summary>
    public required Guid CustomerId { get; init; }

    /// <summary>Where the order is in its lifecycle.</summary>
    public required OrderStatus Status { get; init; }

    /// <summary>The total, in the customer's currency.</summary>
    [Range(0, 1_000_000)]
    public required decimal Total { get; init; }
}

/// <summary>A request to place an order.</summary>
/// <example>{"customerId":"7c9e6679-7425-40de-944b-e07fc1f90ae7","sku":"MUG-RED","quantity":2}</example>
public sealed record PlaceOrderRequest
{
    /// <summary>The customer placing the order.</summary>
    [Required]
    public required Guid CustomerId { get; init; }

    /// <summary>The product's stock-keeping unit.</summary>
    [Required, MaxLength(32), RegularExpression("^[A-Z0-9-]+$")]
    public required string Sku { get; init; }

    /// <summary>How many to order.</summary>
    [Range(1, 100)]
    public required int Quantity { get; init; }
}
```

## Resource locations in .NET
<!-- tags: { kind: [api], concern: [documentation], implements: [Resource locations] } -->
- Links MUST be built with `LinkGenerator.GetUriByName(httpContext, "<endpoint name>", values)`,
  from the endpoint's name, never by concatenating strings.
- Problem Details MUST set `instance` in one place: `AddProblemDetails` with `CustomizeProblemDetails`.
- Behind a proxy, MUST use forwarded headers from trusted proxies only, so absolute URLs carry the
  public host and scheme.

Why: endpoint names are already required for the description; the same names make every link
correct after a route changes.

### Example
```csharp
// OrderResponse is documented as in Model documentation in .NET; ToResponse maps an order and its
// own URL.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Instance ??= context.HttpContext.Request.GetEncodedUrl());

static async Task<Results<Ok<OrderResponse>, NotFound>> GetOrder(
    Guid id, OrdersDbContext database, LinkGenerator linkGenerator, HttpContext httpContext,
    CancellationToken cancellationToken)
{
    var order = await database.Orders.AsNoTracking()
        .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
    return order is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(order.ToResponse(linkGenerator.GetUriByName(httpContext, "GetOrder", new { id })!));
}

static async Task<Created<OrderResponse>> PlaceOrder(PlaceOrderRequest request, /* … */)
{
    // … place the order …
    var orderUrl = linkGenerator.GetUriByName(httpContext, "GetOrder", new { id = order.Id })!;
    return TypedResults.Created(orderUrl, order.ToResponse(orderUrl));
}
```

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
<!-- tags: { kind: [data-access], uses: [ef-core], implements: [Migrations, Queries] } -->
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
