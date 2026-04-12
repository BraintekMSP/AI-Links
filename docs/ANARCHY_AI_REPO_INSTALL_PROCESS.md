# Anarchy-AI Repo Installation Process

## Purpose

This document defines the exact current repo-local installation process for bringing Anarchy-AI into another repository.

This is the current real delivery path.
It is repo-local, not machine-level install.
It is the current process that makes the harness:

- delivered
- accessible
- enforced enough to matter
- real enough to trust

Current reality:

- packaged delivery is Windows-first
- Codex is the current first-class packaged host
- Claude shares the contract model and MCP direction, but does not yet have an equivalent packaged adapter in this repo
- Cursor is not a first-class install target yet

## Preferred current delivery surface

The preferred first-delivery surface is one file:

- `plugins/AnarchyAi.Setup.exe`

In the `AI-Links` source repo, that installer should be generated first with:

```powershell
powershell -ExecutionPolicy Bypass -File .\harness\setup\scripts\build-self-contained-exe.ps1
```

That file now handles:

- plugin bundle materialization into `plugins/anarchy-ai/`
- repo-local marketplace registration
- readiness assessment
- bundle refresh from local source path or public source url
- default seeding of missing portable schema-family files during install
- explicit root schema-family refresh when requested

The older script-first lane still exists, but it is now the compatibility/fallback path after the bundle already exists.

## What gets delivered after setup runs

### 1. Harness plugin bundle

The setup executable materializes:

`plugins/anarchy-ai/`

That bundle contains:

- `.codex-plugin/plugin.json`
- `.mcp.json`
- `contracts/`
- `runtime/win-x64/AnarchyAi.Mcp.Server.exe`
- `schemas/`
- `skills/anarchy-ai-harness/SKILL.md`
- `scripts/bootstrap-anarchy-ai.ps1`
- `scripts/start-anarchy-ai.cmd`
- `scripts/stop-anarchy-ai.ps1`
- plugin trust and asset files

### 2. Marketplace registration

Setup also creates or updates:

`./.agents/plugins/marketplace.json`

It enforces this plugin entry shape:

```json
{
  "name": "anarchy-ai",
  "source": {
    "source": "local",
    "path": "./plugins/anarchy-ai"
  },
  "policy": {
    "installation": "INSTALLED_BY_DEFAULT",
    "authentication": "ON_INSTALL"
  },
  "category": "Productivity"
}
```

### 3. Portable root schema family

If the target repo is adopting the AGENTS Heuristic Underlay, setup now seeds missing portable schema-family files into repo root during install by default.

If those repo-root schema files already exist, install leaves them in place.

Use explicit schema refresh only when you want the embedded portable schema family to overwrite repo-root copies.

That set is:

- `AGENTS-schema-governance.json`
- `AGENTS-schema-1project.json`
- `AGENTS-schema-narrative.json`
- `AGENTS-schema-gov2gov-migration.json`
- `AGENTS-schema-triage.md`
- `Getting-Started-For-Humans.txt`

Do not confuse harness delivery with schema reality.
Copying or refreshing schema files alone does not make the system materially governed.

## Exact installation steps in another repo

Assume:

- source repo = the repo that already carries Anarchy-AI
- target repo = the repo you want to equip

### Step 1. Copy the setup executable

Generate and then copy this file from the source repo into the target repo:

`plugins/AnarchyAi.Setup.exe`

Place it here:

`./plugins/AnarchyAi.Setup.exe`

### Step 2. Run install

From the target repo root, run:

```powershell
.\plugins\AnarchyAi.Setup.exe /install
```

Or double-click:

`./plugins/AnarchyAi.Setup.exe`

Then use the simple installer UI.

Result required:

- `./plugins/anarchy-ai/.codex-plugin/plugin.json` exists
- `./plugins/anarchy-ai/.mcp.json` exists
- `./plugins/anarchy-ai/runtime/win-x64/AnarchyAi.Mcp.Server.exe` exists
- `./plugins/anarchy-ai/contracts/` contains all current contract files
- `./plugins/anarchy-ai/skills/anarchy-ai-harness/SKILL.md` exists
- `./plugins/anarchy-ai/scripts/bootstrap-anarchy-ai.ps1` exists
- `./.agents/plugins/marketplace.json` contains the `anarchy-ai` entry

### Step 3. Verify readiness

Run:

