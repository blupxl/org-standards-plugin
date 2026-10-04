---
title: Node services
version: 1.1
runtime: node
kind: [backend, api, testing]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. How the general
     platform standards are met on Node.js; they reach only projects that name the node runtime. -->

## Node versions
<!-- tags: { kind: [backend, api] } -->
- Services MUST run on an active LTS release of Node.js, stated in `package.json` (`engines.node`)
  and in `.nvmrc`.

Why: LTS releases get security fixes; odd-numbered releases stop within months.

## Tests in Node
<!-- tags: { kind: [testing], implements: [Unit tests] } -->
- Tests MUST use Vitest.
- Unit tests MUST NOT read environment variables to decide whether to call real services; a test
  that needs a network belongs in the integration tests.

Why: a test that sometimes calls the network passes on one machine and fails on another.
