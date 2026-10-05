---
title: Layout
version: 1.2
kind: [layout, css, styling, ui]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Spacing
- Margins, padding and gaps MUST use the spacing tokens from the Acme stylesheet:
  `var(--acme-space-1)` (4px) through `var(--acme-space-8)` (64px).
- MUST NOT use literal lengths for spacing.

### Scale
| Token | Value |
|---|---|
| `--acme-space-1` | 4px |
| `--acme-space-2` | 8px |
| `--acme-space-3` | 12px |
| `--acme-space-4` | 16px |
| `--acme-space-5` | 24px |
| `--acme-space-6` | 32px |
| `--acme-space-7` | 48px |
| `--acme-space-8` | 64px |

Why: a shared scale is what makes separately built pages look like one product.

## Breakpoints
- Styles MUST be written mobile-first: base styles for small screens, `min-width` media queries
  for larger ones.
- MUST use the shared breakpoints only: 600px (tablet), 960px (desktop), 1280px (wide).

Why: one set of breakpoints means components switch layout together.

## Units
<!-- tags: { kind: [css, layout], concern: [accessibility, ux] } -->
- Font sizes MUST use `rem`. MUST NOT set font sizes in `px`.
- Layout SHOULD use flexbox or grid; MUST NOT use floats for layout.

Why: `rem` respects the user's browser font setting, an accessibility requirement as much as a
style one.
