---
name: setup
description: >-
  Set up this repository on this machine: check the requirements, build, run the tests, have the
  user start the services (from their IDE, so they can debug), and connect Claude Code to the
  standards plugin. On an existing setup, asks what to do: update, rename the plugin, check it, or
  uninstall.
disable-model-invocation: true
---

# Set up org-standards-plugin

Walk the user through getting this repository running, from the repository root. Report what you
find at each step before moving on.

**Ground rule:** ask before changing anything outside this folder. That includes installing
software and changing Claude Code settings. Show the exact command and wait for a yes. Never
install anything silently.

## 0. Already set up? Ask what to do

Check first: `claude plugin marketplace list` and `claude plugin list`. If a catalog's source is
this folder, the plugin is already installed. Say so (its name, and its scope: all projects or this
folder only), then ask what the user wants, and do only that:

- **Update:** pick up new code and standards. The plugin is read from this folder in place, so an
  update never needs a reinstall:
  1. If this folder is a git clone with a remote, offer `git pull` (show the command, wait for a yes).
  2. If the AppHost from this folder is running, ask the user to stop it where they started it (a
     running AppHost locks its build output, so the build fails otherwise). Stop it yourself only
     if you started it.
  3. Build and test (step 2), then have the user start the services again (step 3). The databases
     are rebuilt from the standards files on every start, so the new standards are served.
  4. Tell the user to run `/reload-plugins` in any open Claude Code session.
- **Rename the plugin:** ask for the new name. This changes Claude Code settings, so confirm first.
  1. Remove the current catalog: `claude plugin marketplace remove <old>-standards`.
  2. In `.claude-plugin/marketplace.json`, set the catalog `name` to `<new>-standards` and the
     plugin entry's `name` to `<new>`. Tell the user this edits a tracked file, so they shouldn't
     commit it.
  3. Register and install again as in step 4, with the **same scope** as before:
     `claude plugin marketplace add ./`, then `claude plugin install <new>@<new>-standards --scope <same>`.
  4. Tell the user what changes: the skill becomes `/<new>:standards`, the reviewer
     `<new>:standards-reviewer`, and the MCP server `plugin:<new>:standards`. A project that turns
     the plugin on through its `.claude/settings.json` must use the new names. The standards
     themselves don't change.
  5. Tell the user to run `/reload-plugins`.
- **Update and rename:** both, update first.
- **Check it:** change nothing. Report whether the services are up (`http://localhost:5480/`) and
  whether `claude mcp list` shows the plugin's server connected.
- **Uninstall:** confirm, then `claude plugin marketplace remove <name>-standards`. Offer to restore
  `.claude-plugin/marketplace.json` (`git checkout .claude-plugin/marketplace.json`) if it was
  renamed. The services keep running until the user stops them.

Then report (step 5), covering only what was done. Run steps 1–4 only for a fresh setup.

## 1. Check the requirements

| Need | Check | Passes when |
|---|---|---|
| .NET SDK 10 or later | `dotnet --list-sdks` | a `10.x` or newer SDK is listed. `global.json` at the root sets 10.0.100 as the floor and allows newer and preview SDKs on purpose; don't suggest pinning one |
| A container runtime | `docker info` (or `podman info`) | it prints server information |
| Ports 5480 and 5500 | nothing is listening on them | both are free |

If something is missing, say so and offer to install it:

- **Windows** (winget): `winget install Microsoft.DotNet.SDK.10`, and for the container runtime one of
  `Docker.DockerDesktop`, `SUSE.RancherDesktop` or `RedHat.Podman-Desktop`. Ask which runtime the
  user wants; Docker Desktop is Aspire's default.
- **macOS and Linux:** link to the official pages instead of guessing package names:
  https://dotnet.microsoft.com/download/dotnet/10.0 and https://aspire.dev/get-started/prerequisites/.

Runtime notes:

- Installed but not running: ask the user to start it, then check again.
- **Rancher Desktop** must use the *dockerd (moby)* engine (Preferences → Container Engine).
- **Podman:** set `ASPIRE_CONTAINER_RUNTIME=podman` for the commands you run.
- A port in use: report what's using it (for example another copy of this app) and ask before
  stopping anything.

## 2. Build and test

