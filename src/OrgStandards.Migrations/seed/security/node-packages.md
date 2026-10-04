---
title: Node packages
version: 1.1
runtime: node
kind: [backend, frontend]
concern: [security, dependencies]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. How the Dependencies
     standard is met with npm. Cites the OWASP Top 10:2025 by identifier. -->

## Node lockfiles
<!-- tags: { implements: [Package sources] } -->
- The package manager's lockfile MUST be committed, and builds MUST install from it without
  changing it (`npm ci`, `pnpm install --frozen-lockfile`, `yarn install --immutable`).
- Scoped internal packages (`@acme/*`) MUST resolve from the internal registry (`.npmrc`).

Why: without a lockfile and a scope mapping, a build can install a different package than the one
that was reviewed. *(Top 10 A03:2025 Software Supply Chain Failures)*

## Node package audit
<!-- tags: { implements: [Vulnerable packages] } -->
- Builds MUST run the package manager's audit at the high level (`npm audit --audit-level=high`,
  `pnpm audit --audit-level high`), so high and critical vulnerabilities fail the build.

Why: an audit that doesn't fail the build is read by no one.
