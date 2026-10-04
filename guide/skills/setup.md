# The setup skill: `/setup`

Sets this repository up on your machine, or updates an existing setup. It's a project skill that
comes with the repository, not part of the plugin: open Claude Code in the repository's root
folder, accept the workspace trust prompt (that's what makes `/setup` available), and run it.
Source: [`.claude/skills/setup/SKILL.md`](../../.claude/skills/setup/SKILL.md).

**Ground rule:** it asks before changing anything outside this folder, including installing
software and changing Claude Code settings, and shows the exact command first.

## A fresh setup

1. **Checks the requirements:** .NET SDK, a container runtime, free ports. If something's missing,
   it offers to install it (winget on Windows; links on macOS and Linux).
2. **Builds and runs the tests**, and reports the counts.
3. **You start the services**, from Visual Studio or Rider with debugging (recommended, so you can
   step through a request), or your own terminal. It waits, then checks the gateway answers. It
   starts them itself only if you ask, and says that such a run can't be debugged.
4. **Connects Claude Code**, your choice:
   - try it for one session (`claude --plugin-dir ./plugins/my-company`), nothing installed;
   - install it for **all your projects** (usual) or this folder only, from the local catalog in
     this folder ([what that means](../install.md#what-the-marketplace-is-here));
   - install it under your own name.
5. **Reports**, opening with where everything runs (locally; nothing published or uploaded), how
   the plugin is connected, and how to stop and uninstall.

## An existing setup

If the plugin is already installed from this folder, it says so (name and scope) and asks what you
want, then does only that:

| Choice | Does |
|---|---|
| **Update** | Optional `git pull`, asks you to stop the services, rebuilds and tests, has you restart them, then `/reload-plugins`. No reinstall: the plugin is read in place. |
| **Rename the plugin** | Asks for the name, renames the catalog entry, reinstalls with the same scope, and lists the names that change. |
| **Update and rename** | Both, update first. |
| **Check it** | Changes nothing; reports whether the services and the plugin's server are up. |
| **Uninstall** | Removes the catalog; your services keep running until you stop them. |
