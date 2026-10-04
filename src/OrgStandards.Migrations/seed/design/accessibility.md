---
title: Accessibility
version: 1.0
kind: [ui]
concern: [accessibility, ux]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. Cites W3C WCAG 2.2. -->

## Conformance target
- Everything user-facing MUST meet WCAG 2.2 level AA.

Why: AA is the level most accessibility laws and procurement rules refer to.

## Keyboard
- Every action MUST be reachable and usable with the keyboard alone. *(WCAG 2.1.1 Keyboard)*
- Focus MUST be visible, and MUST NOT be hidden behind sticky headers or banners.
  *(WCAG 2.4.7 Focus Visible, 2.4.11 Focus Not Obscured)*
- Tab order MUST follow the visual order. MUST NOT use a positive `tabindex`.

Why: keyboard users include screen-reader users and anyone who can't use a mouse.

## Color contrast
- Text MUST have a contrast ratio of at least 4.5:1 with its background; large text (24px, or
  19px bold) at least 3:1. *(WCAG 1.4.3 Contrast (Minimum))*
- Color MUST NOT be the only way information is shown, for example errors in red only.
  *(WCAG 1.4.1 Use of Color)*

Why: low contrast fails in sunlight and for many users with low vision.

## Text alternatives
- Informative images MUST have `alt` text that says what the image conveys; decorative images
  MUST have `alt=""`. *(WCAG 1.1.1 Non-text Content)*
- Icon-only buttons MUST have an accessible name (`aria-label`).

Why: a screen reader reads out exactly what's there; a missing name reads as "button".