```powershell
.\plugins\AnarchyAi.Setup.exe /assess
```

Expected good result shape:

- `bootstrap_state = ready`
- `runtime_present = true`
- `marketplace_registered = true`
- `installed_by_default = true`
- `next_action = use_preflight_session`

If that result is not reached, the harness is present but not yet accessible enough to count as installed.
It is just partially delivered files.

### Step 4. Default schema seeding and optional schema refresh

Plain install now seeds missing portable schema-family files by default.

If you want install to overwrite repo-root schema files from the embedded portable schema family, run:

```powershell
.\plugins\AnarchyAi.Setup.exe /install /refreshschemas
```

### Step 5. Refresh the delivered bundle when needed

Preferred current refresh commands:

Refresh from a local source path:

```powershell
.\plugins\AnarchyAi.Setup.exe /update /sourcepath "C:\path\to\AI-Links"
```

Refresh from the configured public source:

```powershell
.\plugins\AnarchyAi.Setup.exe /update
```

Refresh the plugin bundle and root portable schema family together:

```powershell
.\plugins\AnarchyAi.Setup.exe /update /refreshschemas /sourcepath "C:\path\to\AI-Links"
```

Current update behavior:

- refreshes the local plugin bundle surfaces in `./plugins/anarchy-ai/`
- seeds missing portable root schema files during install by default
- force-refreshes the root portable schema family only when `/refreshschemas` is passed
- returns bounded update state in the JSON result
- cannot replace a running `AnarchyAi.Mcp.Server.exe` in place; the active runtime must be stopped before retrying an update that touches the bundled runtime

Useful result fields:

- `update_requested`
- `update_state`
- `update_source_zip_url`
- `update_source_path`
- `update_notes`

Expected success result:

- `update_state = completed`

Expected failure result when the machine cannot reach or trust the public source:

- `update_state = failed`
- `missing_components` contains `update_pull_failed`
- `safe_repairs` contains `verify_public_repo_access_and_retry_update`

Expected failure result when the local bundle runtime is currently running:

- `update_state = failed`
- `missing_components` contains `update_pull_failed`
- `safe_repairs` contains `release_runtime_lock_and_retry_update`
- `safe_repairs` may also contain:
  - `run_safe_release_runtime_lock`
  - `run_force_release_runtime_lock`

Assess whether the runtime lock is still live with:

```powershell
powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\stop-anarchy-ai.ps1 -Mode AssessRuntimeLock
```

Try a safe runtime-lock release with no UAC elevation:

```powershell
powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\stop-anarchy-ai.ps1 -Mode SafeReleaseRuntimeLock
```

Force runtime-lock release with one UAC-backed retry on access denied:

```powershell
powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\stop-anarchy-ai.ps1 -Mode ForceReleaseRuntimeLock
```

## Compatibility and fallback lane

The script-first lane still exists after bundle materialization.

Use it when:

- source work is happening inside the plugin bundle itself
- a repo already carries `plugins/anarchy-ai/`
- you need the existing PowerShell bootstrap semantics specifically

