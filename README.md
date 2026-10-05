# org-standards-plugin

**Work that meets your company's standards the first time, so reviews can focus on the business
logic.**

Developers, and the AI assistants working with them, rarely know every standard that applies to the
task in front of them: the product's timeout rules, the brand's colors, the website's footer, the
accessibility basics. Those rules live with different teams and differ by product, so they tend to
be caught late in review, and the work goes around again.

This Claude Code plugin brings those standards into the conversation at the moment the work
happens, has an independent agent check the result, and can assess a whole existing project. A
developer gets:

- **the standards for this task, and only those,** with product- and runtime-specific rules already
  applied;
- **an approved way to start new work**: recipes from the architects (data access with EF Core
  or Dapper, …), chosen through a few questions and recorded for every agent to see;
- **a check of the finished work** against each required rule, citing file and line, and a record
  of what was applied and decided, saved with the change;
- **an assessment of an existing project**, grouped by root cause into defects to fix and decisions
  for the architects, saved so each run can be compared with the last;
- **a clear statement of what the standards don't cover,** instead of a confident guess.

Who this helps, and how: [use-cases.md](use-cases.md).

## What this demonstrates

- **A deterministic core with the model around it.** Filtering, precedence, exclusions and
  validation are code; Claude interprets the result.
- **Verification separate from generation.** A read-only reviewer agent checks the work, so the
  author doesn't grade its own output.
- **Classification from published standards.** A taxonomy of what the code is (`api`, `css`,
  `react`) and what it must achieve (`security`, `accessibility`), sourced from ISO/IEC 25010, OWASP
  ASVS and W3C WCAG, plus what it runs on (`dotnet`, `node`), depends on (`postgres`, `kafka`) and
  follows (`ddd`, `event-sourcing`), so rules reach only the code they fit.
- **The repository, not the prompt, decides.** A project declares its scope and its approved
  exclusions, so an ordinary request is enough, and Claude never excuses itself from a rule.
- **Measured, not assumed.** A golden set scores retrieval; assessments keep every run for
  comparison.
- **Context kept small.** A few hundred tokens per session until a skill is used; details are
  fetched only when a task needs them.
- **Built, tested and corrected.** 170 unit tests, 13 integration tests and 31 hook tests, and the
  failures found along the way, with how each was fixed.
- **Fully local, and debuggable.** One clone, one command (`/setup`), nothing hosted; run it from
  your IDE and step through a real request.

The reasoning behind each decision is in [design-notes.md](design-notes.md).

> **Status: usable, not yet production-hardened.** What started as a proof of concept now works
> end to end: clone it, run `/setup`, and it builds, passes its unit and integration tests, and
> connects Claude Code to the services, which you run from your IDE so you can step through them.
> It ships with example standards that show the format. It has no authentication and filters in
> memory; what a production deployment would need is listed in
> [design-notes.md](design-notes.md#what-a-production-version-would-need), and what we measured
> and plan next in [Known gaps and next revision](design-notes.md#known-gaps-and-next-revision).

## Quick start

1. **Clone** the repository and start your container runtime (Docker Desktop, Podman, or Rancher
   Desktop with dockerd).
2. **Open Claude Code** in the repository's root folder, accept the trust prompt, and run
   `/setup`. It checks the requirements, builds, runs the tests, and asks before installing
   anything.
3. **Start the services** when it asks: open `src/OrgStandards.slnx` in Visual Studio or Rider and
   press F5 (or `dotnet run --project src/OrgStandards.AppHost --launch-profile http`).
4. **Connect** the plugin for one session, or install it for all your projects. Everything stays on
   your machine.

Then try: *"Which standards apply to a web API for xyz-public-app?"* In another repository, run
`/acme:init` to describe it once, then `/acme:assess` to see how it measures up.

## Documentation

Everything below is also indexed in [guide/](guide/README.md).

| Read | For |
|---|---|
| [Getting started](guide/getting-started.md) | Requirements, running the services (and debugging them), trying it, troubleshooting |
| [Install the plugin](guide/install.md) | The local catalog, installing for all projects or one, update, rename, uninstall |
| [Set up a project](guide/project-setup.md) | `.claude/standards.json`: a project's scope and approved exclusions |
| [How it works](guide/how-it-works.md) | The architecture, the four MCP tools, the document Claude reads, filters and fields |
| [Write standards](guide/writing-standards.md) | The Markdown format, the taxonomy, runtimes, adding an owner |
| [Templates and recipes](guide/templates.md) | Approved ways to start new work (data access with EF Core or Dapper, …), and how every agent sees the decisions |
| [Testing and evaluation](guide/testing-and-evaluation.md) | The tests, the golden set, retrieval results |

**Skills and agents** ([all](guide/skills/README.md)):

| | |
|---|---|
| [`/acme:init`](guide/skills/init.md) | Describes a project once: scans it, confirms it with you, and writes its standards settings |
| [`/acme:standards`](guide/skills/standards.md) | Brings the standards for a task into the conversation, and has the work checked |
| [`/acme:assess`](guide/skills/assess.md) | Assesses an existing project; saves each run for comparison |
| [`/acme:classify`](guide/skills/classify.md) | Classifies work into the standards' categories |
| [`/acme:plan-check`](guide/skills/plan-check.md) | Checks a finished plan before it's built |
| [`acme:standards-reviewer`](guide/skills/standards-reviewer.md) | Read-only agent that checks code against the standards |
| `acme:classifier` | Read-only agent that classifies work and returns JSON |
| `acme:plan-checker` | Read-only agent that checks a plan against the standards |
| [`/setup`](guide/skills/setup.md) | Sets up, updates, renames or uninstalls; a project skill of this repository |

"Acme" is a stand-in: the plugin is generic (`plugins/my-company`), and a one-line local catalog
entry gives it its name ([how](guide/install.md#what-the-marketplace-is-here)).

## Limits

It's usable, but not hardened for production. The main limits:

- Tests cover the gateway's rules and the running chain, but not whether Claude uses the skills
  well; that was checked by hand in real sessions.
- Local only: plain HTTP on localhost, no authentication.
- Placeholder standards, filtered in memory; retrieval matches categories, not meaning (search is
  the next step, measured against the recorded baseline).
- A stopped owner is reported after the 10-second call timeout, because Aspire keeps its port open.

The full list, and what a production version would need, is in
[design-notes.md](design-notes.md#what-a-production-version-would-need).

## License

[MIT](LICENSE).
