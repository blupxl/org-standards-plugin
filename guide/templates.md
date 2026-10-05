# Templates and recipes

Standards say what work must do. **Recipes** say how to start it: the approved way, decided by the
architects, to add a capability such as data access to a service. They let a new project begin
inside the standards instead of being corrected into them later.

## What a recipe is

A recipe is a standards document an owner writes, marked `template: recipe`, with one topic per
option. For data access on PostgreSQL, the data team approves two:

| Recipe | For | Adds | |
|---|---|---|---|
| *Recipe: EF Core with PostgreSQL* | `uses: postgres` | `ef-core` | recommended by the owners |
| *Recipe: Dapper with PostgreSQL* | `uses: postgres` | `dapper` | approved alternative |

Each recipe topic says what to do (MUST rules), why, and which standards following it meets.
Its details carry what the implementing agent needs: the packages per project, the wiring
(AppHost and `Program.cs`), the configuration keys, and how migrations run.

Recipes never come back with the ordinary standards. They're returned only when a request asks for
them, so they don't add noise to everyday work:

```jsonc
get_standards({ "requests": [{ "filter": {
  "template": ["recipe"], "kind": ["data-access"], "runtime": ["dotnet"], "uses": ["postgres"] } }] })
// → Recipe: EF Core with PostgreSQL · recommended by the owners · adds: ef-core
//   Recipe: Dapper with PostgreSQL · adds: dapper
```

## How it's used

Nobody has to invoke anything. When the [standards skill](skills/standards.md) sees that the work
starts a project or adds a capability, it:

1. **asks what the work needs**: "Will it read or write a database?" → yes → "Which one?" →
   PostgreSQL;
2. **lists the approved recipes**, the recommended one first: "EF Core (recommended) or Dapper?";
3. **follows the chosen recipe**, fetching its packages, wiring and example with `get_topic`;
4. **records the decision** in `.claude/standards.json`, adding the recipe's `adds` to the
   component's `uses` (`["postgres", "ef-core"]`), so the rules for that choice (the *EF Core*
   standards, not Dapper's) apply from then on.

## Every agent sees the decisions

The gateway sends **server instructions**, which Claude Code adds to every session's context, so
they reach every agent, including planning and brainstorming skills from other plugins:

> Company standards and approved recipes. Before planning or building in a project, read
> `.claude/standards.json` if it exists and call `get_standards` for the components the work
> touches. For a new project or a new capability, list the approved recipes …, prefer the one
> marked recommended unless the user picks another, fetch it with `get_topic`, and record the
> choice in `.claude/standards.json`.

The recorded choices live in `.claude/standards.json`, a plain file any agent or person can read.
A plan written by any planning workflow starts from the architects' decisions instead of
rediscovering them.

## Write a recipe

````markdown
---
title: Data access recipes
version: 1.0
template: recipe                  # makes every topic here a recipe
runtime: dotnet
kind: [data-access, backend]
---

## Recipe: EF Core with PostgreSQL
<!-- tags: { uses: [postgres], adds: [ef-core], recommended: [yes] } -->
What it is, and the standards following it meets.
- MUST rules for following it.

Why: one line.

### Packages                      <- details: fetched by the implementing agent
### Wiring
### Migrations
````

- **`uses`** says which projects the recipe is for; **`adds`** says what following it adds to the
  component's `uses`. Both come from the [taxonomy](writing-standards.md#the-taxonomy).
- Mark one option per question **recommended**. Anything not written as a recipe isn't approved.
- Recipes in the demo: [`seed/data/recipes.md`](../src/OrgStandards.Migrations/seed/data/recipes.md).
