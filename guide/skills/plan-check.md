# The plan check: `/acme:plan-check`

Checks a finished plan against the standards before anyone builds it, and rewrites the steps you
approve. It sits between planning and building. It doesn't build anything or restructure the
plan. Source:
[`plugins/my-company/skills/plan-check/SKILL.md`](../../plugins/my-company/skills/plan-check/SKILL.md).
The checking is done by the read-only
[plan-checker agent](../../plugins/my-company/agents/plan-checker.md).

## How it starts

Normally a hook starts it. You can also ask: *"check this plan against our standards"*, or run
`/acme:plan-check docs/plans/orders-cache.md`.

## The five phases

1. **Trigger.** A small local hook (`hooks/plan_finished.py`) watches for a finished plan: plan
   mode about to end, or a Markdown file written under any folder named `plans` or `specs`. Any
   such file counts as a plan, whichever tool wrote it, because a design spec can hold the plan too
   (as Superpowers' brainstorming sometimes does). Other tools' `specs` folders trigger it as well,
   at most three pauses in a row per file. If the plan hasn't been checked, the hook pauses once
   and asks Claude to run the plan check. It makes no network calls and doesn't judge the plan. Its
   only job is a yes or no. A plan written from a spec that was already checked gets only the steps
   the spec doesn't cover checked.
2. **Classify.** The [classify skill](classify.md) reads the plan and works out which components it
   touches and what each adds: a cache, a new package, a message queue. When the scope is already
   agreed (in `.claude/standards.json`, or earlier in the session), Claude passes it in and the
   agent uses it as it is. It classifies only when the plan adds something outside that scope, and
   then reports just the difference, such as configuration the scope doesn't list.
3. **Fetch, adding as needed.** The agent asks for headlines first (each topic's name and first
   rule), picks the topics each step is about, and fetches only those in full. When more than about
   ten topics apply, as in a new project, it starts by fetching the whole document in one call
   instead. If the plan adds a capability, it also fetches the recipe the plan names or you chose.
   A recipe is an approved option, not a rule: it only binds a plan that has chosen it. Otherwise
   it's offered as a question.
4. **Check locally.** The agent compares each step with the rules it fetched. All of this happens
   on your machine. A step that breaks a MUST in the standards' own rules becomes a proposed
   change. A step that departs from a SHOULD becomes a note. A step that meets every rule is marked
   fine.
5. **Adjust with you.** Claude asks the open questions first (which recipe, whether to record a new
   dependency in the project's scope). Every option it offers meets the MUST rules: a step that
   breaks one is a proposed change, never a question. Then it shows each proposed change as the
   step's text now, the proposed text, and the rules behind it. You apply all, choose some, or
   apply none.

Approved changes replace the step's text in place. The plan keeps its numbering, headings and
checkboxes, and gets no "amendments" section, so whoever builds it reads one correct plan. An
approved scope answer, such as "record redis", is added to the component in
`.claude/standards.json`. Nothing is ever removed from the scope, and no exclusion is ever added.

## Once per version

When the check finishes, it ends the plan with a marker line, a comment that readers don't see:

```
<!-- standards-check: 3fa91c0b7d52 -->
```

The number is a fingerprint of the plan's text, ignoring spacing. The hook stays quiet while the
fingerprint matches, so a checked plan isn't paused again. Edit the plan and the fingerprint
changes, so the next version gets checked. Ticking a step's checkbox (`- [ ]` to `- [x]`) doesn't
change the fingerprint, so tracking progress doesn't start another check.

The hook asks once per version of a plan in a session. As a guard against loops, it also stops
after three pauses in a row for the same plan file (or for plan mode) that didn't end in a checked
plan. A check that finishes starts the count again, so a checked plan that you edit later is
checked again.

## What crosses the network

Only filters and topic names. The plan, your code and the evidence stay on your machine. The
gateway keeps no state between calls, so moving it to a central host changes the address in the
plugin and nothing else.

## When the server is down

If the standards can't be reached, or the agent's report is unusable, the check says so: *"Standards
unavailable: the plan wasn't checked."* It doesn't mark the plan as checked: it only tells the hook
it has stopped working on the file. The hook doesn't ask again for that version in the same
session, and planning carries on. The next session, or the next edit, asks again. If only some
owners answer, the check goes ahead and names the missing ones.

## A call-by-call trace

For one plan, the order is:

1. The hook sees `docs/plans/orders-cache.md` written and blocks once with a request to check it.
2. Claude runs the skill. The skill tells the hook the file is being checked, so edits stay quiet.
3. The plan-checker calls `get_taxonomy`, classifies the plan, and calls `get_standards` with
   `headlines` for the orders API (adding a recipe request, because step 2 adds Redis). Given the
   agreed scope, it starts from that instead, and classifies only what the plan adds beyond it.
4. It calls `get_topic` for the few topics the steps touch (or `get_standards` once, when most
   topics apply).
5. It returns questions, changes and fine steps as JSON.
6. Claude asks you, rewrites the steps you approve, and ends the plan with the marker line: the
   hook stamps a plan file, and in plan mode Claude adds the line the hook prints for the plan.
