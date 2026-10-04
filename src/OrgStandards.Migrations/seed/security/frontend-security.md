---
title: Front-end security
version: 1.0
kind: [frontend, react]
concern: [security]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. Cites OWASP ASVS 5.0.0
     and MITRE CWE by identifier. -->

## Content Security Policy
- Pages MUST send a Content Security Policy that disallows inline scripts (no `'unsafe-inline'`
  for `script-src`) and allows scripts only from our own origin and listed hosts.
  *(ASVS V3 Web Frontend Security)*

Why: if an injection gets through, the policy stops the browser from running it.

## Untrusted HTML
- User-provided content MUST be rendered as text. React code MUST NOT use
  `dangerouslySetInnerHTML` with it; where HTML is required, it MUST be sanitized with DOMPurify
  first. *(CWE-79 Cross-site Scripting)*

Why: rendering a user's text as HTML lets them run script in every other user's browser.

## Third-party scripts
- Scripts from other origins MUST be approved by the security team, listed in the Content Security
  Policy, and loaded with Subresource Integrity (`integrity="sha384-…"`) when the file is static.
- Analytics and tag managers MUST NOT run on pages that show payment or account details.

Why: a third-party script runs with the page's full privileges; its vendor's breach becomes ours.
