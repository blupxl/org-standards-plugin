---
title: Secrets
version: 1.2
kind: [backend, configuration]
concern: [security, secrets]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. Cites OWASP ASVS 5.0.0
     and MITRE CWE by identifier. -->

## Secrets in source
- Passwords, keys, tokens and connection strings with credentials MUST NOT be committed, including
  in `appsettings*.json`, test files and comments. *(CWE-798 Use of Hard-coded Credentials)*
- Locally, secrets MUST come from a per-developer store outside the repository (in .NET,
  `dotnet user-secrets`; in Node, an untracked `.env` file); in deployed environments, from the
  platform's secret store (Azure Key Vault, Kubernetes Secrets), never from files in the image.
  *(ASVS V13 Configuration)*
- A secret that was committed MUST be rotated, not just deleted from the file.

Why: git history keeps everything; a deleted secret is still in every clone.

## Sensitive data in logs
- Logs MUST NOT contain secrets, access tokens, passwords, or full payment card numbers.
  *(ASVS V14 Data Protection, V16 Security Logging and Error Handling)*
- Personal data in logs SHOULD be limited to identifiers (a customer id), not contents (a name or
  address).

Why: logs are copied to more places, kept longer, and read by more people than databases.
