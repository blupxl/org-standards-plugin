# Testing and evaluation

## Run the tests

```bash
dotnet test src/OrgStandards.Tests                                  # everything
dotnet test src/OrgStandards.Tests --filter "Category!=Integration"  # unit tests only, no Docker
python -m unittest discover -s tests/hooks -v                        # the plan hook's tests
```

- **Unit tests** (114) check the rules the gateway applies: which topics a filter selects, the
  product overlay and inherited details, the qualifiers (runtimes, dependencies, patterns),
  exclusions, "did you mean" suggestions, input cleaning, the document header, the Markdown parser,
  and link resolution. They also check the seed
  data: every category used is in the taxonomy, every taxonomy category is used, every `implements`
  link names a real topic, and the golden set points at standards that exist. They need nothing
  running and take well under a second.
- **Integration tests** (8) start the whole AppHost inside the test run, with Postgres in a
  container and randomized ports, so they don't clash with a running copy. They call the gateway
  over MCP, the way Claude Code does, and wait until every owner answers before the first test.
  They need a container runtime and are skipped, not failed, without one. The first run pulls the
  Postgres image. Run them without `--artifacts-path`: the test run starts each service from its
  own `bin/` folder.
- **Hook tests** (28) check the plan hook with Python's `unittest`: which files and events count as
  a finished plan, the marker line, the once-per-version rule, Windows paths and line endings, and
  that any error means silence. They need only Python.

## Measure retrieval

How well do the tools find the right standards for a task? The evaluation answers that, so a change
to retrieval can be shown to help or hurt:

```bash
dotnet run --project src/OrgStandards.Evaluation   # writes evaluation/results/<date>-*.md and .json
```

It runs each retrieval strategy over the **golden set**
([`seed/evals/golden.yaml`](../src/OrgStandards.Migrations/seed/evals/golden.yaml)): tasks written
the way a developer would put them, each with the topics that should be found, the categories it
should be classified with, and topics that must **not** come back. It reports recall, precision,
forbidden topics returned and classification, per task and overall. It runs in process on the seed
files: no services, no model, the same result every time. Each report records the commit and a
fingerprint of the standards and golden set it measured.

The reports are written to `evaluation/results/`, which isn't committed: they're outputs, and any
of them can be reproduced by checking out the commit it records and running the evaluation again.

**What it has shown so far**, on the placeholder standards:

| Change | Mean recall | Precision |
|---|---|---|
| Baseline: tag-only matching, one `categories` field | 6% (14% accepting "did you mean") | 9% (6%) |
| `categories` split into `kind` and `concern` | 10% (16%) | 19% (10%) |

Tag-only matching finds very little: a task has to name its category to be found. That's the gap
search is meant to close, and the number to beat. Splitting the facets doubled precision: an API
question stopped getting browser security rules.

## Measure the classifier

The [classify skill](skills/classify.md) can be scored on the same golden set:

```bash
dotnet run --project src/OrgStandards.Evaluation -- --strategy classifier --plugin-dir ../plugins/my-company
```

Unlike the default strategies, this one needs the services running, because the classifier calls
`get_taxonomy`. It also needs Claude Code, and it makes one model call per task, so it costs money
and the result can vary between runs. That's why it runs only when asked, never by default.

The harness starts `claude` as an executable on the PATH, so it needs the native install; an npm
install's `claude.cmd` isn't found. It runs each call in an empty temporary folder, so no project's
`CLAUDE.md` or settings change the result. It allows only the plugin's taxonomy tools, named after
the plugin folder (`mcp__plugin_my-company_standards__get_taxonomy` for `plugins/my-company`), so a
renamed folder needs no change.

## Assessments are measured too

Each run of [`assess`](skills/assess.md) saves its findings, with stable ids, in a dated folder in
the assessed project, and compares itself with the previous run. That's how a decision (an
exclusion, a reworded rule, a fix) shows up as a change you can see.
