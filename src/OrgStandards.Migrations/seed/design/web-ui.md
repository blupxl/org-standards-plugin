---
title: Web UI
version: 2.1
technology: [web-api, ui]
area: [branding, user-interaction]
company: acme
---
<!-- PLACEHOLDER standards that exercise the format. Every rule can be met with the demo design
     system (Acme.Web). {{design-system}} is filled in by the design standards server. -->

## Colors
- MUST use the Acme color tokens (`var(--acme-primary)`, `var(--acme-surface)`, `var(--acme-danger)`)
  from the Acme stylesheet.
- MUST NOT use literal hex or rgb values in pages, components or stylesheets.

Why: the design team changes the palette in one place; literal values never pick that up.

### Tokens
| Token | Value | Use for |
|---|---|---|
| `--acme-primary` | `#1F4FD8` | Primary actions, links |
| `--acme-surface` | `#FFFFFF` | Page and card backgrounds |
| `--acme-danger` | `#C62828` | Errors, destructive actions |

## Components
- Pages MUST load the Acme stylesheet from the design system: `{{design-system}}/css/acme.css`.
- Buttons, form fields and cards MUST use the Acme component classes: `acme-button`,
  `acme-field`, `acme-card`. Put `acme-page` on `<body>`.
- SHOULD NOT restyle the component classes; ask the design team for a variant instead.

Why: shared components carry accessibility and behavior fixes to every page at once.

### Classes
Every class is shown at {{design-system}}.

| Class | Use for |
|---|---|
| `acme-page` | `<body>`: page background, text color, font |
| `acme-card` | A surface grouping content or a form |
| `acme-field` | A wrapper around a `<label>` and its input, select or textarea |
| `acme-hint` / `acme-error` | Help text / error text inside an `acme-field` |
| `acme-button` | Primary action; add `acme-button--secondary` for a quieter one |
| `acme-alert`, `acme-alert--error` | A message about the page's state |

### Example
```html
<link rel="stylesheet" href="{{design-system}}/css/acme.css">

<body class="acme-page">
  <form class="acme-card">
    <div class="acme-field">
      <label for="location">Location</label>
      <select id="location"><option>Atlanta, GA</option></select>
      <span class="acme-hint">Choose one of your sites.</span>
    </div>
    <button class="acme-button" type="submit">Save</button>
    <button class="acme-button acme-button--secondary" type="button">Cancel</button>
  </form>
</body>
```

## Error messages
- User-facing error messages MUST use plain language and MUST NOT include stack traces or internal codes.
- SHOULD say what the user can do next, and SHOULD appear in an `acme-alert--error`.
