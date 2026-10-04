# Write standards

Each owner's standards are Markdown files in `src/OrgStandards.Migrations/seed/<owner>/`. They're
loaded into the owner's database every time the AppHost starts, so the files are the source of
truth: edit, restart, done. The seed standards are placeholders that show the format.

## The format

````markdown
---
title: Web UI                     # the document's name, shown with each topic
version: 2.0
kind: [ui, css, styling]          # what the code is, from seed/taxonomy.yaml
concern: [branding]               # what it must achieve (optional). Every other key is a tag
                                  # too: a value or a list, and filterable.
product: xyz-public-app           # only for product-specific documents
runtime: dotnet                   # only for runtime-specific documents (dotnet, node)
---

## Colors                         <- a topic. The name is what product documents override.
<!-- tags: { kind: [css] } -->    <- optional: this topic's own tags. Fields named here replace
                                     the document's; the rest are inherited.
- MUST use the Acme color tokens.
- MUST NOT use literal hex values.

Why: one line on the reason, so the agent can handle cases the rule didn't foresee.

### Tokens                        <- a detail: examples, code, reference tables.
| Token | Value |                    Not in the main document; fetched with get_topic.
|---|---|
| `--acme-primary` | `#1F4FD8` |
````

- **Topics** (`##`) are the unit everything works on: filtering returns topics, a product topic
  replaces the general topic with the same name, and the reviewer cites topics by name. A product
  topic's details replace the general topic's by title; the ones it doesn't define are inherited.
- **Rules** use MUST / MUST NOT / SHOULD. Everything else is guidance. Every rule must be possible to
  meet with what the company provides.
- **Details** (`###`) are free-form Markdown, stored as JSON on the server. Use them for anything
  that doesn't need to be in every answer.
- **Pointers to the environment** name a configuration key, never a host: "connect through
  `Cache:ConnectionString`", not a server address.
- **Links** to resources the owner points to are written `{{name}}` (for example
  `{{design-system}}/css/acme.css`). The owner's server fills in the address from its `Links:name`
  setting, set in the AppHost.
- **Citations:** a rule taken from a published standard cites it by identifier, in your own words:
  *(ASVS V8 Authorization; Top 10 A01:2025)*, *(WCAG 1.4.3)*.
- Text before the first `##`, and headings inside code fences, are ignored.

## State the intent; name tools as examples

A rule that names a tool fails projects that meet its intent another way. "Builds MUST install with
`npm ci`" failed a project using `pnpm install --frozen-lockfile`; "the lockfile MUST be committed
and installs MUST NOT change it (`npm ci`, `pnpm install --frozen-lockfile`)" doesn't.

## Runtimes

Write general rules so they hold on any runtime, and show how a runtime meets them in a detail
("Example (.NET)"). Rules that only make sense on one runtime go in a document with `runtime:`, and
each of its topics names the general topic it says how to meet:

```markdown
## Settings in .NET
<!-- tags: { kind: [configuration, backend], implements: [Settings] } -->
- Settings MUST be bound to options classes and validated with `.ValidateOnStart()`.
```

A runtime document reaches only projects that name its runtime, and a failure of both the general
rule and its runtime rule counts once.

## The taxonomy

[`seed/taxonomy.yaml`](../src/OrgStandards.Migrations/seed/taxonomy.yaml) defines every category a
standard may use, each with a description, its broader categories, its facet, and the published
source it comes from where there is one (ISO/IEC 25010, OWASP ASVS 5.0.0, W3C WCAG 2.2, W3C design
tokens, GitHub Linguist).

| Facet | Examples | Field |
|---|---|---|
| **kind**: what the code is | `api`, `backend`, `frontend`, `css`, `sass`, `react`, `data-access`, `testing` | `kind:` |
| **concern**: what it must achieve | `security`, `authorization`, `performance`, `caching`, `accessibility`, `ux`, `branding` | `concern:` |
| **runtime**: what it runs on | `dotnet`, `node` | `runtime:` |

The taxonomy is meant to grow. Tests keep it honest: every category a standard uses must be in the
taxonomy under its facet, and every category in the taxonomy must be used by at least one standard.

## Add a standards owner

1. Add the owner's name to `sources` in `src/OrgStandards.AppHost/Program.cs`.
2. Add a folder `src/OrgStandards.Migrations/seed/<name>/` with its Markdown documents.

Aspire gives it a database and a server, pgweb gets a bookmark for it, and the gateway starts
fanning out to it. No change to the gateway, the skills, or the agent.
