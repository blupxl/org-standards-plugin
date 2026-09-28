# org-standards-plugin

A Claude Code plugin that gives developers their company's standards at the point of work:
the rules for *this* product and *this* kind of component, pulled from the teams that own them.

- **A skill** (`/acme:standards`) that brings the right standards into the conversation.
- **A reviewer agent** that checks finished work against the same standards, in its own context.
- **One MCP server as the front door**, fanning out to the servers that own each set of standards
  (design, platform, ...). Standards are Markdown documents written by those teams; publishing
  one is adding a file. The skill never changes.

"Acme" is a stand-in. The plugin is generic (`plugins/my-company`), and a company's marketplace
entry gives it its name. Why it's built this way, decision by decision:
[design-notes.md](design-notes.md).

> **Status: proof of concept.** Built in a short working session to explore the design, not
> production code. It runs end to end on placeholder standards, with unit and integration tests,
> but it has no authentication and filters in memory. What a production version would need is
> listed in [design-notes.md](design-notes.md#what-a-production-version-would-need).

## How it fits together

```
Claude Code
  └─ plugin "acme" (plugins/my-company, named by the marketplace entry)
       ├─ skill:  /acme:standards
       ├─ agent:  acme:standards-reviewer
       └─ MCP:    http://localhost:5480/mcp ── gateway (front door)
                                                 ├─ design    (MCP server + its own database)
                                                 │    └─ points to acme-web: the design-system site (stylesheet)
                                                 └─ platform  (MCP server + its own database)
```

An owner can point to resources it owns, such as the design team's site. Its standards write
`{{design-system}}`, and its server fills in the address, which the AppHost gives it. The standards
never hard-code where anything lives, and the gateway just passes the answer along.

The gateway exposes three tools:

| Tool | What it does |
|---|---|
| `list_standards(filter?)` | Discovery: which fields exist (product, technology, area, ...) and which values are available under a filter. Returns JSON. |
| `get_standards(requests[])` | The standards to follow, as one Markdown document per request (several in one call). |
| `get_topic(topic, filter?)` | One topic in full, with its details: examples, code, reference tables such as color tokens. The main document says when a topic has more. |

### What the implementing agent receives

```markdown
# Standards
> **Scope:** product = xyz-public-app · technology = web-api
> **Status:** complete
> **Sources:** design ✓, platform ✓
> **Precedence:** already resolved. A product-specific topic replaces the general topic with the same name, and says so.
> **Keywords:** MUST / MUST NOT = required · SHOULD = strong default · anything else is guidance.
> **More detail:** topics with examples or reference material (e.g. color tokens) name the `get_topic` call that fetches it.

## Colors
*design · XYZ Public App UI v1.0 · product: xyz-public-app · replaces the general topic (design v2.0)*

- MUST use the XYZ color tokens (`--xyz-primary`, `--xyz-surface`, `--xyz-danger`), not the Acme ones.
- MUST NOT use literal hex or rgb values.

Why: XYZ Public App is co-branded and has its own palette.

*More in this topic: Tokens. Fetch with `get_topic("Colors")` and the same filter.*
```

The header before the first topic is a fixed contract: what was asked, whether the answer is
complete, which owners answered, and how to read the rules. Warnings go there too: an owner that
was down (`partial`), a product name that didn't match (`unresolved`, with "did you mean"), an
unknown field (`invalid`, with the valid ones). A `## Not covered` section lists requested values
nothing answered, so the agent doesn't read silence as permission.

Filters are JSON: field → accepted values, OR within a field, AND across fields. `company` is
required: standards are kept per company and never mix (a request without it is `invalid` and
lists the companies). Within a company, `product` layers over the general standards. Unknown fields
are rejected with the valid ones listed; unknown values come back with suggestions. If an owner's
server is down, the answer is marked `partial` and names it, instead of failing or guessing.

## Requirements

Everything runs on your machine. You need three things, plus Claude Code to use the plugin:

| Need | Get it | Check it |
|---|---|---|
| **.NET 10 SDK** (10.0.401 or later 10.x; pinned in `src/global.json`) | [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0) | `dotnet --list-sdks` lists a `10.0.x` |
| **A container runtime** (runs one Postgres container) | See [Container runtime](#container-runtime) below | See below |
| **Claude Code** | [code.claude.com/docs/en/setup](https://code.claude.com/docs/en/setup) | `claude --version` |
| Aspire CLI *(optional, only for `aspire run`)* | [aspire.dev/get-started/install-cli](https://aspire.dev/get-started/install-cli/) | `aspire --version` |

Tested on Windows 11 with Rancher Desktop (Docker engine 27.3.1). macOS and Linux should work, but
haven't been tried.

### Container runtime

Pick one. Aspire's own guidance is on its
[prerequisites page](https://aspire.dev/get-started/prerequisites/).

| Runtime | Install | Setup for this project | Check it's running |
|---|---|---|---|
| **Docker Desktop** (Aspire's recommended default) | [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop/) | None | `docker info` prints a *Server* section |
| **Podman** (Podman Desktop) | [podman-desktop.io](https://podman-desktop.io/) | Tell Aspire to use it: set `ASPIRE_CONTAINER_RUNTIME=podman` before running | `podman info` |
| **Rancher Desktop** (community-supported by Aspire; what this was tested on) | [rancherdesktop.io](https://rancherdesktop.io/) | In *Preferences → Container Engine*, choose **dockerd (moby)**; containerd doesn't serve the Docker API | `docker info` prints a *Server* section |

Setting the Podman variable for one terminal session:

```bash
export ASPIRE_CONTAINER_RUNTIME=podman       # bash / zsh
$env:ASPIRE_CONTAINER_RUNTIME = "podman"     # PowerShell
```

## Run the services

1. **Start your container runtime** and wait until its check above succeeds.
2. **Run the AppHost.** It starts everything else, and there is nothing to configure.

   | From | Do |
   |---|---|
   | Visual Studio / Rider | Open `src/OrgStandards.slnx` and run **OrgStandards.AppHost** (the first project, and the startup project on a fresh clone). |
   | Command line | `dotnet run --project src/OrgStandards.AppHost --launch-profile http` |
   | Aspire CLI | `cd src` then `aspire run` |

3. **Check it's up:** `http://localhost:5480/` shows the getting-started page. Visual Studio opens
   it for you.

Don't start the other projects on their own: they get their names, databases, and addresses from
the AppHost, and will say so if started directly.

**Before the first run:**

- **HTTPS certificate.** Visual Studio starts the AppHost with its `https` profile, which needs a
  trusted .NET development certificate. If the dashboard complains about the certificate, run
  `dotnet dev-certs https --trust` once
  ([details](https://learn.microsoft.com/dotnet/core/tools/dotnet-dev-certs)), or choose the
  `http` profile. The command line above already uses `http`.
- **Ports.** `5480` (gateway) and `5500` (design-system site) must be free.

The first run pulls the Postgres image, so it takes a minute. Startup order is handled by Aspire:

1. **postgres** starts, with one database per standards owner.
2. **migrations** migrates each database and loads the Markdown documents in
   `OrgStandards.Migrations/seed/<owner>/`, replacing what was there. The files are the source of
   truth. If it fails, it exits non-zero and nothing downstream starts.
3. **design** and **platform** start once migrations have finished.
4. **gateway** listens on `http://localhost:5480/mcp`.
5. **acme-web**, a stand-in for Acme's design-system site, at `http://localhost:5500`. It owns the
   stylesheet the design standards require (`/css/acme.css`, plus `/css/xyz.css` for the XYZ
   palette and `/css/website.css` for the public website), so every rule can be met by a plain HTML
   page.
6. **pgweb**, a small web UI for the databases, opens from the dashboard with one bookmark per
   owner's database, registered automatically (a new owner gets its bookmark on the next run).

The AppHost opens (or prints a link to) the Aspire dashboard, which shows every resource, its
logs, and traces of each fan-out call.

## Run the tests

```bash
dotnet test src/OrgStandards.Tests                                  # everything
dotnet test src/OrgStandards.Tests --filter "Category!=Integration"  # unit tests only, no Docker
```

- **Unit tests** (38) check the rules the gateway applies: which topics a filter selects, the
  company partition, the product overlay and inherited details, "did you mean" suggestions, the
  document header (`complete`, `partial`, `unresolved`, `invalid`), the Markdown parser, and link
  resolution. They need nothing running and take well under a second.
- **Integration tests** (4) start the whole AppHost inside the test run, with Postgres in a
  container and randomized ports, so they don't clash with a running copy. They call the gateway
  over MCP, the way Claude Code does. They need a container runtime and are skipped, not failed,
  without one. The first run pulls the Postgres image.

## Try it

Everything is local: the services run on your machine, and the plugin loads from this folder.
Nothing connects to an outside server.

With the AppHost running, from the repository root:

```bash
claude --plugin-dir ./plugins/my-company
```

This loads the plugin (skill, agent, and MCP tools) for that session only. Nothing is installed
or saved, so there's nothing to undo. Loaded this way the plugin is named after its folder,
`my-company`. To see it under a company's name (`acme`), install it from the local marketplace
instead; see below.

## Set up a project

The repository, not each prompt, tells Claude that standards apply. Architects commit
`project-template/.claude/CLAUDE.md` into each repository (as `.claude/CLAUDE.md`) and set its
scope:

```json
{ "company": ["acme"], "technology": ["ui"], "area": ["branding", "user-interaction"] }
```

Add `"product": ["<product-name>"]` for a product repository. The company's public website is
`{ "company": ["acme"], "product": ["acme-website"], "technology": ["website", "ui"] }`. Claude Code loads `CLAUDE.md` into
every session in that folder, so a plain request such as "make me a form for our designer" picks up
the standards without mentioning them. The skill starts from the declared scope instead of guessing
it.

## One plugin, any company's marketplace

The plugin has no name of its own. `plugins/my-company` ships without a `plugin.json`, so the
name comes from whichever marketplace lists it:

- Acme's marketplace lists it as `acme` → developers get `/acme:standards` and `acme:standards-reviewer`.
- Another company lists the same code as `globex` → `/globex:standards`, with no code changes.

The marketplace is the only company-specific piece: a catalog (`.claude-plugin/marketplace.json`)
that names the plugin and says where its code lives. Here it is a folder in this repository, and
the plugin's source is a relative path inside it, so nothing is fetched from anywhere. A platform
team would generate one for their company. Projects refer to the plugin as `<name>@<marketplace>`.

## Use it in a project

A project opts in by committing a pointer in its own `.claude/settings.json`. For a project `foo`:

```jsonc
// foo/.claude/settings.json
{
  "extraKnownMarketplaces": {
    "acme-standards": {
      "source": { "source": "directory", "path": "C:\\path\\to\\org-standards-plugin" }
    }
  },
  "enabledPlugins": {
    "acme@acme-standards": true
  }
}
```

What happens when a developer opens `foo` in Claude Code:

1. **Trust first.** Project settings can only turn plugins on after the developer accepts the
   workspace trust dialog for `foo` in an interactive session. Until then the plugin shows as
   disabled. Trusting a parent folder or running `claude -p` isn't enough.
2. **The marketplace is added in the background**, and the plugin loads straight from it. It uses
   a relative-path source, so there is no separate install step.
3. **The MCP server needs approval**, like any server a project declares.

A developer who doesn't want it in `foo` sets `"acme@acme-standards": false` in
`foo/.claude/settings.local.json`. Cloud sessions (claude.ai/code) don't add a project's
marketplaces, because they never show the trust dialog.

### Install under the company name (optional)

To use the plugin as `acme` without a project opting in, register this repository as a local
marketplace and install from it. From the repository root:

```bash
claude plugin marketplace add .
claude plugin install acme@acme-standards --scope local
```

`--scope local` enables it only in the current repository (`.claude/settings.local.json`); without
it, the plugin loads in every session on the machine. Remove it with
`claude plugin marketplace remove acme-standards`.

### Check it

With the services running, `claude mcp list` shows
`plugin:acme:standards: http://localhost:5480/mcp (HTTP) - ✔ Connected`.

### Editing the skill or agent

Either way, Claude Code loads the plugin in place from this folder: edits take effect at the next
session start or after `/reload-plugins`.

## Write standards

Each owner's standards are Markdown files in `src/OrgStandards.Migrations/seed/<owner>/`:

````markdown
---
title: Web UI                     # the document's name, shown with each topic
version: 2.0
technology: [web-api, ui]         # every other key is a tag: a value or a list.
area: [branding]                  # add a new key and it becomes filterable.
company: acme                     # required: whose standards these are
product: xyz-public-app           # only for product-specific documents
---

## Colors                         <- a topic. The name is what product documents override.
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
- **Rules** use MUST / MUST NOT / SHOULD. Everything else is guidance.
- **Details** (`###`) are free-form Markdown, stored as JSON on the server. Use them for anything
  that doesn't need to be in every answer.
- **Pointers to the environment** name a configuration key, never a host: "connect through
  `Cache:ConnectionString`", not a server address.
- **Links** to resources the owner points to are written `{{name}}` (for example
  `{{design-system}}/css/acme.css`). The owner's server fills in the address from its `Links:name`
  setting, set in the AppHost.
- Text before the first `##`, and headings inside code fences, are ignored.

## Add a standards owner

1. Add the owner's name to `sources` in `src/OrgStandards.AppHost/Program.cs`.
2. Add a folder `src/OrgStandards.Migrations/seed/<name>/` with its Markdown documents.

Aspire gives it a database and a server, pgweb gets a bookmark for it, and the gateway starts
fanning out to it. No change to the gateway, the skill, or the agent.

## Limits

This is a proof of concept. The main limits:

- Tests cover the gateway's rules and the running chain, but not whether Claude uses the skill well;
  that was checked by hand in real sessions.
- Local only: plain HTTP on localhost, no authentication.
- Placeholder standards, filtered in memory.
- A stopped owner is reported after the 10-second call timeout, because Aspire keeps its port open.

The full list, and what a production version would need, is in
[design-notes.md](design-notes.md#what-a-production-version-would-need).

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `docker daemon is not running` / resources stuck in `Starting` | Start the container runtime. With Rancher Desktop, choose the dockerd (moby) engine. |
| `migrations` finished with exit code 1, owners never start | Check its logs in the dashboard. Usually malformed YAML frontmatter in a seed document. |
| Build fails with files locked | The app is still running. `aspire stop`, then build. |
