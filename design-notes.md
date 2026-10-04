# org-standards-plugin: design notes

Why the plugin is built the way it is: the decisions, the alternatives that were rejected, and what
went wrong while building it. For setup and usage, see [README.md](README.md) and the pages in [guide/](guide/README.md).

**Status: usable, not yet production-hardened.** It began as a proof of concept to test whether the
idea holds up, and was taken to the point where anyone can clone it, run `/setup`, and use it end to
end, with tests. The design is deliberate; the implementation is kept as simple as the design
allows. The last section lists what a production deployment would need.

## The problem

Companies that standardize on Claude Code have standards that vary by product and by kind of work:
how a web API handles timeouts, which components a UI uses, what the public website's footer must
say. Those rules are owned by different teams and change over time. A developer, or Claude working
for one, should get the rules that apply to the task in front of them, and nothing else, without
having to ask for them.

The idea was chosen against five criteria. It had to be:

- **additive**: not something Claude Code already does, or is likely to;
- **deterministic at its core**: the same request gets the same standards;
- **environment-specific**: it carries knowledge Claude can't have on its own;
- **separate from its rules**: standards change without editing the plugin;
- **a company-dictated process**: it automates something a company has decided for itself.

## Principles

These came out of pushing back on earlier versions of the design.

- **Fully local.** Anyone can clone the repository and run it on one machine: .NET, one Postgres
  container, nothing hosted. *Rejected:* a GitHub-hosted plugin marketplace. It would add a
  dependency on an outside server, which is exactly what makes people decline to try something.
- **The gateway is a front door, not a copy.** The MCP server Claude talks to owns no standards.
  It fans out to the servers of the teams that own them. *Rejected:* one central store of copied
  standards, which goes stale and has no clear owner.
- **Owners own their resources too.** The design team's stylesheet lives on the design team's
  site (Acme.Web), not on the gateway. Standards point to it by name (`{{design-system}}`), and the
  owner's server resolves the address. *Rejected:* serving design assets from the gateway. It
  worked, but it put files on the front door that belonged to someone else.
- **Classified by what the work is, not who wrote it.** Every standard carries categories from a
  taxonomy (`seed/taxonomy.yaml`), as many as apply, in two facets: `kind`, what the code is (`api`,
  `css`, `react`, …), and `concern`, what it must achieve (`security`, `performance`,
  `accessibility`, …). Values within a field are alternatives and the two fields narrow each other,
  so a request can say "security, for an API"; a batch of requests gives each component its own
  concerns. Categories come from published sources where one exists (ISO/IEC 25010, OWASP ASVS, W3C
  WCAG, W3C design tokens), and the taxonomy is expected to grow. Owners stay the authors, not a
  category. *Rejected:* a single `categories` field (briefly): in the first end-to-end test, an API
  question that included `security` also returned browser security rules, because one field can
  only widen. Free-form `technology` and `area` tags (an earlier version), which followed the org
  chart and had no defined vocabulary. A fixed product × technology grid, which couldn't express
  combinations like "web API + branding".
- **Code produces facts; the model interprets them.** Filtering, layering, validation and
  "did you mean" are deterministic code in the gateway. Claude reads the result.
- **Every rule must be satisfiable.** A MUST that points at something developers can't get (a
  package that doesn't exist, a service they can't reach) is a broken standard.
- **The repository, not the prompt, says which standards apply.** A developer shouldn't have to
  mention standards for them to be used.

## Architecture

```
Claude Code
  └─ plugin (skill + reviewer agent + MCP config)
       └─ http://localhost:5480/mcp ── gateway (front door: list_standards, get_standards, get_topic)
                                         ├─ design    MCP server ── design database
                                         │     └─ points to Acme.Web (design-system site, :5500)
                                         └─ platform  MCP server ── platform database
```

- **One MCP server per owner.** The same project runs once per owner, with its own database and
  its own Markdown sources. Adding an owner means adding one name in the AppHost and one folder of
  documents.
