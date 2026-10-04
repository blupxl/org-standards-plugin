---
title: Testing
version: 1.1
kind: [testing]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Unit tests
- Unit tests MUST NOT touch the network, the file system or a database; those belong in
  integration tests.
- Test names SHOULD describe the behavior (`A_cancelled_order_is_not_charged`).

Why: unit tests that touch the outside world become slow and flaky, and then nobody runs them.

## Integration tests
- Services MUST have integration tests that start the real wiring (the service with its actual
  dependencies), not mocks of it.
- Integration tests MUST be marked so unit tests can run on their own.

Why: most production failures are in the wiring between services, which unit tests never see.
