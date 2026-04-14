# Anarchy-AI Plugin

> This README is generated from `docs/ANARCHY_AI_PLUGIN_README_SOURCE.md` by `harness/setup/scripts/build-self-contained-exe.ps1`.
> Keep install-story prose authored here so the published plugin bundle stays destination-relative and honest.

## Purpose

This plugin is the user-facing delivery surface for Anarchy-AI, the runtime framework harness for the AGENTS Heuristic Underlay.

The current architecture is:

- schema family = canonical layer
- AGENTS Heuristic Underlay = operative layer built from that family
- Anarchy-AI = runtime framework harness

## Repo-Authored Published Truth

All carried schema-family artifacts, contracts, docs, disclaimers, and install assertions remain authored in this repo.

The standalone installer is a published carrier:

- it publishes the repo-authored plugin bundle into the setup payload
- it resolves install paths relative to the destination at install time
- it does not become an alternate source of harness truth

The published plugin bundle therefore keeps destination-relative paths such as:

- repo-local installed plugin root: `.\plugins\anarchy-ai-<repo-slug>-<stable-path-hash>`
- user-profile installed plugin root: `~\.codex\plugins\anarchy-ai`
- personal marketplace path: `~\.agents\plugins\marketplace.json`
- personal marketplace `source.path`: `./.codex/plugins/anarchy-ai`

This README should never teach source-repo-relative install paths or up-level source checkout hops.

## What It Connects

- plugin layer:
  - this directory
- bundled contracts:
  - `./contracts/`
- bundled runtime:
  - `./runtime/win-x64/`
- bundled canonical schemas:
  - `./schemas/`
- bundled skill:
  - `./skills/anarchy-ai-harness/`

## Current Delivery Scope

The packaged plugin delivery is Windows-first today.

That means:

- the bundled runtime is `./runtime/win-x64/AnarchyAi.Mcp.Server.exe`
- `.mcp.json` launches that bundled executable directly
- `start-anarchy-ai.cmd` is a development helper and fallback path, not the primary install story

The broader harness contract may later travel farther than the current packaged runtime, but the present bundle should be described honestly as Windows-first.

## Current Delivery Story

The plugin provides:

- a preferred single-file installer at `../AnarchyAi.Setup.exe` after that installer is generated locally by the build helper
- a local MCP declaration through `.mcp.json`
- direct launch of the bundled `.NET 8` self-contained single-file runtime inside the plugin
- a bundled canonical schema family plus hash manifest under `./schemas/`
- a skill that teaches when to use the five bounded core runtime tools and how to discover the experimental `direction_assist_test` module
- a repo-bootstrap script at `./scripts/bootstrap-anarchy-ai.ps1` as a compatibility/fallback lane for repo-local install, assess, and bundle refresh after the bundle already exists
- a runtime lock script at `./scripts/stop-anarchy-ai.ps1` for assessing, safely releasing, or forcibly releasing the bundled Anarchy-AI runtime lock

The repo-local launcher script is retained only as a development helper during source work. It is not the intended packaged delivery path.

The plugin does not yet add a custom UI panel or settings page.

The setup executable help and disclosure surfaces are intentionally generated from current installer facts so an agent or user can see:

- what destination-relative surfaces install will create
- which lane is being chosen:
  - `repo-local`
  - `user-profile`
- what install will and will not change

For Codex, the primary user-profile lane is the plugin marketplace lane:

- plugin bundle under `~\.codex\plugins\anarchy-ai`
- personal marketplace at `~\.agents\plugins\marketplace.json`
- personal marketplace `source.path` of `./.codex/plugins/anarchy-ai`

The older custom `mcp_servers.anarchy-ai` block is no longer the primary Codex home-install truth.
Treat it as an optional fallback/debug surface only.

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

Together, these tools give Anarchy-AI its current runtime promise:

- preflight meaningful governed work before the agent proceeds
- compile active work into bounded operational state
- evaluate whether the schema package is materially real here
- assess install, runtime, schema, and adoption gaps explicitly
- reconcile local drift or partial materialization without replacing schema authorship

Experimental test-lane addition:

- `direction_assist_test` qualifies long direction text using bounded linguistic findings, returns cleaned direction plus fixed two-choice output, and appends local test telemetry.
- it is explicitly test-lane and does not change default core tool order.

The plugin bundle currently carries:

- contracts
- runtime
- canonical schemas
- schema bundle manifest
- skill

The plugin bundle can refresh canonical schema-family files from its own carried schema bundle.
The bootstrap script can refresh the plugin bundle from a public zip source or from a local source path when machine trust or TLS is unreliable.

## Runtime Lock Repair

The bootstrap refresh path does not replace a live bundled runtime in place.
Stop the running `AnarchyAi.Mcp.Server.exe` process before retrying an update that touches `runtime/win-x64/AnarchyAi.Mcp.Server.exe`.

The dedicated runtime-lock commands are:

- assess runtime lock:
  - `powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\stop-anarchy-ai.ps1 -Mode AssessRuntimeLock`
- safe release runtime lock:
  - `powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\stop-anarchy-ai.ps1 -Mode SafeReleaseRuntimeLock`
- force release runtime lock:
  - `powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\stop-anarchy-ai.ps1 -Mode ForceReleaseRuntimeLock`

`<installed-plugin-root>` is either:

- `.\plugins\anarchy-ai-<repo-slug>-<stable-path-hash>`
- `~\.codex\plugins\anarchy-ai`

`SafeReleaseRuntimeLock` does not request UAC elevation.

`ForceReleaseRuntimeLock` tries once normally, then retries once through a UAC elevation prompt if the release fails with access denied.

The safe/force split is intentional for both humans and agents:

- it gives the actor a bounded repair option before resorting to force behavior
- it makes the cause legible when a live runtime lock is blocking update
- it gives the agent stronger direction about the problem before it reaches for broader file or path manipulation

## Current Boundaries

- The plugin bundle still does not invent repo-authored governed files such as `AGENTS-hello.md`, `AGENTS-Terms.md`, `AGENTS-Vision.md`, or `AGENTS-Rules.md`.
- The preferred current first install lane is `../AnarchyAi.Setup.exe`.
- In the `AI-Links` source repo, that setup executable is a generated artifact, not a tracked file.
- The current overall install posture supports:
  - repo-local install
  - user-profile install
- It is still not machine-wide or device-local.

