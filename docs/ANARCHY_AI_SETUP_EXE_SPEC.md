# Anarchy-AI Setup EXE Specification

## Purpose

This document defines the preferred delivery shape for a Windows-first Anarchy-AI installer.

The goal is to replace the current script-first repo bootstrap with a cleaner delivery surface that still preserves the same bounded bootstrap behavior for agents and operators.

Current direction:

- `AnarchyAi.Setup.exe` = installer/bootstrap executable
- `AnarchyAi.Mcp.Server.exe` = long-running MCP runtime executable

These are separate roles and should remain separate.

The installer should own:

- plugin bundle materialization
- repo bootstrap
- marketplace registration
- update/refresh operations
- readiness assessment
- machine-readable install results

The runtime should own:

- MCP server execution
- harness tool exposure
- contract execution during active use

The runtime should not try to install or replace itself in-place.

## Role Split

### `AnarchyAi.Setup.exe`

This is the delivery and bootstrap surface.

It should be usable in two ways:

- human-clickable Windows installer
- agent/script callable CLI installer

It should be able to:

- assess install state
- install into the current repo
- update the local Anarchy-AI bundle
- register or repair `.agents/plugins/marketplace.json`
- verify readiness

### `AnarchyAi.Mcp.Server.exe`

This remains the packaged Windows-first MCP runtime launched from the installed plugin bundle.

It should continue to expose harness tools and nothing more.

It should not absorb:

- GUI install behavior
- repo delivery logic
- self-update replacement logic
- marketplace registration logic

## Default Delivery Model

The preferred v1 delivery model is:

1. drop `AnarchyAi.Setup.exe` into `./plugins/`
2. run it
3. let it materialize `./plugins/anarchy-ai/`
4. let it create or update `./.agents/plugins/marketplace.json`
5. let it verify that the repo is ready

The preferred default targeting behavior is:

1. if `/repo` is provided, target that repo
2. else if the setup exe is located inside `<repo>\plugins\`, target that repo
3. else use current working directory only when it can be identified as the intended repo root
4. if repo detection is ambiguous, fail boundedly and ask for `/repo`

This keeps repo-local install simple while still allowing alternate targets.

Inside the `AI-Links` source repo itself, `plugins/AnarchyAi.Setup.exe` is a generated artifact.
It should be built locally rather than tracked in git.

## GUI / CLI Behavior Split

### No arguments

No-argument launch should behave as the human-facing installer mode.

Expected behavior:

- launch a simple Windows GUI
- default target to the local repo when detectable
- show current target path
- allow browsing to a different repo path
- offer `Install`, `Assess`, and later `Update`
- show a responsible-disclosure page before GUI install continues
- show a compact success/failure summary

The GUI should be simple and bounded. It does not need to become a full control center.
The install disclosure should stay concise and mostly generated from current installer facts so it remains aligned with rebuilds.

### CLI arguments

When switches are present, the setup exe should run in CLI mode.

Expected behavior:

- no GUI
- no interactive prompts when `/silent` is passed
- JSON result to stdout
- meaningful exit codes
- same bounded semantics as the current bootstrap script

This is the mode AI agents and automation should use.

## CLI Contract

The CLI should preserve the current bootstrap model instead of inventing a new one.

Primary operations:

- `/assess`
- `/install`
- `/update`
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
AnarchyAi.Setup.exe /install /silent /json
AnarchyAi.Setup.exe /assess /silent /json
AnarchyAi.Setup.exe /update /silent /json /sourcepath "C:\path\to\AI-Links"
AnarchyAi.Setup.exe /install /repo "C:\path\to\other-repo" /silent /json
```

### CLI rules

- `/silent` means no prompts and no GUI
- `/json` means emit machine-readable result to stdout
- `/repo` overrides repo auto-detection
- `/sourcepath` allows local source refresh without depending on public TLS
- `/refreshschemas` means refresh the portable root schema family in addition to the plugin bundle
- help aliases should print plain-text usage plus a generated repo-availability summary instead of raw JSON
- the help summary should tell the actor what Anarchy-AI adds here and what it changes in the repo

## Result Contract

CLI output should remain consistent with the current bootstrap lane.

Expected result shape:

- `bootstrap_state`
- `host_context`
- `update_requested`
- `update_state`
- `update_source_zip_url`
- `update_source_path`
- `update_notes`
- `repo_root`
- `plugin_root`
- `runtime_present`
- `marketplace_registered`
- `installed_by_default`
- `actions_taken`
- `missing_components`
- `safe_repairs`
- `next_action`

The setup exe should preserve these semantics so:

- agents do not need a second install contract
- docs do not fork into script behavior versus exe behavior
- delivery gets cleaner without changing the meaning layer

## Agent-Facing Expectations

Agent use of the setup exe should remain operationally similar to the current bootstrap script.

That means:

- agents can assess readiness
- agents can install when the repo is not bootstrapped
- agents can update from a local source path
- agents can read bounded repair guidance instead of improvising from raw failures

The setup exe should not encourage agents to:

- manually rewrite marketplace files
- manually reconstruct plugin structure
- improvise deletion around locked runtime files

The setup exe should become the bounded repair and delivery lane.

## Human-Facing Expectations

The user should be able to:

- drop one installer file into `./plugins/`
- double-click it
- install Anarchy-AI into the local repo without reading the bootstrap script

The user should also be able to:

- point the installer at a different repo path
- assess whether a repo is already ready
- understand what failed without reading raw JSON

## Alternate Repo Targeting

Alternate repo targeting is a real requirement, not a convenience.

It should exist through:

- GUI browse/select
- CLI `/repo "<path>"`

Current read:

- this does not appear to require a major architectural rewrite
- it is a normal installer concern
- the authoritative override should always be the explicit repo path

If local path detection is ambiguous, the installer should fail boundedly and ask for `/repo`.

## Why This Is Better

This shape is better than script-first repo delivery because it:

- keeps runtime and installer responsibilities separate
- makes human installation much easier
- preserves machine-readable automation behavior for agents
- reduces direct manipulation of repo plugin structure
- keeps marketplace registration inside one bounded tool
- gives RMM/Immybot and other automation lanes a cleaner future entry point

## Current Gaps

This direction is now partially delivered in local state.

Still open:

- GUI mode currently covers:
  - `Install`
  - `Assess`
- GUI `Update` is not yet implemented
- the current payload strategy is embedded plugin-bundle resources; future build/refinement work may still change how payload generation is maintained
- code-signing or self-signed trust story remains future work
- Claude/Cursor-specific packaging still remains adapter work even after installer cleanup

## Rule

The installer may simplify delivery, but it must not become a new source of harness truth.

Its job is to materialize, register, refresh, and assess the delivery surface.

The harness contracts and runtime semantics still belong in the shared core and runtime implementation.

## Build And Publish Helper

The current one-command build helper is:

- `harness/setup/scripts/build-self-contained-exe.ps1`

Its current responsibilities are:

- resolve a usable SDK path, preferring the user-scoped install when needed
- publish the setup project with temp `obj/bin/publish` lanes under `AppData\Local\Temp`
- refresh the local generated `plugins/AnarchyAi.Setup.exe`

That keeps setup regeneration machine-local and avoids synced-workspace build churn.
