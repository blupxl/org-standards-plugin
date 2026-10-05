# The standards skill: `/acme:standards`

Brings the standards that apply to a task into the conversation before Claude builds anything, and
has the work checked afterwards. Source:
[`plugins/my-company/skills/standards/SKILL.md`](../../plugins/my-company/skills/standards/SKILL.md).

## When it runs

On its own, whenever work has to fit company conventions: building or changing a web API, a backend
service, UI code, a page, form, mockup or prototype, or asking which standards apply. In a
[project set up for it](../project-setup.md), an ordinary request is enough. You can also call it:

```
/acme:standards web API for xyz-public-app
```

## What it does

1. **Works out the scope.** Starts from the project's `.claude/standards.json`. Otherwise it decides
   the `product` (and asks if that isn't clear), the `kind` of work, the `runtime` from the files
   (`*.csproj` means `dotnet`, `package.json` with server code means `node`), and leaves `concern`
   out so every concern applies. It uses only fields and values `list_standards` returns.
2. **Starts new work from a recipe.** When the task starts a project or adds a capability (data
   access, messaging, …), it asks what the work needs, lists the architects' approved
   [recipes](../templates.md) with the recommended one first, follows the chosen one, and records
   the choice in `.claude/standards.json`.
3. **Gets the standards** in one `get_standards` call, one request per component, each with its own
   kind and concern, and the project's exclusions. It reads each document's header first: an
   incomplete or unresolved answer is reported, not ignored.
4. **Follows them.** MUST and MUST NOT are requirements: if one can't be met, it stops and says why.
   SHOULD is the default; a deviation needs a stated reason. Details such as exact color tokens are
   fetched with `get_topic`, never reconstructed from memory.
5. **Has the work checked** by the [standards reviewer](standards-reviewer.md), with the same filters
   and exclusions, and passes on its findings as they are.
6. **Reports** which standards applied, the decisions it made itself (marking the ones you should
   confirm), any SHOULD not followed and why, any gaps the work touched, and the reviewer's result.
7. **Records the work** in a new dated folder, `docs/standards/implementations/<YYYY-MM-DD-HHmm>/`:
   `implementation.md` for people and `implementation.json` as data, with each review round's
   failures, fixes and result, and the same finding ids as an [assessment](assess.md). Commit it
   with the change, and the pull request carries its own standards record.

## What it won't do

- **Add exclusions.** Only the architects can, in `.claude/standards.json`. Asked to skip a rule
  while building, it says so and leaves the decision to them.
- **Guess.** An unknown product or field is a question, not a substitution.
- **Refactor existing code** that contradicts a standard. It follows the standard in new code and
  points the old code out.
