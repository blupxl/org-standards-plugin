# How it works

```
Claude Code
  └─ plugin "acme" (plugins/my-company, named by the local catalog entry)
       ├─ skill:  /acme:init
       ├─ skill:  /acme:standards
       ├─ skill:  /acme:assess
       ├─ skill:  /acme:classify
       ├─ skill:  /acme:plan-check
       ├─ agent:  acme:standards-reviewer
       ├─ agent:  acme:classifier
       ├─ agent:  acme:plan-checker
       ├─ hook:   plan finished (runs locally, no network)
       └─ MCP:    http://localhost:5480/mcp ── gateway (front door)
                                                 ├─ design    (MCP server + its own database)
                                                 │    └─ points to acme-web: the design-system site (stylesheet)
                                                 ├─ platform  (MCP server + its own database)
                                                 ├─ security  (MCP server + its own database)
                                                 └─ data      (MCP server + its own database)

Everything above runs on your machine.
```

- **The plugin** is the only thing a developer installs: five skills,
  [init](skills/init.md), [standards](skills/standards.md), [assess](skills/assess.md),
  [classify](skills/classify.md) and [plan-check](skills/plan-check.md), the
  [reviewer agent](skills/standards-reviewer.md), the classifier and plan-checker agents, a
  [hook](skills/plan-check.md#the-five-phases) for finished plans, and the address of the gateway.
- **The gateway** is the front door. It owns no standards: it fans each request out to the owners,
  merges their answers, applies the product overlay and the project's exclusions, and renders one
  document per request.
- **Each owner** (design, platform, security, data) runs its own MCP server with its own database, loaded
  from its folder of Markdown documents. Adding an owner is a folder and a line in the AppHost
  ([how](writing-standards.md#add-a-standards-owner)).

**Server instructions.** The gateway also sends short instructions that Claude Code adds to every
session's context, so every agent (planning skills from other plugins too) reads the project's
`.claude/standards.json` and fetches the standards and [recipes](templates.md) before planning.

An owner can point to resources it owns, such as the design team's site. Its standards write
`{{design-system}}`, and its server fills in the address, which the AppHost gives it. The standards
never hard-code where anything lives, and the gateway just passes the answer along.

## The tools

| Tool | What it does |
|---|---|
| `list_standards(filter?)` | Discovery: which fields exist (product, kind, concern, runtime, ...) and which values are available under a filter. Returns JSON. |
| `get_standards(requests[])` | The standards to follow, as one Markdown document per request (several in one call). Each request has its own `filter`, and optionally the project's `exclude`. With `headlines: true` it returns only each topic's name, owner and first rule, so a caller can pick what to fetch in full. |
| `get_taxonomy()` | The categories standards are filed under, each with its facet, description, and the file patterns and package or image names that point to it. Used to classify work. Comes from one configured owner; if that owner is down it reports `unavailable`. Takes no input. |
| `get_topic(topic, filter?, exclude?)` | One topic in full, with its details: examples, code, reference tables such as color tokens. The main document says when a topic has more. |

## Plans

A small hook on the developer's machine notices when a plan is finished and asks for the
[plan check](skills/plan-check.md), once for each version of the plan. The hook makes no network
calls. The check classifies the plan, asks `get_standards` for headlines, fetches only the topics
the plan's steps touch, and proposes changes you approve. Only filters and topic names reach the
gateway.

## The document

```markdown
# Standards
> **Scope:** product = xyz-public-app · kind = css, backend
> **Status:** complete
> **Sources:** data ✓, design ✓, platform ✓, security ✓
> **Precedence:** already resolved. A product-specific topic replaces the general topic with the same name, and says so.
> **Keywords:** MUST / MUST NOT = required · SHOULD = strong default · anything else is guidance.
> **More detail:** topics with examples or reference material (e.g. color tokens) name the `get_topic` call that fetches it.

## Colors
*design · XYZ Public App UI v1.1 · product: xyz-public-app · replaces the general topic (design v2.2)*

- MUST use the XYZ color tokens (`--xyz-primary`, `--xyz-surface`, `--xyz-danger`), not the Acme ones.
- MUST NOT use literal hex or rgb values.

Why: XYZ Public App is co-branded and has its own palette.

*More in this topic: Tokens, Example. Fetch with `get_topic("Colors")` and the same filter.*
```

The header before the first topic is a fixed contract: what was asked, whether the answer is
complete, which owners answered, and how to read the rules. Warnings go there too:

| Status or note | Means |
|---|---|
| `complete` | Every owner answered. |
| `partial` | An owner didn't answer; its standards are missing and it's named. |
| `unavailable` | No owner answered. Nothing is shown, and the filter isn't blamed. |
| `unresolved` | A product name didn't match; "did you mean" suggestions follow. |
| `invalid` | An unknown field; the valid ones are listed. |
| *Excluded by the project's scope* | Topics the project's exclusions left out, with the matching exclusion. Not checked; not passes. |

A `## Not covered` section lists requested values nothing answered, so the agent doesn't read
silence as permission. A runtime topic's line also says which general topic it `implements`, so a
reviewer counts one failure, not two.

## Filters

A filter is JSON: field → accepted values. **OR within a field, AND across fields.** Standards are
classified from a defined [taxonomy](writing-standards.md#the-taxonomy) in five facets:

| Field | What it says | How it filters |
|---|---|---|
| `kind` | What the code is: `api`, `backend`, `css`, `react`, … | Narrows |
| `concern` | What it must achieve: `security`, `performance`, `accessibility`, … | Narrows; `{ "kind": ["api"], "concern": ["security"] }` is API security, not browser security |
| `runtime` | What it runs on: `dotnet`, `node` | Qualifies: a standard written for a runtime comes back only when the request names it; every other standard applies to all |
| `uses` | What it depends on: `postgres`, `sqlserver`, `mongodb`, `redis`, `kafka`, `eventuous` | Qualifies, like `runtime`: Postgres rules reach only components that use Postgres |
| `pattern` | The architecture it follows: `ddd`, `cqrs`, `event-sourcing` | Qualifies, like `runtime`: event-sourcing rules reach only event-sourced components |
| `product` | Which product: `xyz-public-app`, `acme-website` | Qualifies and layers: a product topic replaces the general topic with the same name |

- A batch can give each component its own concerns: security for the API and performance for the
  page, in one call.
- The gateway widens each `get_standards` and `get_topic` request's `kind` with the broader kinds
  from the taxonomy, up to the root. A request for `sass` also matches `css` and `styling`. The
  document then says `> **Also applies:** css, styling (broader kinds of the requested kind).` If
  the taxonomy is unavailable and the request has a kind, it says `> ⚠ The taxonomy is
  unavailable, so broader kinds weren't added.` and the answer may miss rules. Concerns and
  qualifiers are never widened, and `list_standards` isn't either.
- Unknown fields are rejected with the valid ones listed; unknown values come back with
  suggestions. Nulls and blanks in a filter are ignored.
- One deployment serves one company's standards, so there's no company field: the plugin's server
  address decides whose standards these are.
