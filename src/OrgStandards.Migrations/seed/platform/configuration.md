---
title: Configuration
version: 1.1
kind: [configuration, backend]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Settings
- Settings MUST be validated when the service starts: a missing or invalid setting MUST stop the
  startup, not fail a request later.
- Code MUST read settings through one typed settings object per section, read once, not by looking
  up keys or environment variables throughout the code.

Why: a missing or mistyped setting fails the deployment at startup, not a user's request later.

## Environments
- Values that differ between environments MUST come from configuration (environment variables,
  `Section__Key`), never from `#if` or code that checks the environment name.
- `appsettings.json` MUST hold only safe defaults; environment files MUST NOT hold secrets
  (see the Secrets standard).

Why: the same build runs in every environment; only its configuration changes.
