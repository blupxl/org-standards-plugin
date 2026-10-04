---
title: .NET packages
version: 1.1
runtime: dotnet
kind: [backend]
concern: [security, dependencies]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. How the Dependencies
     standard is met with NuGet. Cites the OWASP Top 10:2025 by identifier. -->

## .NET package sources
<!-- tags: { implements: [Package sources] } -->
- `nuget.config` MUST map every package to one source with `<packageSourceMapping>`.
- Package versions MUST be set in `Directory.Packages.props` (central package management).

Why: source mapping is NuGet's defense against dependency confusion.
*(Top 10 A03:2025 Software Supply Chain Failures)*

## .NET package audit
<!-- tags: { implements: [Vulnerable packages] } -->
- NuGet audit MUST stay on, and warnings for high and critical vulnerabilities (`NU1903`,
  `NU1904`) MUST be treated as errors (`<WarningsAsErrors>NU1903;NU1904</WarningsAsErrors>`).

Why: an audit warning that doesn't fail the build is read by no one.
