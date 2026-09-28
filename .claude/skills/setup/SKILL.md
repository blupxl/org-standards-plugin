---
name: setup
description: >-
  Set up this repository on this machine: check the requirements, build, run the tests, start the
  services, and connect Claude Code to the standards plugin.
disable-model-invocation: true
---

# Set up org-standards-plugin

Walk the user through getting this repository running, from the repository root. Report what you
find at each step before moving on.

**Ground rule:** ask before changing anything outside this folder. That includes installing
software and changing Claude Code settings. Show the exact command and wait for a yes. Never
install anything silently.

## 1. Check the requirements

| Need | Check | Passes when |
|---|---|---|
| .NET SDK 10 or later | `dotnet --list-sdks` | a `10.x` or newer SDK is listed |
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
four integration tests are skipped, the container runtime isn't reachable: go back to step 1.

## 3. Start the services

Start the AppHost in the background, because it keeps running:

```
dotnet run --project src/OrgStandards.AppHost --launch-profile http
```

Wait until `http://localhost:5480/` responds; the first start can take a minute or two. Then tell the
user:

- the getting-started page: http://localhost:5480/
- the design-system site: http://localhost:5500/
- the Aspire dashboard: its login link is in the AppHost's output.

## 4. Connect Claude Code

A running Claude Code session can't load a plugin by folder, so offer both options and let the user
choose:

- **Try it, nothing installed (recommended):** in a new terminal at the repository root, start
  `claude --plugin-dir ./plugins/my-company`. The plugin loads for that session only, named
  `my-company`.
- **Install it under the company name `acme`:** this changes Claude Code settings, so ask first. Then
  run `claude plugin marketplace add .` and `claude plugin install acme@acme-standards --scope local`,
  and tell the user to run `/reload-plugins`. Undo with
  `claude plugin marketplace remove acme-standards`.

## 5. Report

Summarize:

- what passed,
- what the user still has to do,
- how to stop the services (stop the background AppHost),
- a first thing to try from the getting-started page, for example *"Which standards apply to a web
  API for xyz-public-app?"*
