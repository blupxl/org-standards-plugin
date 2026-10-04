---
title: Front-end code
version: 1.1
kind: [frontend]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## TypeScript
- Browser code MUST be written in TypeScript with `"strict": true`.
- MUST NOT use `any` except at the boundary with untyped third-party code, and there with a comment.

Why: strict types catch the null and shape errors that otherwise reach users.

## Browser support
- MUST support the last two versions of Chrome, Edge, Firefox and Safari.
- MUST NOT ship polyfills for browsers outside that list.

Why: every polyfill is paid for by every user, including the ones who don't need it.