- **Fan-out:** the gateway calls every owner in parallel, once per batch, and merges the results.
- **Everything runs in .NET Aspire,** which starts Postgres, the migration app, the owners, the
  gateway, Acme.Web and pgweb in the right order. Because Aspire services are long-running, the MCP
  servers use the HTTP transport rather than stdio, and the gateway has a fixed port (5480) that the
  plugin points to.

## Standards as documents

Standards were first modeled as small structured records (key, one-line rule). That was precise
but too terse to write or maintain. They're now **Markdown documents written by the owning team**,
and the output was designed before the storage.

**Writing them:**

- YAML frontmatter holds `title`, `version`, and tags. Every other key is a tag.
- `## Heading` is a **topic**: the unit that filtering, layering and citations work on. Topics are
  named by subject ("Colors"), not by team.
- Rules use **MUST / MUST NOT / SHOULD** (RFC 2119). Everything else is guidance.
- A one-line **"Why:"** and short **examples** are encouraged. The reason lets Claude handle cases the
  rule didn't foresee. Claude follows examples closely, so they must be correct.
- `### Heading` inside a topic is a **detail** (an example, a reference table). Details are left out
  of the main answer and fetched on demand with `get_topic`, which keeps the main answer short.
- Pointers to the environment name a configuration key or a `{{link}}`, never a host.
- The files are the source of truth. The migration app reloads each owner's database from them on
  every start, and details are stored as free-form JSON (jsonb), so authors can add content without
  a schema change.

**What Claude receives** is one Markdown document per request. It opens with a fixed header:

- scope,
- status: `complete`, `partial` (an owner didn't answer), `unresolved` (a product name didn't match),
  or `invalid` (unknown or missing fields),
- which owners answered,
- how precedence was resolved,
- what the keywords mean.

Then come the topics, each with its source and version. A closing "Not covered" section lists
requested values nothing answered, so silence isn't read as permission. The header exists because
the document is instructions, not data: it has to say how far it can be trusted.

## Filters and layering

- A filter is JSON: field → accepted values. OR within a field, AND across fields. Unknown fields
  are rejected with the valid ones listed. Unknown values come back with "did you mean"
  suggestions, and nothing is substituted silently.
- **One deployment per company.** A company runs its own gateway and owners, and the plugin's
  server address decides whose standards it gets. *Rejected:* a required `company` field on every
  topic and request (an earlier version). The plugin only ever serves one company, so callers had
  to repeat a value that could only be one thing, and getting it wrong was one more way to fail;
  the deployment already keeps companies apart.
- **`product` is an overlay.** General topics are the base. A product topic with
  the same name replaces the general one, and says what it replaced. Details are inherited by title,
  so a product that restates a rule keeps the general example unless it provides its own. Without
  a product filter, only general topics apply.
- The same topic defined by two owners in the same layer is returned twice and flagged as a
  conflict, never silently picked.
- `list_standards` is discovery. It shows every field, product and value, and its output becomes
  the next request's filter. Requests are batched: several scopes in one call.

## The plugin

- **No name of its own.** The plugin folder has no `plugin.json`, so whichever marketplace lists it
  names it: `acme` gives `/acme:standards`. The same code serves any company. Loading it directly
  with `--plugin-dir` names it after its folder and installs nothing.
- **The skill** works out the scope, starting from the repository's declared scope. It fetches the
  standards and checks the header before anything else. It follows MUSTs strictly and states any
  SHOULD it departs from. It stops and asks when a MUST can't be met, and hands the result to the
  reviewer.
- **The reviewer** is a separate agent with its own context. It fetches the standards itself instead
  of trusting the implementer's summary, and reports pass, fail or n/a per rule, in a fixed shape,
  citing lines. SHOULDs are notes, never failures. It is read-only through a *deny* list (no Write,
  Edit, NotebookEdit, Bash or PowerShell). An allow list would have to name the MCP tools, whose
  names include the plugin's name, and that would break under a different marketplace name.
