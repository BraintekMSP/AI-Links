# Anarchy-AI Plugin

## Purpose

This plugin is the user-facing delivery surface for Anarchy-AI, the runtime framework harness for the AGENTS Heuristic Underlay.

The current architecture is:

- schema family = canonical layer
- AGENTS Heuristic Underlay = operative layer built from that family
- Anarchy-AI = runtime framework harness

It exists so users do not need to manually wire:

- harness paths
- MCP launch commands
- runtime tool discovery

## What It Connects

- plugin layer:
  - this directory
- bundled contracts:
  - `./contracts/`
- bundled runtime:
  - `./runtime/win-x64/`
- source harness in this repo:
  - `../../../harness/`

## Current Delivery Scope

The packaged plugin delivery is Windows-first today.

That means:

- the bundled runtime is `./runtime/win-x64/AnarchyAi.Mcp.Server.exe`
- `.mcp.json` launches that bundled executable directly
- `start-anarchy-ai.cmd` is a development helper and fallback path, not the primary install story

The broader harness contract may later travel farther than the current packaged runtime, but the present bundle should be described honestly as Windows-first.

## Current Delivery Story

The plugin provides:

- a local MCP declaration through `.mcp.json`
- direct launch of the bundled `.NET 8` self-contained single-file runtime inside the plugin
- a bundled canonical schema family plus hash manifest under `./schemas/`
- a skill that teaches when to use the five bounded runtime tools
- a repo-bootstrap script at `./scripts/bootstrap-anarchy-ai.ps1` for install, assess, and bundle refresh
- a runtime lock script at `./scripts/stop-anarchy-ai.ps1` for assessing, safely releasing, or forcibly releasing the bundled repo-local Anarchy-AI runtime lock

The repo-local launcher script is retained only as a development helper during source work. It is not the intended packaged delivery path.

The plugin does not yet add a custom UI panel or settings page.

## Current Tool State

- `preflight_session` is implemented and returns:
  - bounded readiness for meaningful governed work
  - recommended next path
  - adoption state and active gaps
- `compile_active_work_state` is implemented and returns a bounded operational packet for:
  - current objective
  - active lane
  - blockers
  - stop point
  - evidence and degradation signals
- `is_schema_real_or_shadow_copied` is implemented and returns:
  - bounded schema reality classification
  - canonical schema bundle integrity
  - derived `possession_state` for canonically diverged but operative workspaces
- `assess_harness_gap_state` is implemented and returns:
  - installation state
  - runtime state
  - schema state
  - adoption state
  - missing components and safe repairs
- `run_gov2gov_migration` is implemented for:
  - planning non-destructive gov2gov reconciliation
  - copying missing canonical schema bundle files into the workspace in `non_destructive_apply`
  - auditing canonical divergence instead of silently overwriting it
- Together, these tools give Anarchy-AI its current runtime promise:
  - preflight meaningful governed work before the agent proceeds
  - compile active work into bounded operational state
  - evaluate whether the schema package is materially real here
  - assess install, runtime, schema, and adoption gaps explicitly
  - reconcile local drift or partial materialization without replacing schema authorship
- The plugin bundle currently carries:
  - contracts
  - runtime
  - canonical schemas
  - schema bundle manifest
  - skill
- The plugin bundle can refresh canonical schema-family files from its own carried schema bundle.
- The bootstrap script can refresh the plugin bundle from a public zip source or from a local source path when machine trust/TLS is unreliable.
- The bootstrap refresh path does not replace a live bundled runtime in place; stop the running `AnarchyAi.Mcp.Server.exe` process before retrying an update that touches `runtime/win-x64/AnarchyAi.Mcp.Server.exe`.
- The dedicated runtime-lock commands are:
  - assess runtime lock:
    - `powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\stop-anarchy-ai.ps1 -Mode AssessRuntimeLock`
  - safe release runtime lock:
    - `powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\stop-anarchy-ai.ps1 -Mode SafeReleaseRuntimeLock`
  - force release runtime lock:
    - `powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\stop-anarchy-ai.ps1 -Mode ForceReleaseRuntimeLock`
- `SafeReleaseRuntimeLock` does not request UAC elevation.
- `ForceReleaseRuntimeLock` tries once normally, then retries once through a UAC elevation prompt if the release fails with access denied.
- The safe/force split is intentional for both humans and agents:
  - it gives the actor a bounded repair option before resorting to force behavior
  - it makes the cause legible when a live runtime lock is blocking update
  - it gives the agent stronger direction about the problem before it reaches for broader file or path manipulation
- The plugin bundle still does not invent repo-authored governed files such as `AGENTS-hello.md`, `AGENTS-Terms.md`, `AGENTS-Vision.md`, or `AGENTS-Rules.md`.
- The current first install lane is repo bootstrap, not machine-wide install.
