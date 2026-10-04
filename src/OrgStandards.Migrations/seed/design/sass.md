---
title: Sass
version: 1.0
kind: [sass, css, styling]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Sass modules
- MUST load other Sass files with `@use` and `@forward`. MUST NOT use `@import`.
- Partials MUST be named with a leading underscore (`_buttons.scss`) and imported without it.

Why: `@import` is deprecated in Dart Sass and leaks every variable into every file; `@use`
namespaces them.

### Example
```scss
@use "tokens";

.order-summary {
  padding: tokens.$space-4;
}
```

## Sass variables
- Sass variables MUST take their values from the Acme design tokens (the CSS custom properties in
  the Acme stylesheet). MUST NOT introduce new color or size literals.
- SHOULD prefer the CSS custom property itself (`var(--acme-primary)`) when the value can change
  at runtime, for example per theme.

Why: a Sass variable is fixed at build time; the design system changes values at runtime.

## Nesting
- Selectors MUST NOT nest more than three levels deep.
- SHOULD nest only for states and children of one component (`&:hover`, `&__title`).

Why: deep nesting produces long, specific selectors that are hard to override and slow to match.