- **Projects opt in** by committing `.claude/CLAUDE.md` and `.claude/standards.json` (see
  `project-template/`). CLAUDE.md is always loaded, which is what makes the standards apply to plain
  requests; it points to `standards.json`, which holds the scope and the **exclusions**: standards
  the project may leave out, each with a reason, decided by the architects. Like allowed commands in
  `settings.json`, Claude reads them and never adds its own. The gateway applies them and lists the
  excluded topics in every answer, so an exclusion is never mistaken for a pass. *Rejected:*
  letting Claude exclude rules on request, which would make compliance whatever the conversation
  decided.
- **Runtimes qualify, like products.** `runtime` (`dotnet`, `node`) is a third facet, but it
  doesn't narrow like kind and concern: a standard without one applies to every runtime, and one
  written for a runtime applies only when it's named. General rules are written runtime-neutral,
  with the .NET or Node way as a detail. *Rejected:* tagging .NET rules with a `dotnet` kind, which
  still matched any request for `backend`; found when an assessment of a Node server got .NET rules
  and the reviewer had to judge them "by intent".
- **Topics can narrow their document's tags** with a `<!-- tags: { … } -->` line under the
  heading, so one document can hold topics for different concerns without tagging each with all of
  them.
- **Cost:** about 250 tokens in every session for the standards skill's and the reviewer's
  descriptions (measured). The assess skill's description adds roughly 70 more (estimated, not yet
  measured). The skills' instructions (~1k tokens each) and the reviewer (~570) load only when used.
- **Assessing a whole project** is a second skill, `assess`, rather than a new mode of the reviewer.
  It works out the scope (the declared one, or a proposed one the user confirms), splits the project
  into components, runs the unchanged reviewer on each in parallel, and merges the findings into one
  report that can become a task list. The reviewer stays a single-purpose checker of given files.
  Each run is saved in its own dated folder in the project
  (`docs/standards/assessments/<YYYY-MM-DD-HHmm>/`: `assessment.md` and `.json`), with a stable id
  per finding, so each run compares itself with the last and every run stays side by side. A finding that disappears without a reviewer marking it **pass** is "not reported this
  run", never "fixed": reviewers vary between runs, and only the data can tell variance from
  progress. *Rejected:* keeping the report in the console only, which couldn't be compared; and
  one report overwritten each run, which left the history to git.
- **Implementation work is recorded the same way.** The standards skill writes
  `docs/standards/implementations/<YYYY-MM-DD-HHmm>/` when it's done: the standards applied, the
  **decisions it made without asking** (a guessed company name, a skipped SHOULD, a change to
  template code to meet a rule), each review round, and open questions. Committed with the change,
  the pull request carries its own standards record. Found when a one-prompt build made sensible
  but unasked choices that were only visible in the console.

## What went wrong while building it

1. **Two owners, one server.** Both instances of the owner project inherited the same launch-profile
   ports, so calls meant for one owner reached the other. The conflict check caught it, because
   every rule came back twice. Fix: the AppHost ignores launch profiles for those instances.
2. **Migrations silently applied nothing.** An EF Core version mismatch meant EF couldn't load the
   migration classes and skipped them without an error. The fail-fast startup contained it: the
   migration app exited with an error, and the owners never started on an empty database. Fix: pin
   the EF Core packages to one version.
3. **Products weren't discoverable.** "No product filter, no product rules" was right for fetching
   standards but hid product names from discovery. Fix: discovery lists product topics without
   applying them.
4. **The skill didn't trigger.** In the first real test (a mockup request that didn't mention
   standards), a built-in page-building skill matched the prompt better, and ours never ran. Skill
   descriptions compete, and the MCP tools' own descriptions weren't loaded yet. Fix: the
   repository's CLAUDE.md declares that standards apply and their scope, and the skill's description was widened as
   a backstop. The same prompt then used the standards.
5. **A rule nobody could follow.** A placeholder standard required a React component package that
   doesn't exist. Claude treated the MUST as binding and started building a React prototype to meet
   it, for a static mockup. Fix: a real, local design system (Acme.Web) that every rule can be met
   with, and technology-specific rules tagged so they only reach that technology.
6. **The reviewer wasn't fully read-only.** It used the PowerShell tool to fetch a stylesheet. The
   deny list blocked Bash but not PowerShell, which Claude Code also has on Windows. Fix: deny both.
