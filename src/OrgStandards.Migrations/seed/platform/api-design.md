---
title: API design
version: 1.5
kind: [api, backend]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Routes and versioning
- Routes MUST be nouns in plural kebab case (`/customer-orders/{id}`), versioned in the path
  (`/v1/...`).
- A change of state SHOULD be a `PATCH` on the resource (`PATCH /v1/orders/{id}` with
  `{"status": "Shipped"}`). An operation with side effects beyond the resource's own fields
  (cancelling with a refund, resending a receipt) MAY be a `POST` to an action
  (`POST /v1/orders/{id}/cancel`).
- MUST NOT invent a resource only to avoid a verb (`POST /orders/{id}/shipments` with nothing to
  fetch there).
- Breaking changes MUST go to a new version; the previous version MUST keep working for at least
  six months after the new one ships.

Why: callers upgrade on their own schedule; a path version makes the contract explicit. A state is
a field of the resource, so changing it is changing the resource; a made-up resource is a contract
nobody can read back.

### Example
```http
PATCH /v1/orders/0f8fad5b-d9cb-469f-a165-70867728950e
Content-Type: application/merge-patch+json

{ "status": "Shipped" }
```
Returns `200` with the updated order, or `409` when its current state doesn't allow the change
(shipping a cancelled order).

## Errors
- Error responses MUST use Problem Details (RFC 9457).
- MUST NOT return stack traces or exception messages to callers.
- MUST use the matching status code: `400` invalid input, `401` no or bad credentials, `403` not
  allowed, `404` not found, `409` conflict.
- Every `400` MUST have the same Problem Details shape, with an `errors` map from field to
  messages, whether validation or a check in the handler found the bad input.

Why: one error shape lets every client handle errors the same way.

### Example
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Order not found",
  "status": 404,
  "detail": "No order 1042 for this customer.",
  "instance": "https://api.acme.example/v1/orders/1042",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

### Example: 400
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "instance": "https://api.acme.example/v1/orders",
  "errors": { "sku": ["No product MUG-PURPLE in the catalog."] },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

## Idempotent creates
<!-- tags: { concern: [resilience] } -->
- Creates SHOULD accept an `Idempotency-Key` header (an IETF draft): a retried `POST` with the same
  key from the same caller returns the first result instead of creating a duplicate. The same key
  with a different body is refused with `422`. A client-generated id does the same job (see
  Concurrent changes).

Why: networks drop responses, not only requests. A client that retries a `POST` without a key
can place the same order twice. Keys are scoped to the caller so one caller's key can't return
another's result.

## API description
<!-- tags: { concern: [documentation] } -->
- Every HTTP API MUST publish an OpenAPI 3 description of its endpoints, generated from the code
  (not written by hand), at `/openapi/v1.json`, and serve an interactive explorer for it at
  `/scalar/v1`. Every API uses these two addresses, so a developer always knows where to look.
- Every operation MUST have a unique name (its `operationId`), a summary, and a tag for its
  resource, and MUST declare every response it can return: the success type and each error status
  as Problem Details.

Why: other teams build against the description, and generate clients from it; an operation
without a name or its error responses can't be called correctly without reading the code.

## Model documentation
<!-- tags: { concern: [documentation] } -->
- Every request and response model, and every property, MUST have a description in the API
  description.
- Constraints MUST appear in the schema, not only in code: required fields, ranges, lengths and
  patterns.
- Every request and response model MUST have a realistic example, so the explorer shows real-looking
  data (`"total": 18.50`), not placeholders (`"total": 0`).
- Enumerations MUST be serialized and described as strings.

Why: the description is the contract other teams read and generate clients from; a type name alone
doesn't say what's valid or what a real value looks like.

## Resource locations
<!-- tags: { concern: [documentation] } -->
- Every resource in a response MUST carry its own absolute URL in a `self` field, and a page of a
  collection MUST carry `self`, `next` (when there's another page) and its `items`.
- A create MUST return `201 Created` with the new resource's absolute URL in `Location` and as the
  body's `self`.
- Problem Details MUST set `instance` to the absolute URL of the request.

Why: a caller can follow, cache or report any response without rebuilding URLs from ids. These are
published conventions: `Location` (RFC 9110), `instance` (RFC 9457), and simple `self` and `next`
links (Zalando's API guidelines), so consumers know them already.

### Example
```jsonc
// GET https://api.acme.example/v1/orders?after=abc
{
  "self": "https://api.acme.example/v1/orders?after=abc",
  "next": "https://api.acme.example/v1/orders?after=def",
  "items": [
    { "self": "https://api.acme.example/v1/orders/42", "id": "42", "total": 18.50 }
  ]
}

// 404
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5", "title": "Order not found",
  "status": 404, "instance": "https://api.acme.example/v1/orders/42", "traceId": "…" }
```

## Pagination
- Collection endpoints MUST be paginated, with a default page size of 50 and a maximum of 200.
- SHOULD use cursor pagination (`?after=<cursor>`) for collections that change while being read.

Why: an unpaginated collection works in testing and fails in production when the data grows.
