# The init skill: `/acme:init`

Describes a project once, so every later task starts from a settled scope instead of working it
out again. It scans the project, confirms what it found with you, and writes two files. It changes
no code. Source: [`plugins/my-company/skills/init/SKILL.md`](../../plugins/my-company/skills/init/SKILL.md).

## How to run it

In the project, run `/acme:init`, or ask *"set this project up for our standards"*. To accept the
scan's answers without questions, run `/acme:init auto`; every answer it assumed is listed so you
can check it later.

## What it does

1. **Reads what's there.** An existing `.claude/standards.json` provides the defaults, and its
   approved exclusions are kept. It asks the standards server which kinds, runtimes, dependencies,
   patterns and products are valid.
2. **Scans the project:**
   - **what it does**, from the README and project names;
   - the **product**, by matching the server's products against the repository; no clear match
     means company-wide standards, and it never picks a close match without asking;
   - its **components**: the entry points (a web app, an API, a worker), each with its path,
     kinds and runtime (`*.csproj` means `dotnet`; a `package.json` with server code means `node`);
     libraries belong to the component that loads them;
   - what each component **uses**, from facts: packages (`Npgsql` → `postgres`,
     `Confluent.Kafka` → `kafka`, `Eventuous.*` → `eventuous`, …), Aspire resources and container
     images;
   - the **patterns** it seems to follow (`ddd`, `cqrs`, `event-sourcing`), as suggestions you
     confirm.
3. **Asks you to confirm**, a few questions at a time, with the scan's answer first: the
   description, the product, the components, what each uses and follows, and any approved
   exclusions. Exclusions are the
   architects' decision, so it records one only when you say it's approved, with the reason.
4. **Writes the files**, after showing you what will be written (and, on a rerun, what changes):
   - [`.claude/standards.json`](../project-setup.md#standardsjson): the description, product,
     components with their scopes, and approved exclusions;
   - a section of `.claude/CLAUDE.md` between `<!-- standards:start -->` and
     `<!-- standards:end -->`, saying what the project is and pointing Claude at
     `standards.json`. A rerun replaces only that section; the rest of the file stays as it is.
5. **Reports** what it wrote and what it assumed, and suggests committing both files and running
   the [assess skill](assess.md) next on an existing codebase.

## Why per component

One repository can hold a .NET API and a JavaScript front end. One merged scope would give the
API the front-end rules and the front end the .NET ones. With a scope per component, the
[standards skill](standards.md) and the [assess skill](assess.md) give each part only its own
rules.
