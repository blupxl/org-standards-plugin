---
title: Dependencies
version: 1.1
kind: [backend]
concern: [security, dependencies]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. Cites the OWASP Top
     10:2025 by identifier. -->

## Package sources
- Packages MUST come from the public registry or the company's internal feed only, and each package
  MUST have exactly one source.
- Package versions MUST be set in one place per repository, not per project.

Why: without source mapping, a public package with an internal package's name can be installed
instead (dependency confusion). *(Top 10 A03:2025 Software Supply Chain Failures)*

## Vulnerable packages
- Every build MUST audit dependencies for known vulnerabilities, and high or critical ones MUST
  fail the build.
- A known vulnerability MUST NOT be suppressed without a security review, recorded next to the
  suppression.

Why: most vulnerable code in an application is code it didn't write.
