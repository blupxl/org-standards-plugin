# Getting started

Everything runs on your machine. The quickest way is to let Claude do it: clone the repository,
open Claude Code in its root folder, and run `/setup` (see [the setup skill](skills/setup.md)). This
page is the same thing by hand.

## Requirements

| Need | Get it | Check it |
|---|---|---|
| **.NET SDK 10 or later** (the minimum is set in `global.json`; newer SDKs, previews included, are used when installed) | [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0) | `dotnet --list-sdks` lists `10.x` or newer |
| **A container runtime** (runs one Postgres container) | See [Container runtime](#container-runtime) below | See below |
| **Claude Code** | [code.claude.com/docs/en/setup](https://code.claude.com/docs/en/setup) | `claude --version` |
| Aspire CLI *(optional, only for `aspire run`)* | [aspire.dev/get-started/install-cli](https://aspire.dev/get-started/install-cli/) | `aspire --version` |

Tested on Windows 11 with Rancher Desktop (Docker engine 27.3.1), with .NET SDKs 10.0.204, 10.0.401
and 11 preview. macOS and Linux should work, but haven't been tried.

### Container runtime

Pick one. Aspire's own guidance is on its
[prerequisites page](https://aspire.dev/get-started/prerequisites/).

| Runtime | Install | Setup for this project | Check it's running |
|---|---|---|---|
| **Docker Desktop** (Aspire's recommended default) | [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop/) | None | `docker info` prints a *Server* section |
| **Podman** (Podman Desktop) | [podman-desktop.io](https://podman-desktop.io/) | Tell Aspire to use it: set `ASPIRE_CONTAINER_RUNTIME=podman` before running | `podman info` |
| **Rancher Desktop** (community-supported by Aspire; what this was tested on) | [rancherdesktop.io](https://rancherdesktop.io/) | In *Preferences → Container Engine*, choose **dockerd (moby)**; containerd doesn't serve the Docker API | `docker info` prints a *Server* section |

Setting the Podman variable for one terminal session:

```bash
export ASPIRE_CONTAINER_RUNTIME=podman       # bash / zsh
$env:ASPIRE_CONTAINER_RUNTIME = "podman"     # PowerShell
```

## Run the services

1. **Start your container runtime** and wait until its check above succeeds.
2. **Run the AppHost.** It starts everything else, and there is nothing to configure.

   | From | Do |
   |---|---|
   | Visual Studio / Rider (recommended) | Open `src/OrgStandards.slnx` and run **OrgStandards.AppHost** with debugging (F5). It's the first project, and the startup project on a fresh clone. |
   | Command line | `dotnet run --project src/OrgStandards.AppHost --launch-profile http` |
   | Aspire CLI | `cd src` then `aspire run` |

3. **Check it's up:** `http://localhost:5480/` shows the getting-started page. Visual Studio opens
   it for you.

**Walk through the code.** Run from Visual Studio or Rider with debugging: the AppHost and every
service it starts run under the debugger, so a breakpoint in the gateway (`Resolver.Overlay`, for
example) or an owner's server stops on a real request from Claude Code.

Don't start the other projects on their own: they get their names, databases, and addresses from
the AppHost, and will say so if started directly.

**Before the first run:**

- **HTTPS certificate.** Visual Studio starts the AppHost with its `https` profile, which needs a
  trusted .NET development certificate. If the dashboard complains about the certificate, run
  `dotnet dev-certs https --trust` once
  ([details](https://learn.microsoft.com/dotnet/core/tools/dotnet-dev-certs)), or choose the
  `http` profile. The command line above already uses `http`.
- **Ports.** `5480` (gateway) and `5500` (design-system site) must be free.

The first run pulls the Postgres image, so it takes a minute. Startup order is handled by Aspire:

1. **postgres** starts, with one database per standards owner.
2. **migrations** migrates each database and loads the Markdown documents in
   `OrgStandards.Migrations/seed/<owner>/`, replacing what was there. The files are the source of
   truth. If it fails, it exits non-zero and nothing downstream starts.
3. **design**, **platform**, **security** and **data** start once migrations have finished.
4. **gateway** listens on `http://localhost:5480/mcp`.
5. **acme-web**, a stand-in for Acme's design-system site, at `http://localhost:5500`. It owns the
   stylesheet the design standards require (`/css/acme.css`, plus `/css/xyz.css` for the XYZ
   palette and `/css/website.css` for the public website), so every rule can be met by a plain HTML
   page.
6. **pgweb**, a small web UI for the databases, opens from the dashboard with one bookmark per
   owner's database, registered automatically (a new owner gets its bookmark on the next run).

The AppHost opens (or prints a link to) the Aspire dashboard, which shows every resource, its
logs, and traces of each fan-out call.

## Try it

With the AppHost running, from the repository root:

```bash
claude --plugin-dir ./plugins/my-company
```

This loads the plugin (skills, agents, the plan hook and the MCP tools) for that session only. Nothing is installed or
saved, so there's nothing to undo. Loaded this way the plugin is named after its folder,
`my-company`. To use it in your other projects, [install it](install.md).

Things to ask:

- *"What standards are available? Use the standards tools."*
- *"I'm building a web API for xyz-public-app. Which standards apply?"*
- *"Which colors should I use for a new settings page?"*
- *"Start a new orders API with data access on PostgreSQL."* (Claude offers the approved recipes.)
- In another repository, with the plugin [installed](install.md): *"Assess this project against
  our standards"* (or `/acme:assess`).

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `docker daemon is not running` / resources stuck in `Starting` | Start the container runtime. With Rancher Desktop, choose the dockerd (moby) engine. |
| `migrations` finished with exit code 1, owners never start | Check its logs in the dashboard. Usually malformed YAML frontmatter in a seed document. |
| Build fails with files locked | The app is still running. Stop debugging (or `aspire stop`), then build. |
| A document says `unavailable` or `partial` | One or more owners didn't answer within 10 seconds. Check the dashboard; the first call after a cold start can be slow. |
| `/mcp` shows the server failing | The AppHost isn't running, or something else is using port 5480. |
