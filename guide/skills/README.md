# Skills and agents

What the plugin adds to Claude Code, and the repository's own setup skill. Back to the
[guide](../README.md) or the [main README](../../README.md).

| | Runs | Does |
|---|---|---|
| [`/acme:init`](init.md) | When you set a project up | Scans what the project is, confirms it with a short questionnaire, and writes `.claude/standards.json` and a standards section in `.claude/CLAUDE.md` |
| [`/acme:standards`](standards.md) | On its own when work has to fit company conventions, or when called | Brings the standards for a task into the conversation, and has the work checked |
| [`/acme:assess`](assess.md) | When asked to assess a project | Checks an existing project against its standards; saves each run for comparison |
| [`/acme:classify`](classify.md) | When another skill needs a scope, or when called | Classifies work into the standards' categories |
| [`/acme:plan-check`](plan-check.md) | When a plan is finished (a hook asks), or when asked | Checks a finished plan before it's built, and rewrites the steps you approve |
| [`acme:standards-reviewer`](standards-reviewer.md) | Handed work by the standards and assess skills, or asked for a review | Read-only agent that checks code against the standards, rule by rule |
| `acme:classifier` | Handed each component by the assess skill | Read-only agent that runs the classify skill and returns JSON |
| `acme:plan-checker` | Handed a plan by the plan-check skill | Read-only agent that checks a plan against the standards and proposes changes |
| [`/setup`](setup.md) | When you run it in this repository | Sets up, updates, renames or uninstalls the plugin on this machine |

The `acme` prefix comes from the local catalog entry; [renamed](../install.md#update-rename-check-uninstall),
the same skills become `/<name>:init`, `/<name>:standards`, `/<name>:assess`, `/<name>:classify` and `/<name>:plan-check`.
