# Anarchy-AI Setup EXE Specification

## Purpose

This document defines the preferred delivery shape for the Windows-first Anarchy-AI installer.

The goal is to replace a script-first repo bootstrap with a cleaner delivery surface that still preserves bounded bootstrap behavior for agents and operators.

Current direction:

- `AnarchyAi.Setup.exe` = installer/bootstrap executable
- `AnarchyAi.Mcp.Server.exe` = long-running MCP runtime executable

These are separate roles and should remain separate.

The installer owns:

- plugin bundle materialization
- repo bootstrap
- marketplace registration
- update and refresh operations
- readiness assessment
- machine-readable install results

The runtime owns:

- MCP server execution
- harness tool exposure
- contract execution during active use

Install and self-replace belong to the setup exe. The runtime stays a pure MCP server.

## Repo-Authored Truth Rule

All carried schema-family artifacts, contracts, docs, disclaimers, and install assertions remain authored in the repo.

The standalone installer is a published carrier:

- it publishes repo-authored surfaces into the setup payload
- it resolves install paths relative to the destination at install time
- it does not become an alternate source of harness truth

Installed README and disclosure text should be generated from repo-authored source docs or structured source fragments where feasible so the published bundle stays destination-relative and honest.

## Role Split

### `AnarchyAi.Setup.exe`

This is the delivery and bootstrap surface.

It should be usable in two ways:

- human-clickable Windows installer
- agent or script callable CLI installer

It should be able to:

- assess install state
- install into the current repo
- update the local Anarchy-AI bundle
- register or repair `.agents/plugins/marketplace.json`
- verify readiness

Marketplace identity rule:

- each repo-local install uses its own repo-scoped marketplace root identity
- setup generates a repo-scoped marketplace `name` while keeping the human-facing display name recognizable
- this reduces collisions with Codex host-side install and uninstall state
- setup keeps the repo-local install directory repo-scoped while keeping the visible plugin identity and MCP server key stable as `anarchy-ai`
- this preserves predictable tool syntax like `mcp__anarchy_ai__...` while still keeping repo-local bundle materialization bounded to the selected workspace

### `AnarchyAi.Mcp.Server.exe`

This remains the packaged Windows-first MCP runtime launched from the installed plugin bundle.

It continues to expose harness tools and only harness tools.

These responsibilities live outside the runtime:

- GUI install behavior
- repo delivery logic
- self-update replacement logic
- marketplace registration logic

## Default Delivery Model

The preferred v1 delivery model is:

1. Drop `AnarchyAi.Setup.exe` into `./plugins/`.
2. Run it.
3. Choose an install lane.
4. Let it materialize the plugin bundle into the selected lane.
5. Let it create or update the matching marketplace root.
6. Let it verify that the target is ready.

Current lanes:

- `repo-local`
  - plugin bundle under `./plugins/anarchy-ai-local-<repo-slug>-<stable-path-hash>`
  - marketplace under `./.agents/plugins/marketplace.json`
- `user-profile`
  - plugin bundle under `~/.codex/plugins/anarchy-ai`
  - marketplace under `~/.agents/plugins/marketplace.json`

The generated marketplace root should be repo-scoped for repo-local installs, not globally reused.
Keep the top-level marketplace `name` branded and readable because current Codex plugin surfaces can expose that identifier directly even though the official docs describe `interface.displayName` as the marketplace title.

Current repo-local shape:

- `name = anarchy-ai-local-<repo-slug>`
- `interface.displayName = Anarchy-AI Local (<RepoName>)`
- `plugins.<entry>.name = anarchy-ai`
- `plugins.<entry>.source.path = ./plugins/anarchy-ai-local-<repo-slug>-<stable-path-hash>`
- `.codex-plugin/plugin.json -> name = anarchy-ai`
- `.mcp.json -> mcpServers -> anarchy-ai`

Current user-profile shape:

- `name = anarchy-ai-user-profile`
- `interface.displayName = Anarchy-AI User Profile`
- `plugins.<entry>.name = anarchy-ai`
- `plugins.<entry>.source.path = ./.codex/plugins/anarchy-ai`
- `.codex-plugin/plugin.json -> name = anarchy-ai`
- `.mcp.json -> mcpServers -> anarchy-ai`

Codex home readiness is plugin-marketplace-first:

- the normal home registration mode is the personal marketplace lane
- a custom `[mcp_servers.anarchy-ai]` block in `~/.codex/config.toml` is optional fallback or debug evidence only
- older legacy `[mcp_servers.anarchy-ai-herringms]` blocks are cleanup evidence only
- readiness does not require that custom MCP block

The preferred default targeting behavior is:

