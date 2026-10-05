# Set up a project

The repository, not each prompt, tells Claude that standards apply, through two files. The
quickest way to write them is [`/acme:init`](skills/init.md): it scans the project, asks you to
confirm what it found, and writes both. You can also copy
[`project-template/.claude/`](../project-template/.claude/) and fill it in by hand.

| File | What it does |
|---|---|
| `.claude/CLAUDE.md` | Loaded into every Claude Code session in that folder, so a plain request such as "make me a form for our designer" picks up the standards without mentioning them. Its standards section (between `<!-- standards:start -->` and `<!-- standards:end -->`) says what the project is and points Claude at `standards.json`; the rest of the file is yours. |
| `.claude/standards.json` | The project's **scope** and **exclusions**, set by the architects, much as `.claude/settings.json` lists the commands Claude is allowed to run. |

## standards.json

```json
{
  "description": "An orders API and the storefront that uses it.",
  "product": [],
  "components": [
    { "name": "api", "path": "src/Orders.Api", "scope": { "kind": ["api", "backend", "data-access"], "runtime": ["dotnet"],
      "uses": ["postgres", "kafka"], "pattern": ["ddd", "cqrs"] } },
    { "name": "web", "path": "web", "scope": { "kind": ["frontend", "react", "css"] } }
  ],
  "exclude": [
    { "concern": ["branding"], "reason": "The storefront keeps the partner's look; approved by architecture, 2026-10." }
  ]
}
```

- **`description`**: what the project is and does, also written into `.claude/CLAUDE.md`.
- **`product`**: applies to every component; empty means the company-wide standards.
- **`components`**: the parts built differently, each with its path and its own scope, so a .NET
  API and a JavaScript front end in one repository each get their own rules. A project with one
  component can use a single `"scope"` instead.

### scope

The filter Claude starts from for a component, narrowed or extended for each task.

- **`kind`**: what the code is (`api`, `backend`, `frontend`, `css`, `react`, …).
- **`runtime`**: what it runs on (`dotnet`, `node`). Standards written for a runtime reach only
  projects that name it; every other standard applies to all.
- **`uses`**: what it depends on (`postgres`, `sqlserver`, `mongodb`, `redis`, `kafka`,
  `eventuous`). Postgres rules reach only components that use Postgres, and so on.
- **`pattern`**: the architecture it follows (`ddd`, `cqrs`, `event-sourcing`).
- **`product`**: for a product repository, for example `["xyz-public-app"]`. Product topics replace
  the general topics with the same name. The public website is
  `"product": ["acme-website"], "kind": ["website", "ui", "content"]`.
- **`concern`**: usually left out, so every concern (security, accessibility, branding, …) applies.
  Setting it narrows the scope to those concerns only.

Valid fields and values are whatever `list_standards` returns; see
[how it works](how-it-works.md#filters) and the [taxonomy](writing-standards.md#the-taxonomy).

### exclude

The standards this project may leave out, each with its **reason**. An entry names a field and
values (`{ "concern": ["branding"] }`), or `{ "topic": ["Colors"] }` for single topics.

- Claude passes them to the server exactly as written and **never adds its own**. If a standard
  seems not to fit, Claude says so and leaves the decision to the architects.
- The server leaves excluded topics out and lists them in every answer as *excluded*: not checked,
  and never counted as passed.
- An [assessment](skills/assess.md) may leave something out because you asked, for that run only,
  but reports it as *requested in this session, not approved* and suggests recording it here.

## Without standards.json

Claude still works: it works out the scope from the repository (its files, README and project
names), asks which product it is if that isn't clear, and doesn't guess. Older repositories that
declare a scope inside `.claude/CLAUDE.md` still work too.
