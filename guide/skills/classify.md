# The classify skill: `/acme:classify`

Sorts work into the standards' categories. It's one method for a task, a plan, or a whole project
or component, so every part of the plugin that needs a scope gets it the same way. It changes no
file. Source:
[`plugins/my-company/skills/classify/SKILL.md`](../../plugins/my-company/skills/classify/SKILL.md).
The [classifier agent](../../plugins/my-company/agents/classifier.md) runs it without filling the
main conversation.

## How to run it

You rarely run it yourself. The other skills call it. To try it, run `/acme:classify` with a task,
a plan file, or a path: `/acme:classify src/Orders.Api`.

## What it does

1. **Gets the taxonomy.** It calls `get_taxonomy`, which returns the categories the owners file
   standards under, with the file patterns and the package, resource and image names that point to
   each one. It uses only those. If an owner didn't answer, it says which. If none did, it stops.
2. **Reads the declared scope.** That's `.claude/standards.json`: the product, each component's
   path and scope, and the exclusions. Without the file, everything it finds is inferred.
3. **Gathers evidence.** For a task, the words. For a plan, each step's packages, resources and
   file paths. For a project, the project files (`*.csproj`, `package.json`), the Aspire AppHost,
   container images, file extensions and folders. None of this goes to the server.
4. **Matches evidence to categories.** Exact signals first, then file patterns, then descriptions.
   Every inferred value names its evidence. No evidence, no value. The product is never inferred.
5. **Compares.** It lists what the work adds to the declared scope (`add`) and, for a whole
   component, what the scope declares with no evidence in the code (`remove?`). The declared scope
   stays in charge. The skill reports differences and never applies them.

## The result

One JSON block, then a short summary. An example for a plan that adds a cache to an orders API:

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

The `filter` is what to send to `get_standards`. It never holds a `concern`, unless the caller asked
for one, because a concern narrows the answer. `questions` holds whatever the evidence couldn't settle.

## Who uses it

| Caller | Gives it | Uses the result to |
|---|---|---|
| [`/acme:init`](init.md) | A component's path | Fill in kinds, runtimes, dependencies and suggested patterns, and ask about the rest |
| [`/acme:standards`](standards.md) | The task | Compare the task's scope with the project's, and widen the filter when the work adds a dependency |
| [`/acme:assess`](assess.md) | Each component's path | Report scope drift: what the code uses that the scope doesn't say |
| [`/acme:plan-check`](plan-check.md) | The plan | Find which standards the plan's steps touch |
| The [evaluation](../testing-and-evaluation.md#measure-the-classifier) | One golden-set task | Score the classifier against the expected categories |

The taxonomy comes from one owner (the gateway's `Gateway:TaxonomyOwner`), so adding a category or a signal to the standards changes what
every caller sees, with no change to the skill.
