# Company standards

This repository is Acme work. Acme's standards apply to everything built here, including mockups,
prototypes, and throwaway pages made to discuss a design.

Before building or changing anything user-facing (pages, forms, components, styles) or any
service or API:

- Use the `standards` skill from the company standards plugin. If the skill isn't available, call
  the standards tools (`list_standards`, `get_standards`, `get_topic`) directly.
- Start from this repository's scope, set by the architects:

  ```json
  { "company": ["acme"], "technology": ["ui"], "area": ["branding", "user-interaction"] }
  ```

  `company` is required: standards are kept per company. No product is set, so general Acme
  standards apply. A product repository adds `"product": ["<product-name>"]`; the company's
  public website, for example, is `"product": ["acme-website"], "technology": ["website", "ui"]`.
- The standards server is the source of truth for colors, components and conventions. Don't invent
  them, and don't copy them from other projects.
