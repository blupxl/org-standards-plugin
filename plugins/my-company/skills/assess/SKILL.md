---
name: assess
description: >-
  Assess an existing project against the company's standards and report where it doesn't comply,
  rule by rule with file and line, as a starting point for a task list. Saves each run in a dated
  folder under docs/standards/assessments/ so runs can be compared; changes no code.
when_to_use: >-
  "assess this project against our standards", "what in this repo doesn't meet the standards",
  "give me a standards report for this codebase", "audit this project". Not for checking a change
  you just made (the standards skill hands that to the reviewer itself).
argument-hint: "[optional: part of the project to assess, e.g. src/Api]"
---

# Assess this project against the company's standards

The user wants a report on how this project compares with the standards its architects assigned
to it. You report; you don't fix. The only files you write are this run's report files (step 5);
don't change any other file unless the user asks afterwards.

Scope of the assessment, if given: $ARGUMENTS

## 1. Work out the scope

- If the repository has `.claude/standards.json` (written by the init skill, kept by the
  architects), use it and say so. That's the assignment the user means. Its `components` are the
  components to review, each with its own `scope` plus the file's `product`; a single `scope`
  applies to the whole repository. Use its `exclude` list too. (Older repositories declare the
  scope in `.claude/CLAUDE.md`.)
  For each component, also run the `classifier` agent with the component's path, in parallel. Its
  `differences` are **scope drift**: what the code shows that the declared scope doesn't (a Redis
  package in a component that doesn't list `redis`), and declared values with no evidence. Keep
  using the declared scope for the review; report the drift (step 4).
  If the classifier's status is `unavailable`, the Scope drift line reads "not checked (standards
  unavailable)".
- Otherwise, look at what the project contains and propose a scope: its components (for example
  an API, a web front end, a stylesheet folder) and a `kind` for each (`api`, `backend`, `frontend`,
  `react`, `css`, `sass`, …), its `runtime` (`dotnet` for C#/.NET, `node` for JavaScript or
  TypeScript servers), what it `uses` (`postgres`, `kafka`, …, from its packages and
  configuration) and the `pattern`s it follows (`ddd`, `cqrs`, `event-sourcing`; ask, don't
  guess). Standards written for a runtime, dependency or pattern come back only when it's named. Call
  `list_standards` and use only fields and values it returns. Ask the user which product this is
  if it isn't clear, and confirm the proposed scope before going on. Don't guess a product.
- Leave `concern` out unless the user asked about specific concerns, so every concern applies.
- Exclusions come from `.claude/standards.json`. Never add one of your own. If the user asks to
  leave something out of this assessment, do it, but report it separately as **requested in this
  session, not approved**, and suggest recording it in `.claude/standards.json`. If the findings
  suggest a standard doesn't fit this project, say so in the report as a question for the
  architects.

## 2. Pick the files for each component

For each component, choose the files the standards could apply to: source files of that kind,
their configuration (`Program.cs`, `appsettings*.json`, `*.csproj`, `package.json`,
`nuget.config`), stylesheets, and markup. Skip generated and vendored files (`bin/`, `obj/`,
`node_modules/`, `dist/`, migrations designer files).

A component with more than about 40 such files: prefer entry points, configuration, and a
representative sample of each folder, and record how many files were reviewed out of how many, so
the report can say how complete it is.

## 3. Review each component

Hand each component to the `standards-reviewer` agent, with its own filter, the project's
exclusions, and its own files. Components are independent, so run the reviewers in parallel. Each
fetches the standards itself and reports pass, fail or n/a per rule with file and line.

## 4. Report

Combine the reviewers' findings. Don't add findings of your own: every finding comes from a
reviewer, with its rule and evidence. A general rule and the runtime rule that `implements` it count
as one failure when they fail for the same evidence.

Group the MUST failures by **root cause**: one decision or omission that breaks several rules at
once (for example "the UI doesn't use the design system" breaks Colors, Components and Spacing).
Under each cause, list every rule it breaks with its file:line evidence, so nothing is lost by the
grouping. Order the causes by how many failures they explain.

Then separate what the user can fix from what someone has to decide:

- **Decisions:** a cause that reflects a deliberate choice of the project (its own look, no login
  on a demo). These go to the architects: comply, or add an exclusion to `.claude/standards.json`.
- **Defects:** failures that are mistakes whatever the scope (a double submit, raw exception text,
  failing contrast). These can be fixed now.

