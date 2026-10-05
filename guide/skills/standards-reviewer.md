# The standards reviewer agent: `acme:standards-reviewer`

An independent check of code against the standards, in its own context, so the author doesn't
grade its own work. Source:
[`plugins/my-company/agents/standards-reviewer.md`](../../plugins/my-company/agents/standards-reviewer.md).

## When it runs

- After work done with the [standards skill](standards.md), which hands it the filters,
  exclusions and changed files.
- For each component of an [assessment](assess.md), in parallel.
- When you ask for a review against the standards.

It needs a filter and the files to review. Without either it says so and stops, rather than
guessing. It skips generated and vendored files even when they're given: `bin/`, `obj/`,
`node_modules/`, `dist/`, EF Core migrations and model snapshots, and lock files.

A follow-up round is given the same filters, the previous report and the files changed since. It
checks that the earlier failures are fixed, and checks the changes against the MUST rules in the
report, so a fix that breaks another rule is caught; SHOULDs only where the report already has a
note. It takes the rule numbers and text from the report: no second fetch of the standards and no
re-reading of unchanged files.

Decisions you approved in the [plan check](plan-check.md) are passed in as context. They never
excuse a broken MUST.

## What it does

1. Fetches the standards **itself**, with the given filter and exclusions, instead of trusting the
   implementer's summary.
2. Reads the document header; if the answer isn't `complete`, the review says it's incomplete and
   why.
3. Fetches details when a rule needs them to be checked (exact color tokens, component names).
4. Numbers each topic's rules in the order the document lists them (`Settings #1`), so the same
   rule has the same number on every run. Marks every MUST and MUST NOT **pass**, **fail** (with
   file and line) or **n/a**, and every SHOULD deviation as a **note**, never a failure.
5. Counts a general rule and the runtime rule that `implements` it as **one** failure when the
   evidence is the same.

```
Standards review: kind = api · runtime = dotnet
Status: complete
Result: FAIL (3 MUST failures)

MUST
- [fail] Settings #1 (Settings in .NET): validated at startup. PricingService.cs:22: a missing value falls back to a default
- [pass] Async I/O #1: non-blocking end to end
- [n/a]  Pagination #1: no collection endpoints
...
Excluded by the project's scope
- Colors (concern = branding): not checked, not a pass
```

## Read-only, by design

It can't change files: `Write`, `Edit`, `NotebookEdit`, `Bash` and `PowerShell` are denied. It's a
deny list rather than an allow list because the MCP tool names include the plugin's name, which
changes with the catalog entry. It reports only what the standards say; general code review and
personal preference are out of scope.