7. **IDE side effects.** Visual Studio injected a debugging add-in into services that run without a
   launch profile, which logged errors and ran them as Production. Fix: the AppHost sets the
   environment and disables hosting-startup add-ins for those services.
8. **A null crashed the gateway.** A filter like `{"kind": ["api", null]}`, which JSON allows,
   reached the "did you mean" code and threw. Nothing cleaned input at the boundary. Fix: every tool,
   on the gateway and the owners, cleans its input first (nulls and blanks dropped, values trimmed).
9. **An outage blamed the filter.** With every owner timed out, every field was reported as unknown
   ("Valid fields: ."). Fix: fields are checked only against owners that answered, and a document
   with none answering says `unavailable`. Found through an integration test that failed now and
   then: the owners reported healthy before their first query (database connection, EF Core's
   model) could finish within the gateway's 10-second limit. The test fixture now waits until every
   owner answers.

## Demo runs

Two tasks were run end to end, with only the repository's CLAUDE.md telling Claude that standards
apply:

- **A weather-form mockup:** built with the Acme tokens and components. The reviewer passed it, with
  one SHOULD note (a restyled card).
- **"Generation portfolio" (a ticket):** a filterable page of power-generation facilities for the
  public website. The reviewer passed it, with line-level evidence. Both agents listed what the
  standards don't cover, which amounts to a backlog for the design team:
  - how to mark the current section in the navigation,
  - breadcrumbs,
  - filter controls (and whether "labels start with a verb" applies to them),
  - status badges.

In that run the repository declared no product, so the implementer inferred the website product
from the ticket and said so. A website repository should declare it, so nothing is inferred.

## What a production version would need

**Quality**

- **Retrieval is measured, not assumed.** A golden set of developer-worded tasks and an evaluation
  harness score each retrieval strategy (recall, precision, classification), and each report
  records the commit and the standards it measured. The tag-only baseline is recorded so search
  can be judged against it.
- **Broader tests.** Unit tests cover the gateway's rules (matching, overlay, inherited
  details, validation, the document header, the Markdown parser, link resolution), and six
  integration tests run the whole chain through the AppHost. Missing: failure-path integration
  tests (an owner going down mid-request) and tests for the migration app's error handling.
- **Trigger and behavior evaluation.** The skill was tested with a handful of prompts. A real
  rollout needs a repeatable set of should-trigger and shouldn't-trigger prompts, run on every
  change to the skill or its description.
- **Platforms.** Tested on Windows 11 with Rancher Desktop only.

**Security and operations**

- **Identity and access.** Everything is local, plain HTTP and unauthenticated. It's undecided how a
  developer's identity should reach the owners' servers, and what each person may see.
- **Resilience.** Owner calls have a 10-second timeout and no retries. A stopped owner is reported
  only after the timeout.
- **Configuration.** Ports (5480, 5500) are fixed for the demo, and links between services are
  wired in the AppHost.

**The standards model**

- **Filtering and ownership are a larger design problem than this project tackles.** For
  example, finding and routing *missing* standards (an area with no topic under a scope) would need
  a precise definition of which areas are "in scope", a registry of areas with owners, and per-topic
  tags.
- **Per-document tags.** Every topic in a file shares its tags, so a file tagged with two areas
  answers for both. Per-topic tags would fix it.
- **Report wording.** "Not covered" (requested, unanswered) isn't yet separated from "not addressed
  by the standards".
- **Complete reference tables.** The stylesheets define more tokens than the Colors topics list.
- **Authoring and governance.** Standards are Markdown files reloaded on every start. There's no
  review workflow, publishing step or editing interface. pgweb is for viewing only.

**Scale**

- Filtering happens in memory, which suits hand-written standards but not a large catalog.
- Topics shared across batched requests are repeated rather than deduplicated.

**Out of scope by choice**

- **Live data.** Standards are policy (static, versioned). Live telemetry, such as current message
  rates, would need access to production systems.
- **The placeholder standards themselves.** They exist to exercise the format. A real deployment
  starts from a company's actual standards.
