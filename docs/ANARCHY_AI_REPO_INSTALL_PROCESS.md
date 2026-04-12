# Anarchy-AI Repo Installation Process

## Purpose

This document defines the exact current repo-bootstrap installation process for bringing Anarchy-AI into another repository.

This is the current real delivery path.
It is not the future machine-level installer path.
It is the repo-local process that makes the harness:

- delivered
- accessible
- enforced enough to matter
- real enough to trust

Current reality:

- packaged delivery is Windows-first
- Codex is the current first-class packaged host
- Claude shares the contract model and MCP direction, but does not yet have an equivalent packaged adapter in this repo
- Cursor is not a first-class install target yet

## What gets delivered

For a target repo, delivery means copying these surfaces into that repo.

### 1. Harness plugin bundle

Copy the entire directory:

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
- plugin trust and asset files

### 2. Marketplace registration

Marketplace registration is still required, but it no longer needs to be manual.

The current repo-bootstrap script can:

- create `./.agents/plugins/`
- create `./.agents/plugins/marketplace.json` if it does not exist
- add or update the `anarchy-ai` plugin entry
- enforce `INSTALLED_BY_DEFAULT`

This is what makes the plugin present by default instead of merely available.

### 3. Schema family when the target repo is adopting the underlay

If the target repo is adopting the AGENTS Heuristic Underlay, also copy the portable deployment set into the repo root:

- `AGENTS-schema-governance.json`
- `AGENTS-schema-1project.json`
- `AGENTS-schema-narrative.json`
- `AGENTS-schema-gov2gov-migration.json`
- `AGENTS-schema-triage.md`
- `Getting-Started-For-Humans.txt`

Do not confuse harness delivery with schema reality.
Copying the schema family alone does not make the system real.

## Exact installation steps in another repo

Assume:

- source repo = the repo that already carries Anarchy-AI
- target repo = the repo you want to equip

### Step 1. Copy the plugin bundle

Copy `plugins/anarchy-ai/` from the source repo into the target repo at the same relative path.

Result required:

- `./plugins/anarchy-ai/.codex-plugin/plugin.json` exists
- `./plugins/anarchy-ai/.mcp.json` exists
- `./plugins/anarchy-ai/runtime/win-x64/AnarchyAi.Mcp.Server.exe` exists
- `./plugins/anarchy-ai/contracts/` contains all current contract files
- `./plugins/anarchy-ai/skills/anarchy-ai-harness/SKILL.md` exists
- `./plugins/anarchy-ai/scripts/bootstrap-anarchy-ai.ps1` exists

### Step 2. Run repo bootstrap install

From the target repo root, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\bootstrap-anarchy-ai.ps1 -Mode Install
```

This is the current first install lane.

What it does now:

- checks bundled runtime presence
- checks plugin manifest presence
- checks MCP declaration presence
- checks skill presence
- checks schema bundle manifest presence
- checks contract presence
- creates `./.agents/plugins/` when missing
- creates `./.agents/plugins/marketplace.json` when missing
- creates or updates the repo-local marketplace entry for `anarchy-ai`
- enforces `INSTALLED_BY_DEFAULT`
- returns a bounded bootstrap result

Current exact plugin entry shape after bootstrap:

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

### Step 3. Verify bootstrap state

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\bootstrap-anarchy-ai.ps1 -Mode Assess
```

Expected good result shape:

- `bootstrap_state = ready`
- `runtime_present = true`
- `marketplace_registered = true`
- `installed_by_default = true`
- `next_action = use_preflight_session`

If that result is not reached, the harness is present but not yet accessible enough to count as installed.

### Step 4. Refresh the delivered bundle when needed

If the source repository is public and the target repo already carries `plugins/anarchy-ai/`, bootstrap can refresh the local Anarchy-AI bundle from the published repository zip.

Refresh the plugin bundle only:

```powershell
powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\bootstrap-anarchy-ai.ps1 -Mode Assess -Update
```

Refresh the plugin bundle and also force refresh the portable schema family at repo root:

```powershell
powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\bootstrap-anarchy-ai.ps1 -Mode Assess -Update -RefreshPortableSchemaFamily
```

