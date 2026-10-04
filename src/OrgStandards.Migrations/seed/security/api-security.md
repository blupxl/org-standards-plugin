---
title: API security
version: 1.0
kind: [api, backend]
concern: [security, authentication, authorization, input-validation]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. Rules are written in
     our own words and cite OWASP ASVS 5.0.0, the OWASP Top 10:2025 and MITRE CWE by identifier. -->

## Authentication
- Every endpoint MUST require an authenticated caller unless it's explicitly marked public
  (`[AllowAnonymous]`), with a comment saying why.
- Access tokens MUST be validated for issuer, audience, expiry and signature, through the
  platform's authentication setup, never by decoding the token by hand.
  *(ASVS V9 Self-contained Tokens, V10 OAuth and OIDC; Top 10 A07:2025 Authentication Failures)*

Why: a token that's only decoded, not validated, can be forged by anyone.

### Example (.NET)
```csharp
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Auth:Authority"];
    options.Audience = builder.Configuration["Auth:Audience"];
});

app.MapGet("/v1/orders/{id}", GetOrder).RequireAuthorization("orders.read");
```

## Authorization
- Every endpoint MUST check that the caller may act on the **specific** resource, server-side, not
  only that they're signed in. *(ASVS V8 Authorization; Top 10 A01:2025 Broken Access Control)*
- Authorization MUST use named policies, not role names scattered through code.

Why: "signed in" isn't "allowed"; reading another customer's order by changing an id is the most
common API breach.

## Input validation
- Request bodies MUST be validated against their declared model; unknown fields MUST be rejected.
  *(ASVS V2 Validation and Business Logic; CWE-20 Improper Input Validation)*
- Identifiers, sizes and counts from the caller MUST be range-checked before use.

Why: everything that arrives from a caller is untrusted, including from our own front end.

## Output encoding
- Data MUST be encoded for the context it goes into: HTML, URLs, SQL (parameters), shell, logs.
  MUST NOT build any of these by concatenating input. *(ASVS V1 Encoding and Sanitization;
  Top 10 A05:2025 Injection; CWE-79, CWE-89)*

Why: injection happens where data is mistaken for code; encoding keeps it data.
