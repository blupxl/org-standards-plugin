---
title: User experience
version: 1.0
kind: [ui, content]
concern: [ux]
---
<!-- PLACEHOLDER standards that exercise the format. Replace with real ones. -->

## Error messages
- User-facing error messages MUST use plain language and MUST NOT include stack traces or internal codes.
- SHOULD say what the user can do next, and SHOULD appear in an `acme-alert--error`.

Why: an error is the moment users most need help, and the least able to decode jargon.

## Forms
- Every input MUST have a visible `<label>`; placeholder text MUST NOT replace it.
- Validation messages SHOULD appear when a field loses focus or on submit, not on every keystroke.
- On a failed submit, focus MUST move to the first field with an error.

Why: labels that vanish while typing lose context; early validation scolds users mid-word.

### Example
```html
<div class="acme-field">
  <label for="email">Work email</label>
  <input id="email" type="email" autocomplete="email" aria-describedby="email-error">
  <span id="email-error" class="acme-error">Enter an address like name@acme.com.</span>
</div>
```

## Loading states
- Anything that takes longer than 300 ms MUST show progress: a skeleton for content, a spinner
  for actions.
- Buttons MUST be disabled while their action runs, to prevent double submission.

Why: without feedback, users click again, and a second submit can mean a second order.