Fallback commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\bootstrap-anarchy-ai.ps1 -Mode Install
powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\bootstrap-anarchy-ai.ps1 -Mode Assess
powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\bootstrap-anarchy-ai.ps1 -Mode Assess -Update -UpdateSourcePath 'C:\path\to\AI-Links'
```

## How to make the system accessible

Accessible means the host and agent can actually reach the harness without ceremony.

For the current Codex-first path, accessibility requires all of the following:

- `plugins/AnarchyAi.Setup.exe` or the already materialized plugin bundle is present
- plugin bundle exists in `./plugins/anarchy-ai`
- marketplace entry exists in `./.agents/plugins/marketplace.json`
- policy is `INSTALLED_BY_DEFAULT`
- bundled runtime exists
- `.mcp.json` points at the bundled runtime
- skill exists in the plugin bundle
- setup or bootstrap assessment returns `ready`

If any of those are missing, the harness is not yet accessible enough to behave like a harness.
It is just partially delivered files.

## How to make the system enforced enough to matter

Installed is not the same as enforced.

For the current architecture, enforcement means the harness changes how work begins.

### 1. Agent startup expectation

The target repo should treat this as the default rule:

- meaningful governed work starts with `preflight_session`

That is the current harness posture.
Without it, the harness remains optional utilities.

### 2. Target repo startup or control-plane instructions

To make that rule operational, add a direction in the target repo's startup surface or control-plane prompt packet that says the agent should:

- run `preflight_session` before meaningful governed work
- use `assess_harness_gap_state` when install/runtime/schema/adoption state is unclear
- use `is_schema_real_or_shadow_copied` before trusting copied schema presence

### 3. Keep the plugin installed by default

Do not downgrade the marketplace policy to `AVAILABLE` if the goal is harness behavior.

`AVAILABLE` means:

- the harness may exist
- the host may not actually present it to the agent at startup

`INSTALLED_BY_DEFAULT` is the current repo-local policy that makes the harness present enough to matter.

## How to make the system real

Real means more than installed and reachable.

A real harness deployment must satisfy both harness reality and schema reality.

### A. Harness reality

The harness is real in the target repo when:

- repo install/assess returns `ready`
- bundled runtime is present
- plugin is installed by default
- preflight is callable
- gap assessment is callable

### B. Schema reality

If the target repo is using the schema family, schema reality must also be true.

That means copied schema files alone are not enough.
The repo must be materially governed.

Use:

- `is_schema_real_or_shadow_copied`
- `assess_harness_gap_state`
- `preflight_session`

Expected stable state for a governed repo:

- `schema_reality_state = real`
- `integrity_state = aligned`
- `possession_state = unpossessed`
- `adoption_state = fully_adopted` or at minimum `partially_adopted` with named gaps being actively resolved

If the schema package is only:

- `partial`
- `copied_only`
- `possessed`

then the repo is not yet real enough to trust as governed.

### C. Gov2gov when needed

If the target repo already has existing authority surfaces, do not just copy schemas and call it complete.

Use:

- `run_gov2gov_migration`

in `plan_only` first, then `non_destructive_apply` only when appropriate.

That is how the harness helps make a copied or drifted package real without silently overwriting local truth.

## Full adoption definition for another repo

A target repo should only be considered fully adopted when all of the following are true:

- `plugins/AnarchyAi.Setup.exe` has been used or the equivalent plugin bundle has already been materialized
- `plugins/anarchy-ai/` is present
- `.agents/plugins/marketplace.json` contains the `anarchy-ai` entry
- installation policy is `INSTALLED_BY_DEFAULT`
- bundled runtime exists
- assess returns `ready`
- `preflight_session` is callable
- `assess_harness_gap_state` is callable
- the repo's agent-facing startup/control-plane direction expects preflight-first for meaningful governed work
- if the schema family is in use, the schema package is materially `real`, not merely copied

## Current limitations you should state honestly

This process is current and real, but it is not the final install architecture.

Current limitations:

- this is repo-local install, not machine-level install
- packaged delivery is Windows-first
- Claude does not yet have an equivalent packaged adapter in this repo
- Cursor is not yet a first-class delivery target
- host-native install suggestion chips are not part of the guaranteed install story
- GUI mode currently covers `Assess` and `Install`, not GUI `Update`
- reflection (`assess the last exchange and do better`) is still a secondary workflow, not a first-class install target
- public update depends on outbound access to the configured source zip and a working local trust/TLS path
- local-source update is the safer fallback when public HTTPS is unreliable on the machine
- update does not hot-swap a running bundled runtime; stop the active Anarchy-AI process first if the update needs to replace `runtime/win-x64/AnarchyAi.Mcp.Server.exe`

## Minimum checklist for another repo

1. Copy `plugins/AnarchyAi.Setup.exe` into the target repo `plugins/` folder.
2. Run `AnarchyAi.Setup.exe /install` or double-click it.
3. Require install to provision or update `.agents/plugins/marketplace.json` with `INSTALLED_BY_DEFAULT`.
4. Run `AnarchyAi.Setup.exe /assess` and require `bootstrap_state = ready`.
5. Install now seeds missing portable schema-family files by default; use `/refreshschemas` only when repo-root schema copies should be overwritten from the embedded payload.
6. Use `AnarchyAi.Setup.exe /update` when the carried bundle needs to be refreshed.
7. Add a startup/control-plane instruction that meaningful governed work starts with `preflight_session`.
8. Verify schema reality before trusting copied schema presence.
9. Use gov2gov planning where existing authority surfaces must be reconciled.

If those nine things are not true, the system is not yet fully delivered, accessible, enforced, and real in the target repo.
