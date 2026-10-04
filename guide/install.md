# Install the plugin

To use the plugin in your other projects, install it into Claude Code. **Everything stays on your
machine:** nothing is published, uploaded or downloaded, no account is involved, and the plugin
doesn't appear in any public listing. [`/setup`](skills/setup.md) does all of this, and asks first.

## What the "marketplace" is here

Claude Code installs plugins from a *marketplace*, which is just a catalog file saying which
plugins exist and where their code is. This repository includes one,
`.claude-plugin/marketplace.json`, listing a single plugin, `acme`, whose code is
`./plugins/my-company` in this folder. Registering it (`claude plugin marketplace add ./`) only tells
Claude Code where this folder is on your disk. Despite the name, it's a **local catalog**, not a
store.

The catalog entry is also what names the plugin. The plugin folder has no `plugin.json`, so the same
code becomes `/acme:standards` here, and could become `/globex:standards` in another company's
catalog, with no code changes.

## For all your projects (usual)

From the repository root:

```bash
claude plugin marketplace add ./                            # register this folder's local catalog
claude plugin install acme@acme-standards --scope user      # enable the plugin in every project
```

Then run `/reload-plugins` in any open session. Use `--scope local` instead to enable it only in
this folder. To remove it, `claude plugin marketplace remove acme-standards`.

Two things to know:

- **The plugin is read from this folder in place.** Edits take effect at the next session or after
  `/reload-plugins`; moving or deleting the folder breaks the plugin.
- **The services must be running wherever you use it.** The plugin connects to
  `http://localhost:5480/mcp`, which the AppHost serves from this folder.

## For one project, by its settings

Instead of installing it yourself, a project can turn the plugin on for everyone who opens it, by
committing a pointer to this folder in its `.claude/settings.json`. For a project `foo`:

```jsonc
// foo/.claude/settings.json
{
  "extraKnownMarketplaces": {
    "acme-standards": {
      "source": { "source": "directory", "path": "C:\\path\\to\\org-standards-plugin" }
    }
  },
  "enabledPlugins": {
    "acme@acme-standards": true
  }
}
```

The path is a folder on the developer's machine; nothing is fetched from anywhere. What happens when
a developer opens `foo` in Claude Code:

1. **Trust first.** Project settings can only turn plugins on after the developer accepts the
   workspace trust dialog for `foo` in an interactive session. Until then the plugin shows as
   disabled. Trusting a parent folder or running `claude -p` isn't enough.
2. **The local catalog is registered in the background**, and the plugin loads straight from the
   folder. There is no separate install step.
3. **The MCP server needs approval**, like any server a project declares.

A developer who doesn't want it in `foo` sets `"acme@acme-standards": false` in
`foo/.claude/settings.local.json`. Cloud sessions (claude.ai/code) don't register a project's
catalogs, because they never show the trust dialog.

## Update, rename, check, uninstall

- **Update:** pull the changes (or copy in the new version), restart the services, and run
  `/reload-plugins`. The plugin is read in place and the databases are rebuilt from the standards
  files on every start, so there's nothing to reinstall.
- **Rename:** change the catalog's `name` and the plugin entry's `name` in
  `.claude-plugin/marketplace.json`, then remove and add the catalog again. Every name changes with
  it: `/<name>:standards`, `<name>:standards-reviewer`, `plugin:<name>:standards`.
- **Check:** with the services running, `claude mcp list` shows
  `plugin:acme:standards: http://localhost:5480/mcp (HTTP) - ✔ Connected`.
- **Uninstall:** `claude plugin marketplace remove acme-standards`.

Running `/setup` again on an existing setup asks which of these you want, and does only that.
