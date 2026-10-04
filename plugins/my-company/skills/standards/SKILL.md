---
name: standards
description: >-
  Fetch and follow the company's engineering, design and platform standards for the work at hand.
  Use before building or changing anything that has to fit company conventions: a web API, a
  backend service, UI code, or a page, form, mockup or prototype, with or without a named product.
  Also use when the user asks which standards, rules, colors, components or conventions apply.
when_to_use: >-
  "build the orders API for xyz-public-app", "add a settings page", "what colors should I use",
  "which standards apply here", "make this follow our conventions". Not for general programming
  questions that don't touch this company's code.
argument-hint: "[what you're building, e.g. web API for xyz-public-app]"
---

# Work to the company's standards

The standards live on the company's standards server, reached through this plugin's MCP tools:
`list_standards`, `get_standards` and `get_topic`. They're the source of truth. Don't rely on
memory or on what other projects did.

The task, if one was given: $ARGUMENTS

## 1. Work out the scope

If the repository has `.claude/standards.json` (set by the architects), start from its `scope`,
and narrow or extend it for the task. (Older repositories declare the scope in `.claude/CLAUDE.md`
instead.) Otherwise, decide what the work is for:

- **product**: which product this is. Look at the repository name, README and solution/project
  names. If it isn't clear, ask the user. Don't guess a product.
- **kind**: what the code is (for example `api`, `backend`, `css`, `react`). List every kind the
  work touches; values within a field are alternatives, so more kinds bring more standards.
- **concern** (optional): what the work must achieve (for example `security`, `performance`,
  `accessibility`). It narrows: only standards for those concerns come back. Leave it out to get
  every concern for the kind, which is usually right before building; add it for a focused
  question ("the security rules for this API").
- **runtime**: what the code runs on (`dotnet` for C#/.NET, `node` for JavaScript or TypeScript
  servers). Name it whenever the work has one: standards written for a runtime come back only when
  it's named, and standards without one always apply. Tell it from the files (`*.csproj` means
  `dotnet`; a `package.json` with server code means `node`); if a component's runtime isn't clear,
  ask.

**Exclusions** come only from the `exclude` list in `.claude/standards.json`. Each entry names a
field and values (or `topic` and topic names) and a `reason`. Pass them to `get_standards` as the
request's `exclude` (merge the entries: field -> all their values; leave out `reason`), and keep the
reasons for your report. Never add an exclusion of your own, even if the user asks for one while
working: say that exclusions are the architects' decision, made in `.claude/standards.json`.

Call `list_standards` first and use only field names and values it returns. If the product the
user named isn't listed, say so and ask. Don't substitute a close match without confirmation.

## 2. Get the standards

Call `get_standards` once, with one request per component when the task spans several (for
example the API and its UI). Each request has its own `kind` and `concern`, so concerns can differ
per component: `{ "kind": ["api"], "concern": ["security"] }` for a public API and
`{ "kind": ["frontend"], "concern": ["performance"] }` for its page, in the same call. Give every
request the project's `exclude`, if it has one. Then read each document's header before anything
else:

| Status | What to do |
|---|---|
| `complete` | Proceed. |
| `partial` | An owner didn't answer. Tell the user which one, and ask whether to proceed without its standards. |
| `unresolved` | The product didn't match. Confirm the product with the user before relying on the document. |
| `invalid` | The filter was wrong. Fix it using the valid fields listed, and ask again. |

Read "Not covered" too: a gap there means there's no standard for it, not that anything goes. Say
so when your work falls into a gap.

## 3. Follow them

- **MUST / MUST NOT** are requirements. If one can't be met, stop and explain why before going
  further. Never quietly work around it.
- **SHOULD** is the default. You may deviate for a good reason, and you must state the reason.
- A topic that says more is available (examples, reference tables such as color tokens): call
  `get_topic` with that topic name and the same filter when the task needs the detail, for example
  before writing UI code that uses colors. Don't reconstruct the detail from memory.
- Configuration comes from configuration keys the standards name. Never hard-code hosts or
  connection strings.
- If existing code in the repository contradicts a standard, follow the standard in new code and
  point out the existing code. Don't refactor it unless asked.
- **Keep track of the decisions you make yourself**: anything you chose without asking, such as a
  name, a default, a library, an assumption about the product or company, or a change to template
  code to meet a rule. Ask when the choice is the user's to make; otherwise decide, and note it for
  the record (step 6), marking the ones the user should confirm.

## 4. Check the work

When the change is done, hand it to the `standards-reviewer` agent with:

- the exact filter(s) and exclusions you used for `get_standards`, and
- the list of files you created or changed.

The reviewer fetches the standards itself and checks the files independently. Pass on its findings
as they are. If it reports a MUST failure, fix it, or explain to the user why it can't be fixed, and
hand the fixed files back to it. Keep each round's result (failures, fixes, and the final result)
for the record.

## 5. Report

End with a short summary:

- which standards applied (topic names, and the product layer if any),
- the decisions you made yourself, and which need the user's confirmation,
- any SHOULD you didn't follow, and why,
- any gaps ("Not covered") the work touched,
- the reviewer's result,
- where the record is (step 6).

## 6. Record the work

Write a record of what you did with the standards, so it outlives this session and can go into the
pull request with the change. Write it when the work is done (after the last review round), and
skip it for questions that changed no files.

Write a new folder, `docs/standards/implementations/<YYYY-MM-DD-HHmm>/` (local time; create the
folders if needed), and never change an earlier one:

- `implementation.md`, for people:

  ```
  Standards record: <the task, in a line>
  Date · commit before the work (git rev-parse --short HEAD, or "not a git repository") · plugin
  Scope: <per component: kind, runtime, product; from .claude/standards.json or decided, and why>
  Exclusions: <approved ones from .claude/standards.json, or "none">

  Standards applied: <topic (owner, version, layer)>, grouped by component
  Decisions made without asking: <decision: why> [confirm] for the ones the user should confirm
  SHOULD not followed: <topic #n: rule: reason>
  Gaps: <what the work touched that no standard covers>
  Review: <round 1: FAIL (n MUST): topic #n: rule. file:line → fix> ... <final: PASS | FAIL>
  Files: <created or changed>
  Open questions: <for the user or the architects>
  ```

- `implementation.json`: the same, as data:

  ```json
  {
    "run": "2026-10-04-1530", "commit": "a1b2c3d", "plugin": "acme",
    "task": "Orders API with order lookup and a paged order history",
    "scope": [{ "component": "api", "filter": { "kind": ["api", "backend"], "runtime": ["dotnet"] }, "source": "decided" }],
    "exclusions": [],
    "applied": [{ "topic": "Settings", "owner": "platform", "version": "1.1", "layer": "general" }],
    "decisions": [{ "decision": "Named the tracing source Acme.Orders", "why": "Company name taken from the product name", "confirm": true }],
    "shouldNotFollowed": [{ "id": "api/Caching#1", "rule": "cache read-heavy responses", "reason": "The data comes from an in-memory stub for now" }],
    "gaps": ["..."],
    "review": [
      { "round": 1, "result": "fail", "failures": [{ "id": "api/Settings#2", "rule": "no key strings outside Program.cs", "evidence": ["src/Orders.Api/Program.cs:27"], "fix": "read through a typed options class" }] },
      { "round": 2, "result": "pass" }
    ],
    "files": ["src/Orders.Api/Program.cs"],
    "questions": ["..."]
  }
  ```

  Finding ids are the same as in an assessment, `<component>/<topic>#<n>` with the reviewer's rule
  numbers, so a later assessment can be lined up with this record.

Tell the user the folder, and that committing it with the change puts the record in the pull
request.
