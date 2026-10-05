---
title: MongoDB
version: 1.0
uses: mongodb
kind: [data-access, backend]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. They reach only
     components that use MongoDB. -->

## MongoDB indexes
- Every query the service runs MUST be served by an index; collection scans MUST NOT reach
  production. Indexes MUST be created by the service's deployment step, not by hand.

Why: a collection scan is fast in testing and stalls the database once the data grows.

## MongoDB documents
- Every collection MUST have a schema validator (`$jsonSchema`).
- Documents MUST NOT hold arrays that grow without limit (for example every order of a customer);
  model those as their own collection.

Why: a schema keeps bad writes out of a schemaless store; an unbounded array eventually hits the
16 MB document limit.
