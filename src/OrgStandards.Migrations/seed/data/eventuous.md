---
title: Eventuous
version: 1.0
uses: eventuous
runtime: dotnet
kind: [backend]
pattern: [event-sourcing]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. They reach only .NET
     components that use Eventuous for event sourcing. -->

## Eventuous events
<!-- tags: { implements: [Events] } -->
- Every event type MUST have a stable, versioned name with `[EventType("V1.OrderPlaced")]`, so
  renaming the class never changes what's stored.

Why: stored events are read back by that name; a class name is free to change, the stored name
isn't.

## Eventuous subscriptions
<!-- tags: { implements: [Projections] } -->
- Subscriptions MUST store their checkpoint in the read model's own database, so a projection and
  its position are saved together.

Why: a checkpoint kept elsewhere drifts from the data, and a restart replays or skips events.
