# Company standards

This repository is Acme work. Acme's standards apply to everything built here, including mockups,
prototypes, and throwaway pages made to discuss a design.

Before building or changing anything user-facing (pages, forms, components, styles) or any
service or API:

- Use the `standards` skill from the company standards plugin. If the skill isn't available, call
  the standards tools (`list_standards`, `get_standards`, `get_topic`) directly.
- Start from this repository's scope and exclusions in `.claude/standards.json`, set by the
  architects. Use its exclusions exactly as written, and never add exclusions of your own: if a
  standard seems not to fit this project, say so and leave the decision to the architects.
- The standards server is the source of truth for colors, components and conventions. Don't invent
  them, and don't copy them from other projects.