Current `-Update` behavior:

- downloads the current repo zip from `UpdateSourceZipUrl`
- refreshes the local plugin bundle surfaces in `./plugins/anarchy-ai/`
- refreshes the root portable schema family only when `-RefreshPortableSchemaFamily` is passed
- re-runs bundle presence checks after refresh
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

`SafeReleaseRuntimeLock` does not request elevation.
`ForceReleaseRuntimeLock` tries once normally, then retries once through UAC if the normal release fails with access denied.

That split is intentional:

- it gives the user a visible choice before a UAC-backed force action
- it gives the agent a bounded repair lane before it reaches for broader file or path manipulation
- it creates stronger feedback about the actual problem: a live runtime lock is blocking update, not a missing file or a broken bundle

The update source can be overridden when needed:

```powershell
powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\bootstrap-anarchy-ai.ps1 -Mode Assess -Update -UpdateSourceZipUrl 'https://your-host/AI-Links-main.zip'
```

The most reliable fallback when Windows trust or Schannel is damaged is a local source path:

```powershell
powershell -ExecutionPolicy Bypass -File .\plugins\anarchy-ai\scripts\bootstrap-anarchy-ai.ps1 -Mode Assess -Update -UpdateSourcePath 'C:\path\to\AI-Links'
```

`-UpdateSourcePath` may point to:

- a local repository directory
- a local zip file
- a mounted file-share path that resolves locally

## How to make the system accessible

Accessible means the host and agent can actually reach the harness without ceremony.

For the current Codex-first path, accessibility requires all of the following:

- plugin bundle exists in `./plugins/anarchy-ai`
- marketplace entry exists in `./.agents/plugins/marketplace.json`
- policy is `INSTALLED_BY_DEFAULT`
- bundled runtime exists
- `.mcp.json` points at the bundled runtime
- skill exists in the plugin bundle
- bootstrap assessment returns `ready`

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

This does not require the user to operate the harness manually.
It means the repo's own agent instructions expect the harness to be used.

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

- repo bootstrap returns `ready`
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

- `plugins/anarchy-ai/` is present
- `.agents/plugins/marketplace.json` contains the `anarchy-ai` entry
- installation policy is `INSTALLED_BY_DEFAULT`
- bundled runtime exists
- bootstrap assess returns `ready`
- `preflight_session` is callable
- `assess_harness_gap_state` is callable
- the repo's agent-facing startup/control-plane direction expects preflight-first for meaningful governed work
- if the schema family is in use, the schema package is materially `real`, not merely copied

## Current limitations you should state honestly

This process is current and real, but it is not the final install architecture.

Current limitations:

- this is repo bootstrap, not machine-level install
- packaged delivery is Windows-first
- Claude does not yet have an equivalent packaged adapter in this repo
- Cursor is not yet a first-class delivery target
- host-native install suggestion chips are not part of the guaranteed install story
- reflection (`assess the last exchange and do better`) is still a secondary workflow, not a first-class install target
- `-Update` depends on outbound access to the configured source zip and a working local trust/TLS path
- local-source update is the safer fallback when public HTTPS is unreliable on the machine
- `-Update` does not hot-swap a running bundled runtime; stop the active Anarchy-AI process first if the update needs to replace `runtime/win-x64/AnarchyAi.Mcp.Server.exe`

## Minimum checklist for another repo

1. Copy `plugins/anarchy-ai/` into the target repo.
2. Run `bootstrap-anarchy-ai.ps1 -Mode Install`.
3. Require bootstrap to provision or update `.agents/plugins/marketplace.json` with `INSTALLED_BY_DEFAULT`.
4. Run `bootstrap-anarchy-ai.ps1 -Mode Assess` and require `bootstrap_state = ready`.
5. If using the underlay, copy the portable schema family into the target repo root.
6. Use `-Update` when the carried bundle needs to be refreshed from the public source repo.
7. Add a startup/control-plane instruction that meaningful governed work starts with `preflight_session`.
8. Verify schema reality before trusting copied schema presence.
9. Use gov2gov planning where existing authority surfaces must be reconciled.

If those nine things are not true, the system is not yet fully delivered, accessible, enforced, and real in the target repo.
