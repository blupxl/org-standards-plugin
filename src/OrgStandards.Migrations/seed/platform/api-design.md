---
title: API design
version: 1.1
kind: [api, backend]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Routes and versioning
- Routes MUST be nouns in plural kebab case (`/customer-orders/{id}`), versioned in the path
  (`/v1/...`).
- Breaking changes MUST go to a new version; the previous version MUST keep working for at least
  six months after the new one ships.

Why: callers upgrade on their own schedule; a path version makes the contract explicit.

## Errors
- Error responses MUST use Problem Details (RFC 9457).
- MUST NOT return stack traces or exception messages to callers.
- MUST use the matching status code: `400` invalid input, `401` no or bad credentials, `403` not
  allowed, `404` not found, `409` conflict.

Why: one error shape lets every client handle errors the same way.

### Example
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Order not found",
  "status": 404,
  "detail": "No order 1042 for this customer.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

## Pagination
- Collection endpoints MUST be paginated, with a default page size of 50 and a maximum of 200.
- SHOULD use cursor pagination (`?after=<cursor>`) for collections that change while being read.

Why: an unpaginated collection works in testing and fails in production when the data grows.
