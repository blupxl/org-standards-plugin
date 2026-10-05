# The assessment skill: `/acme:assess`

Checks an existing project against its standards and reports where it doesn't comply, rule by rule
with file and line, ready to become a task list. It changes no code. Source:
[`plugins/my-company/skills/assess/SKILL.md`](../../plugins/my-company/skills/assess/SKILL.md).

## How to run it

Open the project in Claude Code and run `/acme:assess`, or just ask: *"assess this project against
our standards and give me a report"*. To assess part of it: `/acme:assess src/Api`.

## What it does

1. **Scope.** Uses the scope and exclusions in the project's `.claude/standards.json`. Without one,
   it proposes the project's components with their kinds and runtimes (a .NET API, a Node server, a
   front end, stylesheets), and asks you to confirm them and the product. With a declared scope,
   the [classifier agent](../../plugins/my-company/agents/classifier.md) also checks each component
   for **scope drift**: what the code uses that the scope doesn't list, and declared values with no
   evidence. The review still uses the declared scope; the drift is reported.
2. **Review.** Each component goes to the [standards reviewer](standards-reviewer.md) with its own
   filter, in parallel. Large components are sampled, and the report says how many files were
   reviewed.
3. **Report.** MUST failures **grouped by root cause** (one omission that breaks several rules), each
   cause marked:
   - **defect**: a mistake whatever the scope; fix it now;
   - **decision**: a deliberate choice of the project; the architects comply, or add an exclusion.

   Every rule and file:line stays under its cause. Then SHOULD deviations, MUST rules that couldn't
   be verified, what passed, what was excluded and why, what no standard covers, and questions for
   the architects.
4. **Saved and compared.** Every run gets its own dated folder in the project:

   ```
   docs/standards/assessments/
     2026-10-04-1430/   assessment.md · assessment.json
     2026-10-05-0915/   assessment.md · assessment.json   ← compared with 2026-10-04-1430
   ```

   `assessment.json` holds every finding, passes included, with a stable id such as `api/Settings#1`
   (the component, the topic, and the rule's number within the topic as the standards list it:
   never line numbers or made-up words, which change between runs). Each run compares itself with
   the previous one: **new**, **fixed** (only when a reviewer marks the rule pass), **still
   failing**, and **not reported this run** (reviewers vary between runs; silence is never counted
   as a fix).
5. **Next steps.** Offers to turn the causes into tasks in Claude Code's task list: defects to fix,
   decisions for the architects. Fixing is a separate request, made with the
   [standards skill](standards.md) so the reviewer checks each fix.

## Exclusions in an assessment

Approved exclusions come from `.claude/standards.json`. If you ask it to leave something out for
this run, it does, but reports it separately as *requested in this session, not approved*, and
suggests recording it so the next run doesn't depend on someone remembering.

## A sample of the output

An illustrative example, for a made-up project: a .NET orders API with a React storefront.

```
Result: 14 MUST failures from 6 root causes (5 defects, 1 decision) · 2 SHOULD deviations ·
        1 MUST rule unverified

1. The API skips the shared service defaults (4 failures) [defect]
   - Tracing #1 (Service defaults in .NET): no OpenTelemetry is set up (src/Orders.Api/Program.cs).
   - Health endpoints #1: only /health is mapped (Program.cs:52); there's no /alive.
   - Timeouts #1: the payment client keeps HttpClient's 100 s default (Program.cs:31).
   ...
6. A chat widget on the checkout page (1 failure) [decision]
```