1. `dotnet build src/OrgStandards.slnx`
2. `dotnet test src/OrgStandards.Tests`

The first test run pulls the Postgres image, so it can take a few minutes. Report the counts. If the
integration tests are skipped, the container runtime isn't reachable: go back to step 1.

## 3. The user starts the services

The user starts the AppHost themselves, not you. Started from their IDE, the AppHost and every
service it launches run under the debugger, so they can set breakpoints and walk through a request
(gateway, owners, migrations). A shell started by you can't be debugged and stops when this
session ends.

Offer the ways to start it, recommending the IDE:

| From | Do |
|---|---|
| Visual Studio or Rider (recommended) | Open `src/OrgStandards.slnx` and run **OrgStandards.AppHost**, with debugging (F5). It's the first project and the startup project. |
| Their own terminal | `dotnet run --project src/OrgStandards.AppHost --launch-profile http` |

Then wait for the user to say it's running. Check that `http://localhost:5480/` responds (the first
start pulls the Postgres image and can take a minute or two); if it doesn't, help them read the
AppHost's output or the dashboard. Then tell the user:

- the getting-started page: http://localhost:5480/
- the design-system site: http://localhost:5500/
- the Aspire dashboard: Visual Studio opens it; otherwise its login link is in the AppHost's output.

Start it yourself only if the user asks you to. Then run it in the background, and say plainly
that it can't be debugged and stops when this session ends.

## 4. Connect Claude Code

Everything in this step stays on this machine. Say so plainly when you offer the options: nothing is
published, uploaded or downloaded, and no account is involved.

A running Claude Code session can't load a plugin by folder, so offer these options and let the
user choose:

- **Try it, nothing installed:** in a new terminal at the repository root, start
  `claude --plugin-dir ./plugins/my-company`. The plugin loads for that session only, named
  `my-company`. Nothing to undo.
- **Install it for your projects (usual):** this changes Claude Code settings, so ask first, and
  ask where it should be available:
  - **All your projects** (`--scope user`): the usual choice; the plugin is meant to be used in
    other repositories.
  - **This folder only** (`--scope local`): to try it here without affecting other projects.

  Before running anything, explain the word "marketplace" in one or two sentences: Claude Code
  installs plugins from a marketplace, which here is only a **local catalog file in this folder**
  (`.claude-plugin/marketplace.json`). Registering it tells Claude Code where the folder is; nothing
  is published or fetched. Then run `claude plugin marketplace add ./` (the `./` is required; a bare
  `.` is rejected), and only if that succeeds, `claude plugin install acme@acme-standards --scope
  <user|local>`. Tell the user to run `/reload-plugins`. Undo with
  `claude plugin marketplace remove acme-standards`.
- **Install it under the user's own name:** the plugin has no name of its own; the catalog entry
  names it. In `.claude-plugin/marketplace.json`, change the catalog `name` (for example to
  `<name>-standards`) and the plugin entry's `name` (to `<name>`), then install as above with
  `<name>@<name>-standards`. Tell the user this edits a tracked file, so they shouldn't commit it,
  and that the standards themselves are still Acme's: renaming the plugin doesn't change what the
  standards server serves.

If a catalog with the same name is already registered (for example from another copy of this
repository), report it and ask before removing it.

## 5. Report

Open with one line that says where everything runs, for example: *"Everything is running locally:
the services on this machine, and the plugin loaded from this folder. Nothing was published or
uploaded."*

Then summarize:

- what passed;
- how the plugin is connected, in plain words: loaded for one session, or installed for all your
  projects or for this folder only, from the local catalog in this folder;
- what that means in use: the plugin is read from this folder in place (moving or deleting it breaks
  the plugin), and the services must be running for it to work, since it connects to
  `http://localhost:5480/mcp`. Other projects pick up the standards automatically when they include
  `project-template/.claude/` (`CLAUDE.md` and `standards.json`, with the project's scope and
  exclusions);
- what the user still has to do (for example `/reload-plugins`);
- how to stop the services (where the user started them: stop debugging, or Ctrl+C in their
  terminal) and how to uninstall;
- a first thing to try from the getting-started page, for example *"Which standards apply to a web
  API for xyz-public-app?"*

Report only what you checked. Don't suggest changes the requirements table already settles (for
example pinning the .NET SDK).
