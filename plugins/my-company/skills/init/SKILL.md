---
name: init
description: >-
  Set up a project for the company's standards: scan what it is and does, confirm it through a
  short questionnaire (or accept the scan's answers), and write .claude/standards.json and a
  standards section in .claude/CLAUDE.md. Changes no code.
when_to_use: >-
  "set this project up for our standards", "initialize standards for this repo", "describe this
  project for the standards", or when a project has no .claude/standards.json and the user wants
  one. Not for building (the standards skill) or reviewing a project (the assess skill).
argument-hint: "[auto] to accept the scan's answers without questions"
---

# Set up this project for the company's standards

Describe this project once, so every later task starts from a settled scope instead of working it
out again. You write two files: `.claude/standards.json` and a section of `.claude/CLAUDE.md`. Don't
change any other file.

Arguments: $ARGUMENTS

## 1. Read what's there

- If `.claude/standards.json` exists, its values are the defaults for every question, and its
  approved exclusions are kept unless the user removes them.
- If `.claude/CLAUDE.md` or a root `CLAUDE.md` exists, read it for what the project does.
- Call `list_standards` for the valid products. Categories (kinds, concerns, runtimes,
  dependencies, patterns) come from the classify skill in the next step.

## 2. Scan the project

Read the README, the solution and project files, `package.json` files and the folder layout, and
work out:

- **What it does:** one or two sentences, from the README and the project names.
- **Product:** match the server's products against the repository name and README. No clear match
  means none (the company-wide standards apply). Never pick a close match without asking.
- **Components:** the entry points, the parts that run on their own (a web app, an API, a
  worker). Libraries belong to the component that loads them. Classify each one by following the
  classify skill with the component's path: it fetches the taxonomy and returns, per component,
  the `kind`, `runtime`, `uses` and `pattern` it found, each with its evidence. Use those as the
  scan's answers; suggested patterns and the classify skill's questions go into the questionnaire.

  For example, `api` at `src/Orders.Api` (api, backend, data-access; dotnet; uses postgres and
  kafka; patterns ddd and cqrs) and `web` at `web/` (frontend, react, css).

## 3. Confirm it

**With `auto`:** skip the questions, accept the scan's answers, and mark each one *assumed* in the
summary so the user can check them later.

**Otherwise,** ask with your question tool, a few questions at a time, the scan's answer first and
marked as recommended, and an option to change it:

1. Is this description right?
2. Which product is it, or none (company-wide standards)?
3. Are these the components, with these kinds and runtimes?
4. Does each component use these dependencies and follow these patterns?
5. Are there approved exclusions? Exclusions are the architects' decision: record one only if the
   user says it's approved, with its reason and who approved it. If not, leave the list empty.

Keep `concern` out of the scopes unless the user asks for it, so every concern applies.

## 4. Write the files

Show the user what will be written (and, on a rerun, what changes), and write it after they agree.
In `auto` mode, write it and show what was written.

`.claude/standards.json`:

```json
{
  "$comment": "Set up with the init skill; maintained by the project's architects. Claude reads it and never adds exclusions of its own.",
  "description": "An orders API and the storefront that uses it.",
  "product": [],
  "components": [
    { "name": "api", "path": "src/Orders.Api", "scope": { "kind": ["api", "backend", "data-access"], "runtime": ["dotnet"],
      "uses": ["postgres", "kafka"], "pattern": ["ddd", "cqrs"] } },
    { "name": "web", "path": "web", "scope": { "kind": ["frontend", "react", "css"] } }
  ],
  "exclude": []
}
```

`product` applies to every component; leave it empty for company-wide standards. A project with
one component can use a single `"scope"` instead of `"components"`.

`.claude/CLAUDE.md`: write the section between the markers, creating the file if needed. Replace
only what's between the markers; leave everything else in the file as it is.

```markdown
<!-- standards:start (written by the init skill; run it again to update) -->
## Company standards

This project: <description>. Product: <product, or "none: company-wide standards">.

The company's standards apply to everything built here, including mockups, prototypes, and
throwaway pages made to discuss a design. Before building or changing anything, use the
`standards` skill (or, if it isn't available, the standards tools `list_standards`,
`get_taxonomy`, `get_standards` and `get_topic`), starting from the components and scopes in
`.claude/standards.json`. Use its exclusions exactly as written and never add your own: if a
standard seems not to fit, say so and leave the decision to the architects. The standards server
is the source of truth for colors, components and conventions; don't invent them.
<!-- standards:end -->
```

## 5. Report

Say what was written, which answers were assumed (in `auto` mode) or changed (on a rerun), and
suggest committing both files so the scope is shared. For an existing codebase, suggest the assess
skill as the next step.
