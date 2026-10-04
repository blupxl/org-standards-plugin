---
title: XYZ Public App UI
version: 1.1
product: xyz-public-app
kind: [ui, css, styling, design-tokens]
concern: [branding]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Colors
- MUST load the XYZ stylesheet after the Acme one: `{{design-system}}/css/xyz.css`. It keeps the
  Acme components and applies the XYZ palette.
- Custom styles MUST use the XYZ color tokens (`var(--xyz-primary)`, `var(--xyz-surface)`,
  `var(--xyz-danger)`), not the Acme ones.
- MUST NOT use literal hex or rgb values.

Why: XYZ Public App is co-branded and has its own palette.

### Tokens
| Token | Value | Use for |
|---|---|---|
| `--xyz-primary` | `#00897B` | Primary actions, links |
| `--xyz-surface` | `#FAFAFA` | Page and card backgrounds |
| `--xyz-danger` | `#D84315` | Errors, destructive actions |

### Example
```html
<link rel="stylesheet" href="{{design-system}}/css/acme.css">
<link rel="stylesheet" href="{{design-system}}/css/xyz.css">
```
