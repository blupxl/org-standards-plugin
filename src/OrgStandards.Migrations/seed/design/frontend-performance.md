---
title: Front-end performance
version: 1.0
kind: [frontend]
concern: [performance]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Bundle budget
- A page's initial JavaScript MUST stay under 200 KB compressed.
- Routes and heavy components SHOULD be loaded on demand (dynamic `import()`).

Why: script size is the largest cost of a page on a mid-range phone.
