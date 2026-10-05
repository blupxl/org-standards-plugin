---
title: Kafka
version: 1.0
uses: kafka
kind: [backend]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. They reach only
     components that use Kafka. -->

## Kafka topics
- Topics MUST be named `<domain>.<entity>.<event>.v<n>` (`sales.order.placed.v1`), and a breaking
  change to a message MUST go to a new version.
- Messages MUST have a schema in the schema registry.

Why: consumers in other teams depend on the topic's name and shape; both are part of the contract.

## Kafka consumers
- Consumers MUST be idempotent: Kafka delivers at least once, so the same message can arrive twice.
- The message key MUST be the entity's id, so events for one entity stay in order (Kafka orders
  messages only within a partition).

Why: duplicates and reordering are normal in Kafka; a consumer that assumes otherwise corrupts data.
