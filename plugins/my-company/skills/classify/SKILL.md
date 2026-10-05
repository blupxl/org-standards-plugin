---
name: classify
description: >-
  Classify work into the company's standards categories: a task, a plan, or a project or one of its
  components. Returns the declared scope, the inferred scope with evidence, the differences, and the
  get_standards filter to use, as JSON. Used by the plugin's other skills and agents.
when_to_use: >-
  Before fetching standards when the scope isn't settled; when a plan or a change may add a
  dependency, runtime or pattern; when checking that .claude/standards.json still matches the code.
argument-hint: "[a task, a plan file, or a project or component path]"
---

# Classify work into the company's standards categories

What to classify: $ARGUMENTS (a task sentence, a plan file, or a project or component path).

You produce facts with evidence. You never change `.claude/standards.json`; you report what differs
from it.

## 1. Get the taxonomy

Call `get_taxonomy`. Use only its categories, facets, file patterns and signals; never a category
or signal from memory. If its status is `partial`, name the owners that didn't answer. If it's
`unavailable`, stop and return `"status": "unavailable"` with no components.

## 2. Read the declared scope

Read `.claude/standards.json` if it exists: the `product`, each component's `name`, `path` and
`scope`, and the `exclude` list. A file with a single `scope` is one component covering the whole
repository. Without the file, every component is undeclared and everything you return is inferred.

For a task or a plan, find the components it touches: by the paths it names, or by what it
describes. If that isn't clear, add a question and classify the most likely one.

## 3. Gather evidence

- **A task:** its words.
- **A plan:** each step: the packages, resources, images and technologies it names, and the files it
  creates or changes (their paths and extensions).
- **A project or component:** project files (`*.csproj` package references, `package.json`
  dependencies), the Aspire AppHost (`AddPostgres(`, `AddRedis(`, …), container images in compose
  files, file extensions and folder layout. Read configuration and project files, not every source
  file.

Never send any of this to the server. Only filters and topic names go there.

## 4. Match evidence to categories

In this order:
1. **Signals:** an exact package, resource call or image named in a category's `signals`.
2. **File patterns:** a path matching a category's `files`.
3. **Descriptions:** only when the evidence plainly says what a description says (a plan step "cache
   customer lookups" is `caching`).

Every inferred value needs evidence: where you saw it, in a few words. No evidence, no value.

- `kind`, `runtime`, `uses`, `pattern`: infer from evidence. Patterns are suggestions: also add a
  question to confirm one that isn't declared.
- `concern`: only when the work is plainly about it (a step about caching, a task about security).
- `product`: never inferred. Take it from `.claude/standards.json` or the user; otherwise `null`,
  and a question if the work seems product-specific.

## 5. Compare

For each component, list the **differences** between declared and inferred:
- `"change": "add"`: inferred but not declared (the plan adds Redis).
- `"change": "remove?"`: declared `runtime`, `uses` or `pattern` with no evidence, only when you
  classified the whole component (a project or component path), never for a task or plan.

The declared scope stays authoritative. Report differences; don't apply them.

## 6. Return

Reply with one fenced `json` block in exactly this shape, then at most three sentences of summary:

```json
{
  "status": "ok",
  "product": null,
  "components": [
    {
      "name": "orders-api",
      "path": "src/Orders.Api",
      "declared": { "kind": ["api", "backend"], "runtime": ["dotnet"], "uses": ["postgres", "ef-core"] },
      "inferred": { "kind": ["api", "backend"], "runtime": ["dotnet"], "uses": ["postgres", "ef-core", "redis"], "concern": ["caching"] },
      "evidence": { "redis": "plan step 2: StackExchange.Redis", "caching": "plan step 2: cache customer lookups" },
      "differences": [ { "field": "uses", "value": "redis", "change": "add", "evidence": "plan step 2: StackExchange.Redis" } ],
      "filter": { "kind": ["api", "backend"], "runtime": ["dotnet"], "uses": ["postgres", "ef-core", "redis"] }
    }
  ],
  "questions": []
}
```

- `filter` is what to send to `get_standards` for this work: the declared scope, plus any `kind`,
  `runtime`, `uses` or `pattern` value the evidence adds (so its standards come back), plus
  `product` when there is one. Never `concern` (it narrows) unless the caller asked for one.
- `questions`: everything the evidence couldn't decide. Never guess instead.