```
Standards assessment: <project>
Scope: <per component: kind, runtime, product; declared or as agreed>
Exclusions: <approved, from .claude/standards.json, each with its reason; then any requested in
            this session, marked "not approved"; or "none">
Coverage: <files reviewed of files found, per component>
Status: <complete | incomplete: reason, from the document headers>
Result: <n> MUST failures from <n> causes · <n> SHOULD deviations

1. <Root cause> (<n> failures) [decision | defect]
   - <Topic> (<owner>): <rule>
     - <file>:<line>: <what's wrong>
2. ...

SHOULD deviations
- <Topic> (<owner>): <rule>. <file>:<line>: <deviation>

Passed: <rules that passed, by topic>
Excluded by scope: <topics, with the reason from .claude/standards.json; not checked, not passes>
Not covered: <areas the project touches that no standard covers>
Scope drift: <per component, from the classifier: "uses redis (StackExchange.Redis in
             Orders.Api.csproj), not declared"; or "none">
Questions for the architects: <standards that seem not to fit this project, if any>
```

Scope drift goes to the architects: record it in .claude/standards.json, or remove what the code shouldn't use.

## 5. Compare with the last run, and save the report

Every run is kept: each gets its own dated folder, and nothing earlier is overwritten, so the runs
can be compared over time.

1. **Find the last run:** the folders in `docs/standards/assessments/` are named
   `YYYY-MM-DD-HHmm`, so the last one by name is the most recent. Read its `assessment.json`, if
   there is one, before writing anything.
2. **Give every finding a stable id:** `<component>/<topic>#<n>` (for example `api/Settings#1`),
   where `<n>` is the rule's number within its topic as the reviewer cites it (`Settings #1`): the
   rules' order in the document, so the same rule gets the same id on every run. When a runtime
   topic implements a general one, use the general topic and the number of the general rule it
   implements; a runtime rule with no general counterpart keeps its own topic. Excluded topics get
   `<component>/<topic>`. Never invent words for ids, and never use line numbers; both change between
   runs. Keep a short readable `rule` text beside the id.
   If the last run's ids don't follow this scheme, say that its findings can't be matched one by
   one, compare the counts only, and treat this run as the new baseline.
3. **Compare by id** and add a **Changes since the last run** section to the report (after
   *Result*), with counts and each finding listed:
   - **New:** failing now, not failing last time.
   - **Fixed:** failing last time, and a reviewer marked it **pass** now.
   - **Still failing:** in both.
   - **Not reported this run:** failing last time, not mentioned now. Reviewers vary between
     runs; never count these as fixed. List them so the user can check.
   - Also say what changed in scope or exclusions, since that changes what was checked.
4. **Write this run's folder**, `docs/standards/assessments/<YYYY-MM-DD-HHmm>/` (local time; create
   the folders if needed). Never change or delete an earlier run's folder.
   - `assessment.md`: the report as shown in step 4, starting with the date and time, the project's
     commit (`git rev-parse --short HEAD`, and whether there are uncommitted changes), the plugin's
     name, and which earlier run it was compared with.
   - `assessment.json`:

     ```json
     {
       "run": "2026-10-04-1430", "comparedWith": "2026-10-03-0915",
       "commit": "a1b2c3d", "uncommittedChanges": false, "plugin": "acme",
       "scope": [{ "component": "api", "filter": { "kind": ["api"], "runtime": ["dotnet"] }, "files": "27 of 27" }],
       "exclusions": { "approved": [{ "concern": ["branding"], "reason": "..." }], "session": [{ "topic": ["Colors"] }] },
       "status": "complete",
       "summary": { "mustFailures": 27, "causes": 12, "shouldDeviations": 1, "unverified": 4 },
       "findings": [{
         "id": "api/Settings#1", "component": "api", "topic": "Settings", "topicVersion": "1.1",
         "rule": "a missing or invalid setting stops startup", "implementedBy": "Settings in .NET",
         "owner": "platform", "severity": "MUST", "result": "fail", "type": "defect",
         "cause": "Settings aren't typed or checked at startup",
         "evidence": ["src/Orders.Api/PricingService.cs:22", "src/Orders.Api/Program.cs:31"]
       }],
       "notCovered": ["..."], "questions": ["..."]
     }
     ```

     `result` is `fail`, `pass`, `unverified` or `excluded`; `type` is `defect` or `decision` for
     failures; `severity` is `MUST` or `SHOULD`. Include passes, so a later run can tell "fixed"
     from "not reported".
5. **Tell the user** where this run's folder is, which run it was compared with, and the
   headline of the comparison (for example "13 MUST failures, down from 19: 3 fewer from
   de-duplication, 2 fixed, 1 not reported this run").

## 6. Offer next steps

Ask whether the user wants the MUST failures turned into tasks, in Claude Code's own task list:
one task per **defect** cause, naming the rules and files, and one task per **decision** cause,
addressed to the architects. Fix nothing unless asked; when asked, fix with the `standards` skill,
so the reviewer checks the fix.
