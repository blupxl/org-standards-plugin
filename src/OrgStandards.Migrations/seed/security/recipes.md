---
title: API security recipes
version: 1.3
template: recipe
runtime: dotnet
kind: [api, backend]
concern: [security]
---
<!-- PLACEHOLDER recipes that exercise the format. Replace with real ones. Approved ways to secure a
     .NET API; each meets the security standards it names. -->

## Recipe: JWT bearer authentication
<!-- tags: { concern: [security, authentication, authorization], recommended: [yes] } -->
How an API checks who's calling and what they may do. Following it meets Authentication and
Authorization.

- MUST validate tokens with the framework's JWT bearer handler, configured from
  `Authentication:Schemes:Bearer` (authority and audiences), never by decoding tokens by hand.
- MUST set a fallback policy that requires an authenticated user, so a new endpoint is protected
  by default; public endpoints opt out with `AllowAnonymous()` and a comment saying why.
- MUST check permissions with named policies (one per scope, `orders.read`), and check that the
  caller may act on the specific resource inside the handler.
- MUST key a caller's data by issuer plus subject (`iss` + `sub`), or by the provider's documented
  stable id (Microsoft Entra: `tid` + `oid` when data is shared across services), never `sub` alone.

Why: with a fallback policy, forgetting an attribute fails closed instead of open. A subject is
unique only within its issuer, so `sub` alone can match another issuer's user.

### Wiring
```csharp
// Program.cs
builder.Services.AddAuthentication().AddJwtBearer(options => options.MapInboundClaims = false);
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy("orders.read", policy => policy.RequireAssertion(context =>
        context.User.FindFirst("scp")?.Value.Split(' ').Contains("orders.read") == true));

v1.MapGet("/orders/{id:guid}", GetOrder).RequireAuthorization("orders.read");

// In a handler, which takes the caller as a ClaimsPrincipal parameter (user). The owner of the
// caller's data, stored as two columns, owner_issuer and owner_subject.
var owner = (Issuer: user.FindFirstValue("iss")!, Subject: user.FindFirstValue("sub")!);
```

### Configuration
```json
{
  "Authentication": {
    "Schemes": {
      "Bearer": {
        "Authority": "https://login.microsoftonline.com/<tenant>/v2.0",
        "ValidAudiences": ["api://orders"]
      }
    }
  }
}
```

## Recipe: Strict request validation
<!-- tags: { concern: [security, input-validation], recommended: [yes] } -->
How an API refuses bad input before it reaches the handler. Following it meets Input validation.

- MUST reject JSON fields the model doesn't declare (`JsonUnmappedMemberHandling.Disallow`).
- MUST validate request models with data annotations through the framework's validation
  (`AddValidation()` for minimal APIs; `[ApiController]` for controllers), returning Problem
  Details.
- Ranges, lengths and patterns MUST be declared on the model with `[Range]`, `[MinLength]`,
  `[MaxLength]` and `[RegularExpression]`, not checked by hand in handlers. These are the ones the
  OpenAPI description shows too (see Model documentation in .NET).

Why: the framework turns the model into the contract, and anything outside it is refused.

### Wiring
```csharp
builder.Services.AddValidation();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);

public sealed record PlaceOrderRequest
{
    [Required]
    public required Guid CustomerId { get; init; }

    [Range(1, 100)]
    public required int Quantity { get; init; }
}
```

## Recipe: Rate limiting per client
<!-- tags: { concern: [security, performance], recommended: [yes] } -->
How a public API protects itself from one caller using it all. Fills a gap the standards don't
cover yet.

- MUST use the framework's rate limiter, partitioned by the authenticated caller (or the address
  for anonymous endpoints), with limits from configuration.
- Rejected requests MUST return `429 Too Many Requests`.

Why: without a limit, one misbehaving client degrades the API for everyone.

### Wiring
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("per-client", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) }));
});

app.UseRateLimiter();
var v1 = app.MapGroup("/v1").RequireRateLimiting("per-client");
```

## Recipe: CORS for known front ends
<!-- tags: { concern: [security], recommended: [yes] } -->
How a public API lets the company's own web front ends call it from the browser.

- Allowed origins MUST come from configuration (`Cors:AllowedOrigins`), listed exactly.
- MUST NOT combine any-origin with credentials; MUST allow only the methods the API uses.

Why: a wildcard origin with credentials lets any site call the API as the signed-in user.

### Wiring
```csharp
builder.Services.AddCors(options => options.AddPolicy("front-ends", policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .WithMethods("GET", "POST")
    .AllowAnyHeader()));

app.UseCors("front-ends");
```