1. If `/repo` is provided, target that repo.
2. Else if the setup exe is located inside `<repo>\plugins\`, target that repo.
3. Else use the current working directory only when it can be identified as the intended repo root.
4. If repo detection is ambiguous, fail boundedly and ask for `/repo`.

Auto-detection boundary:

- auto-detection treats a folder as a repo only when a real repo marker is present, currently `.git`
- a folder containing `plugins/` or `.agents/` alone is ambiguous
- when the marker is absent, the installer requires explicit `/repo`

Inside the `AI-Links` source repo itself, `plugins/AnarchyAi.Setup.exe` is a generated artifact. It should be built locally rather than treated as canonical authored truth.

## GUI / CLI Behavior Split

### No arguments

No-argument launch should behave as the human-facing installer mode.

Expected behavior:

- launch a simple Windows GUI
- default target to the local repo when detectable
- show current target path
- allow browsing to a different repo path
- allow explicit install-lane selection between `repo-local` and `user-profile`
- show platform radios for future payload expansion, with only `Windows` enabled for the current payload
- offer `Install`, `Assess`, and later `Update`
- show a responsible disclosure page before GUI install continues
- show a compact success or failure summary

The GUI stays simple and bounded: install, assess, update. Full control-center features live elsewhere.

The install disclosure should stay concise and mostly generated from current installer facts so it remains aligned with rebuilds.

### CLI arguments

When switches are present, the setup exe should run in CLI mode.

Expected behavior:

- headless operation
- `/silent` suppresses all interactive prompts
- JSON result to stdout
- meaningful exit codes
- the same bounded semantics as the current bootstrap script

This is the mode AI agents and automation should use.

## CLI Contract

The CLI should preserve the current bootstrap model instead of inventing a new one.

Primary operations:

- `/assess`
- `/install`
- `/update`
- `/repolocal`
- `/userprofile`
- `/?`, `-?`, `/h`, `-h`, `/help`, `-help`, `--help`, `--?`

Primary flags:

- `/silent`
- `/json`
- `/repo "<path>"`
- `/host codex|claude|cursor|generic`
- `/sourcepath "<path>"`
- `/sourceurl "<url>"`
- `/refreshschemas`

Recommended Windows-style usage:

```text
AnarchyAi.Setup.exe /install /repolocal /silent /json
AnarchyAi.Setup.exe /install /userprofile /silent /json
AnarchyAi.Setup.exe /assess /userprofile /silent /json
AnarchyAi.Setup.exe /update /userprofile /silent /json /sourcepath "C:\path\to\AI-Links"
AnarchyAi.Setup.exe /install /repolocal /repo "C:\path\to\other-repo" /silent /json
```

### CLI rules

- `/silent` suppresses all prompts and the GUI
- `/json` emits machine-readable result to stdout
- `/repo` overrides repo auto-detection
- `/repolocal` installs or assesses through the selected repo root
- `/userprofile` installs or assesses through the current user profile
- `/sourcepath` allows local source refresh without depending on public TLS
- install seeds missing portable root schema files when a workspace root is targeted (`/repolocal`, or `/userprofile` with explicit `/repo`)
- `/refreshschemas` means force-refresh the portable root schema family only when a workspace root is targeted
- help aliases should print plain-text usage plus a generated repo-availability summary instead of raw JSON
- the help summary should tell the actor what Anarchy-AI adds here and what it changes in the repo

## Result Contract

CLI output should remain consistent with the current bootstrap lane.

Expected result shape:

- `bootstrap_state`
- `host_context`
- `install_scope`
- `registration_mode`
- `update_requested`
- `update_state`
- `update_source_zip_url`
- `update_notes`
- `runtime_present`
- `marketplace_registered`
- `installed_by_default`
- `actions_taken`
- `missing_components`
- `safe_repairs`
- `next_action`
- `paths`
  - `paths.origin`
  - `paths.source`
  - `paths.destination`

User-profile default result semantics:

- when `/userprofile` runs without `/repo`, `paths.destination.root_path` remains the current user profile root and portable schema seeding stays out of scope
- in that case portable schema seeding stays out of scope (`portable_schema_family_not_targeted`)
- `registration_mode = plugin_marketplace` is the normal Codex home result
- `registration_mode = custom_mcp_fallback` is reserved for bounded legacy-state reporting when a stale custom MCP surface exists but the Codex-native marketplace lane is not ready

Path-shape rules:

- setup assess/install/update no longer emit flat path fields such as `workspace_root`, `repo_root`, `plugin_root`, or `update_source_path`
- destination-relative file and directory facts now live under `paths.destination`
- update-source facts now live under `paths.source` when update actually pulled from a local path or downloaded extract root
- repo-authored source facts live under `paths.origin` only when that source is actually available to the current operation

Install lock semantics:

- when install finds one or more existing bundle files locked and skips their refresh, setup reports:
  - `bootstrap_state = registration_refresh_needed`
  - `missing_components` includes `locked_bundle_surface_write_skipped`
  - `next_action = release_runtime_lock_and_retry_install`
- this presents a bounded repair path instead of a hard crash and keeps `ready` state honest

The setup exe preserves these semantics so:

- agents use one install contract for every lane
- docs describe one install lane shared by script and exe
- delivery gets cleaner while the meaning layer stays stable

## Agent-Facing Expectations

Agent use of the setup exe should remain operationally similar to the current bootstrap script.

That means:

- agents can assess readiness
- agents can install when the repo needs bootstrapping
- agents can update from a local source path
- agents can read bounded repair guidance instead of improvising from raw failures

Agents route the following through the setup exe rather than manual operations:

- marketplace file edits
- plugin structure reconstruction
- bounded repair around runtime locks

The setup exe is the bounded repair and delivery lane.

## Human-Facing Expectations

The user should be able to:

- drop one installer file into `./plugins/`
- double-click it
- install Anarchy-AI into the local repo without reading the bootstrap script

The user should also be able to:

- point the installer at a different repo path
- choose repo-local versus user-profile install behavior explicitly
- assess whether a repo is already ready
- understand what failed without reading raw JSON

## Alternate Repo Targeting

Alternate repo targeting is a real requirement, not a convenience.

It should exist through:

- GUI browse and select
- CLI `/repo "<path>"`

Current read:

- this fits within normal installer architecture
- the authoritative override is always the explicit repo path

When local path detection is ambiguous, the installer fails boundedly and asks for `/repo`.

## Why This Is Better

This shape is better than script-first repo delivery because it:

- keeps runtime and installer responsibilities separate
- makes human installation much easier
- preserves machine-readable automation behavior for agents
- reduces direct manipulation of repo plugin structure
- keeps marketplace registration inside one bounded tool
- gives RMM, Immybot, and other automation lanes a cleaner future entry point

## Current Delivery State

This direction is now partially delivered in local state.

Delivered today:

- GUI mode covers `Install` and `Assess`
- GUI exposes install-lane selection and placeholder platform radios (`Windows` payload is the active lane)

Future work reserved for later delivery:

- GUI `Update`
- payload generation strategy refinement beyond the current embedded plugin-bundle resources
- code-signing or self-signed trust story
- Claude and Cursor specific packaging

## Environment Truth Discipline

Environment statements in this spec should be anchored to:

- `ANARCHY_AI_ENVIRONMENT_TRUTH_MATRIX.md`

Current proven environment fact for the user-profile lane:

- user-profile install is considered ready when:
  - `~/.agents/plugins/marketplace.json` contains the Anarchy-AI user-profile entry
  - `plugins.<entry>.source.path = ./.codex/plugins/anarchy-ai`
  - `~/.codex/plugins/anarchy-ai` contains the bundled plugin and runtime surfaces
  - setup result reports `registration_mode = plugin_marketplace`
- a custom `[mcp_servers.anarchy-ai]` entry is optional fallback or debug evidence only and is not required for readiness
- older legacy `[mcp_servers.anarchy-ai]` entries are cleanup evidence only

Current inferred behavior that still needs direct local proof:

- Codex may retain stale tool-surface indexing for a stable server key
- Codex may materialize an installed-copy cache under `~/.codex/plugins/cache/...` only after restart or first use

Inferred behavior stays labeled as inferred. Only observed behavior promotes to settled platform truth.

Portability claims in this spec should use explicit evidence grades:

- application portability (`Codex` proven, other hosts inferred until tested)
- session portability (fresh-session repeat required)
- device portability (second-device repeat required)

Use the matrix as the authority for claim promotion.

## Rule

The installer simplifies delivery. Harness truth stays in the shared core and runtime implementation.

The installer's job is to materialize, register, refresh, and assess the delivery surface.

The harness contracts and runtime semantics remain in the shared core and runtime implementation.

## Build And Publish Helper

The current one-command build helper is:

- `harness/setup/scripts/build-self-contained-exe.ps1`

Its current responsibilities are:

- resolve a usable SDK path, preferring the user-scoped install when needed
- sync `plugins/anarchy-ai/schemas/schema-bundle.manifest.json` hashes from current source schema files before publish
- generate `plugins/anarchy-ai/README.md` from `docs/ANARCHY_AI_PLUGIN_README_SOURCE.md` using destination-relative install facts
- run both the path-canon audit and the documentation-truth audit before publish succeeds
- publish the setup project with temp `obj/bin/publish` lanes under `AppData\Local\Temp`
- refresh the local generated `plugins/AnarchyAi.Setup.exe`

That keeps setup regeneration machine-local and avoids synced-workspace build churn.

