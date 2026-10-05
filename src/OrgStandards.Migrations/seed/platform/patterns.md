---
title: Architecture patterns
version: 1.0
kind: [backend]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. Each topic reaches
     only components that follow its pattern. -->

## Aggregates
<!-- tags: { pattern: [ddd] } -->
- Business rules MUST be enforced inside the aggregate that owns the data, not in handlers or
  controllers.
- A transaction MUST change one aggregate. Other aggregates are referenced by id, not by object.

Why: an aggregate is the boundary where its rules hold; changing two at once couples them for good.

## Ubiquitous language
<!-- tags: { pattern: [ddd] } -->
- Types and methods in the domain MUST use the domain's terms, as the business uses them
  (`PlaceOrder`, not `CreateOrderRecord`).

Why: when code and conversation use the same words, a change request maps straight to the code.

## Commands and queries
<!-- tags: { pattern: [cqrs] } -->
- A command MUST change state and return no data beyond an id or a result status. A query MUST NOT
  change state.
- Read models MAY lag behind commands; the caller MUST NOT assume a write is visible to a query
  straight away.

Why: separating the two lets reads scale and change shape without touching the write side.

## Events
<!-- tags: { pattern: [event-sourcing] } -->
- Events MUST be named in the past tense for what happened (`OrderPlaced`), and MUST NOT be edited
  or deleted once stored.
- A change to an event's shape MUST be a new version (`V2.OrderPlaced`), with the old version still
  readable.

Why: the events are the system of record; rewriting them rewrites history.

## Projections
<!-- tags: { pattern: [event-sourcing, cqrs] } -->
- Every read model MUST be rebuildable from the events alone.
- Projection handlers MUST be idempotent: handling the same event twice leaves the same result.

Why: events are delivered at least once, and a rebuild replays all of them.
