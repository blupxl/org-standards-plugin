---
name: plan-check
description: >-
  Check an implementation plan against the company's standards before implementing it: ask the
  open questions, show the proposed changes, and rewrite the steps the user approves. Use when a
  plan is finished (the plugin's hook asks for it) or when asked to check a plan against the
  standards.
when_to_use: >-
  A hook message saying a plan hasn't been checked against the standards; "check this plan against
  our standards"; before executing a plan from Superpowers, plan mode or any other planning flow.
argument-hint: "[plan file]"
---

# Check a plan against the company's standards

The plan: $ARGUMENTS (a file path; in plan mode, the plan you're about to present).

This sits between planning and implementation. Don't implement anything, don't restructure the plan,
and when you're done, hand back to the planning flow exactly where it was. The hook commands below
are `python3 "${CLAUDE_PLUGIN_ROOT}/hooks/plan_finished.py" …`, or with `python` if `python3` isn't
there.

## 1. Start

For a plan file, run the hook with `--begin "<file>"`, so it stays quiet while you edit the plan.

## 2. Check

Run the `plan-checker` agent with the plan (path, or text in plan mode) and the project folder.
When the filter and exclusions are already agreed (from `.claude/standards.json`, or the standards
skill earlier in this session), pass them too: the agent uses them as they are, and classifies
only what the plan adds beyond them.

If the plan was written from a spec that already ends with a current check marker, pass the
agent that spec's path too, and tell it to check only the plan steps the spec doesn't cover.

If the agent fails or times out, its reply has no JSON or malformed JSON, or its status is anything
but `complete` or `partial` (including `unavailable`), treat the check as unavailable: tell the user
"Standards unavailable: the plan wasn't checked", run the hook with `--end "<file>"` for a plan
file (it stops staying quiet for the file without marking it checked), and hand back to the planning
flow. In plan mode, present the unchanged plan with ExitPlanMode again. Either way the hook doesn't
ask again for this version in this session.

If the status is `partial`, carry on, and tell the user which owners were missing (the agent's
summary says).

## 3. Ask

Ask the report's `questions` with your question tool, a few at a time, the recommended option first.
A recipe is an option, not a rule, until it's chosen. If an answer picks a recipe, fetch it with
`get_topic`, the component's filter with `template: ["recipe"]` added, and the same exclusions, and
adjust the proposed changes to it. If the plan takes another approach the standards
allow, leave it as it is.

## 4. Approve

Show the proposed changes, one per step: the step's current text, the proposed text, and the rules
it comes from. Then the steps already fine, in one line, and any SHOULD notes. Ask with your question
tool: apply all, choose which, or none. For "choose which", ask a multi-select question (several at
a time if there are many changes).

With no changes and no questions, say "The plan meets the standards it touches" and go to step 6.

## 5. Rewrite

- Replace the approved steps' text in place, keeping the plan's numbering, headings, checkboxes and
  format. Never add an amendments section: the implementer reads one correct plan.
- For each approved scope answer (for example "record redis"), add the value to that component's
  scope in `.claude/standards.json` (if the file uses a single top-level `scope`, add it there). If
  the file doesn't exist, offer to run the init skill instead of writing it. Never remove values, and
  never add exclusions.
- In plan mode you may edit nothing but the plan: keep the approved scope changes, and write them to
  `.claude/standards.json` right after plan mode ends (ExitPlanMode accepted), before any
  implementation.

## 6. Finish

- **Plan file:** run the hook with `--stamp "<file>"`. It appends the marker line that tells the hook
  this version was checked.
- **Plan mode:** pipe the final plan text to the hook with `--fingerprint`, end the plan with the
  line it prints, and call ExitPlanMode with that plan. If plan mode keeps the plan in a file, end
  that file with the marker line too, so the hook sees it whichever it reads. Once ExitPlanMode is
  accepted, write the approved scope changes to `.claude/standards.json` (step 5).

Tell the user in two lines what changed (or that nothing needed to) and that planning carries on.
Then continue the planning flow where it was: for example, Superpowers' choice of how to execute
the plan.
