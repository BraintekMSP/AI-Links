# Anarchy-AI Repo Installation Process

## Purpose

This document defines the exact current installation process for bringing Anarchy-AI into another repository.

This is the current real delivery path — a repo-bootstrap install.
Machine-level install is future work.
This process makes the harness:

- delivered
- accessible
- enforced enough to matter
- real enough to trust

Current reality:

- packaged delivery is Windows-first
- Codex is the current first-class packaged host
- Claude shares the contract model and MCP direction; packaged adapter is future work
- Cursor is future work for first-class install targeting
- environment truth separation for Codex install/mount behavior is tracked in:
  - `ANARCHY_AI_ENVIRONMENT_TRUTH_MATRIX.md`

## Preferred current delivery surface

The preferred first-delivery surface is one file:

- `plugins/AnarchyAi.Setup.exe`

In the `AI-Links` source repo, that installer should be generated first with:

```powershell
powershell -ExecutionPolicy Bypass -File .\harness\setup\scripts\build-self-contained-exe.ps1
```

That file now handles:

- plugin bundle materialization into either:
  - a repo-scoped `plugins/anarchy-ai-herringms-<repo-slug>-<stable-path-hash>/`
  - or a Codex user-profile `~/.codex/plugins/anarchy-ai-herringms/`
- matching marketplace registration in either:
  - `./.agents/plugins/marketplace.json`
  - or `~/.agents/plugins/marketplace.json`
- readiness assessment
- bundle refresh from local source path or public source url
- default seeding of missing portable schema-family files during install
- explicit root schema-family refresh when requested
- Codex-native personal plugin registration through `~/.agents/plugins/marketplace.json`

The older script-first lane still exists, but it is now the compatibility/fallback path after the bundle already exists.

All carried schema-family artifacts, contracts, docs, disclaimers, and install assertions remain authored in the repo and are published into the standalone installer payload from that repo-authored source.
Installed links and paths must describe the destination, not the source checkout layout.

Important targeting rule:

- automatic repo detection trusts only a real repo marker (currently `.git`)
- a generic parent folder containing `plugins/` or `.agents/` alone requires explicit `/repo` confirmation
- when auto-detection is ambiguous, run setup with `/repo`

## What gets delivered after setup runs

### 1. Harness plugin bundle

The setup executable materializes one of these plugin roots:

- repo-local:
  - `plugins/anarchy-ai-herringms-<repo-slug>-<stable-path-hash>/`
- user-profile:
  - `~/.codex/plugins/anarchy-ai-herringms/`

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
- `scripts/remove-anarchy-ai.ps1`
- plugin trust and asset files

### 2. Marketplace registration

Setup also creates or updates one of these marketplace roots:

- repo-local:
  - `./.agents/plugins/marketplace.json`
- user-profile:
  - `~/.agents/plugins/marketplace.json`

The plugin identity rule is now split on purpose:

- repo-local installs use repo-scoped plugin identity — one repo-local uninstall action in Codex keeps its effect contained to that repo instead of colliding with sibling repo installs
- user-profile installs use one stable user-profile plugin identity because the install root is intentionally shared for that user
- both lanes keep the plugin-local MCP server key stable as `anarchy-ai-herringms`

Current repo-local shape:

- `name = anarchy-ai-herringms-local-<repo-slug>`
- `interface.displayName = Anarchy-AI Local (<RepoName>)`
- `plugins.<entry>.name = anarchy-ai-herringms-<repo-slug>-<stable-path-hash>`
- `.codex-plugin/plugin.json -> name = anarchy-ai-herringms-<repo-slug>-<stable-path-hash>`
- `.mcp.json -> mcpServers -> anarchy-ai-herringms`

Current user-profile shape:

- `name = anarchy-ai-herringms-user-profile`
- `interface.displayName = Anarchy-AI User Profile`
- `plugins.<entry>.name = anarchy-ai-herringms`
- `plugins.<entry>.source.path = ./.codex/plugins/anarchy-ai-herringms`
- `.codex-plugin/plugin.json -> name = anarchy-ai-herringms`
- `.mcp.json -> mcpServers -> anarchy-ai-herringms`
- Codex home readiness is plugin-marketplace-first; a custom `mcp_servers.anarchy-ai-herringms` block is optional fallback/debug only
- older legacy `mcp_servers.anarchy-ai` blocks are cleanup evidence only

Marketplace naming note:

- official Codex plugin docs describe `interface.displayName` as the marketplace title shown in Codex
- current Codex plugin surfaces can still expose the top-level marketplace `name`
- keep that `name` branded and stable across devices rather than leaking a path-derived hash

It enforces this plugin entry shape:

```json
{
  "name": "anarchy-ai-herringms-<repo-slug>-<stable-path-hash>",
  "source": {
    "source": "local",
    "path": "./plugins/anarchy-ai-herringms-<repo-slug>-<stable-path-hash>"
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

Keep harness delivery separate from schema reality.
Material governance requires the harness to be installed and enforced — copied schema files alone are delivery evidence, governance requires the harness running against them.

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
.\plugins\AnarchyAi.Setup.exe /install /repolocal
```

Or install into the current user profile:

```powershell
.\plugins\AnarchyAi.Setup.exe /install /userprofile
```

Or double-click:

`./plugins/AnarchyAi.Setup.exe`

Then use the simple installer UI.

Result required:

- repo-local:
  - `./plugins/anarchy-ai-herringms-<repo-slug>-<stable-path-hash>/.codex-plugin/plugin.json` exists
  - `./plugins/anarchy-ai-herringms-<repo-slug>-<stable-path-hash>/.mcp.json` exists
  - `./.agents/plugins/marketplace.json` contains the repo-scoped Anarchy-AI plugin entry
- user-profile:
  - `~/.codex/plugins/anarchy-ai-herringms/.codex-plugin/plugin.json` exists
  - `~/.codex/plugins/anarchy-ai-herringms/.mcp.json` exists
  - `~/.agents/plugins/marketplace.json` contains the `anarchy-ai-herringms` plugin entry
- both lanes:
  - the bundled runtime exists
  - `contracts/` contains all current contract files
  - `skills/anarchy-ai-harness/SKILL.md` exists

### Step 3. Verify readiness

Run:

```powershell
.\plugins\AnarchyAi.Setup.exe /assess /repolocal
```

Or:

```powershell
.\plugins\AnarchyAi.Setup.exe /assess /userprofile
```

Expected good result shape:

- `bootstrap_state = ready`
- `runtime_present = true`
- `marketplace_registered = true`
- `installed_by_default = true`
- `next_action = use_preflight_session`
- `install_scope = repo_local|user_profile`
- `paths.destination` is present and carries the target plugin/runtime/marketplace file paths
- for default `/userprofile` with no `/repo`:
  - `paths.destination.root_path = <current-user-profile>`
  - portable schema seeding stays out of scope

Any other result shape means the harness is partially delivered — files present, accessibility incomplete. Count installation as complete only when the expected good result shape is reached.

Install lock behavior:

- if install runs while one or more existing user-profile bundle files are locked by another process, setup can return:
  - `bootstrap_state = registration_refresh_needed`
  - `missing_components` contains `locked_bundle_surface_write_skipped`
  - `next_action = release_runtime_lock_and_retry_install`
- in that case, run runtime-lock release and retry install:
  - `SafeReleaseRuntimeLock` first
  - `ForceReleaseRuntimeLock` only if needed

### Step 4. Default schema seeding and optional schema refresh

Plain install now seeds missing portable schema-family files by default.

Scope note:

- `/repolocal` seeds into the selected repo root.
- `/userprofile` seeds only when `/repo "<path>"` is provided explicitly.
- default `/userprofile` with blank `/repo` keeps portable schema seeding out of scope, reports `portable_schema_family_not_targeted`, and still returns `paths.destination` for the user-profile target.

If you want install to overwrite repo-root schema files from the embedded portable schema family, run:

```powershell
.\plugins\AnarchyAi.Setup.exe /install /repolocal /refreshschemas
```

### Step 5. Refresh the delivered bundle when needed

Preferred current refresh commands:

Refresh from a local source path:

```powershell
.\plugins\AnarchyAi.Setup.exe /update /repolocal /sourcepath "C:\path\to\AI-Links"
```

Refresh from the configured public source:

```powershell
.\plugins\AnarchyAi.Setup.exe /update /repolocal
```

Refresh the plugin bundle and root portable schema family together:

```powershell
.\plugins\AnarchyAi.Setup.exe /update /userprofile /repo "C:\path\to\target-repo" /refreshschemas /sourcepath "C:\path\to\AI-Links"
```

Current update behavior:

- refreshes the selected plugin bundle surfaces in either:
  - repo-local `./plugins/anarchy-ai-herringms-<repo-slug>-<stable-path-hash>/`
  - user-profile `~/.codex/plugins/anarchy-ai-herringms/`
- seeds missing portable root schema files during install by default
- force-refreshes the root portable schema family only when `/refreshschemas` is passed
- returns bounded update state in the JSON result
- replacing a running `AnarchyAi.Mcp.Server.exe` in place requires stopping the active runtime first — update the bundled runtime only after release

Useful result fields:

- `update_requested`
- `update_state`
- `update_source_zip_url`
- `update_notes`
- `paths.source`
- `paths.destination`

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
powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\stop-anarchy-ai.ps1 -Mode AssessRuntimeLock
```

Try a safe runtime-lock release with no UAC elevation:

```powershell
powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\stop-anarchy-ai.ps1 -Mode SafeReleaseRuntimeLock
```

Force runtime-lock release with one UAC-backed retry on access denied:

```powershell
powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\stop-anarchy-ai.ps1 -Mode ForceReleaseRuntimeLock
```

Where `<installed-plugin-root>` is one of:

- repo-local:
  - `.\plugins\anarchy-ai-herringms-<repo-slug>-<stable-path-hash>`
- user-profile:
  - `~\.codex\plugins\anarchy-ai-herringms`

If Anarchy-AI needs to be retired cleanly instead of repaired, use the dedicated retirement script rather than ad hoc file deletion:

```powershell
powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\remove-anarchy-ai.ps1 -Mode Assess
```

```powershell
powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\remove-anarchy-ai.ps1 -Mode Quarantine -Targets repo_local,user_profile,device_app
```

```powershell
powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\remove-anarchy-ai.ps1 -Mode Remove -Targets repo_local,user_profile,device_app
```

That script inventories first, preserves repo-authored source truth, backs up mutable files before rewrite, rewrites shared marketplace files in place to remove only Anarchy-AI entries, clears owned optional custom-MCP fallback blocks such as `mcp_servers.anarchy-ai-herringms` and older legacy `mcp_servers.anarchy-ai`, and retires the documented plugin-cache lane without guessing at broader Codex-private app databases.

## Compatibility and fallback lane

The script-first lane still exists after bundle materialization.

Current rule:

- the PowerShell bootstrap script remains the repo-local compatibility surface
- user-profile installs should use `AnarchyAi.Setup.exe` for assess, install, and update until the fallback script is widened intentionally

Use it when:

- source work is happening inside the plugin bundle itself
- a repo already carries a repo-local Anarchy-AI plugin bundle
- you need the existing PowerShell bootstrap semantics specifically

Fallback commands:

```powershell
powershell -ExecutionPolicy Bypass -File <repo-local-plugin-root>\scripts\bootstrap-anarchy-ai.ps1 -Mode Install
powershell -ExecutionPolicy Bypass -File <repo-local-plugin-root>\scripts\bootstrap-anarchy-ai.ps1 -Mode Assess
powershell -ExecutionPolicy Bypass -File <repo-local-plugin-root>\scripts\bootstrap-anarchy-ai.ps1 -Mode Assess -Update -UpdateSourcePath 'C:\path\to\AI-Links'
```

Where `<repo-local-plugin-root>` is:

- `.\plugins\anarchy-ai-herringms-<repo-slug>-<stable-path-hash>`

## How to make the system accessible

Accessible means the host and agent can actually reach the harness without ceremony.

For the current Codex-first path, accessibility requires all of the following:

- `plugins/AnarchyAi.Setup.exe` or the already materialized plugin bundle is present
- plugin bundle exists in either:
  - `./plugins/anarchy-ai-herringms-<repo-slug>-<stable-path-hash>`
  - `~/.codex/plugins/anarchy-ai-herringms`
- marketplace entry exists in either:
  - `./.agents/plugins/marketplace.json`
  - `~/.agents/plugins/marketplace.json`
- when using user-profile lane with Codex:
  - `~/.agents/plugins/marketplace.json` points at `./.codex/plugins/anarchy-ai-herringms`
  - `~/.codex/plugins/anarchy-ai-herringms` contains the bundled runtime and plugin surfaces
  - `~/.codex/config.toml` custom MCP registration is optional fallback/debug only and is not required for `ready`
- policy is `INSTALLED_BY_DEFAULT`
- bundled runtime exists
- `.mcp.json` points at the bundled runtime
- skill exists in the plugin bundle
- setup or bootstrap assessment returns `ready`

When any of those are missing, the harness remains partially delivered — files present, accessibility incomplete. Count the harness as accessible only when every item above is satisfied.

## How to make the system enforced enough to matter

Installed and enforced are separate states. Installed covers delivery and accessibility; enforced covers behavior change.

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

A paste-ready block that satisfies this requirement lives in:

- `templates/ANARCHY_AI_STARTUP_INSTRUCTION_TEMPLATE.md`

Drop it into the target repo's `AGENTS.md`, control-plane prompt packet, or primary startup surface. The one-line minimum viable adoption is:

> Meaningful governed work in this repo starts with `preflight_session`.

Agents familiar with the harness recognize this entry; agents new to the harness follow the link chain to the harness docs.

### 3. Keep the plugin installed by default

Keep the marketplace policy set to `INSTALLED_BY_DEFAULT` when the goal is harness behavior.

`AVAILABLE` means:

- the harness exists as an available plugin
- the host may defer presenting it to the agent at startup — that is a weaker operational state than installed-by-default

`INSTALLED_BY_DEFAULT` is the current install policy that makes the harness present at startup in either lane.

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

When the target repo is using the schema family, schema reality must also be true.

That means material governance — copied schema files are delivery evidence, governance requires the harness running against them.

Use:

- `is_schema_real_or_shadow_copied`
- `assess_harness_gap_state`
- `preflight_session`

Expected stable state for a governed repo:

- `schema_reality_state = real`
- `integrity_state = aligned`
- `possession_state = unpossessed`
- `adoption_state = fully_adopted` or at minimum `partially_adopted` with named gaps being actively resolved

A repo in any of these schema states stays in the "delivered, pre-governance" category — trust as governed only when schema reality reaches `real` / `aligned` / `unpossessed`:

- `partial`
- `copied_only`
- `possessed`

### C. Gov2gov when needed

When the target repo already has existing authority surfaces, schema reconciliation runs through gov2gov rather than a schema copy.

Use:

- `run_gov2gov_migration`

in `plan_only` first, then `non_destructive_apply` when appropriate.

This is how the harness makes a copied or drifted package real while preserving local truth.

## Full adoption definition for another repo

A target repo should only be considered fully adopted when all of the following are true:

- `plugins/AnarchyAi.Setup.exe` has been used or the equivalent plugin bundle has already been materialized
- either:
  - `plugins/anarchy-ai-herringms-<repo-slug>-<stable-path-hash>/` is present
  - or `~/.codex/plugins/anarchy-ai-herringms/` is present
- either:
  - `.agents/plugins/marketplace.json` contains the Anarchy-AI entry for the repo
  - or `~/.agents/plugins/marketplace.json` contains the Anarchy-AI entry for the current user
- installation policy is `INSTALLED_BY_DEFAULT`
- bundled runtime exists
- assess returns `ready`
- `preflight_session` is callable
- `assess_harness_gap_state` is callable
- the repo's agent-facing startup/control-plane direction expects preflight-first for meaningful governed work
- if the schema family is in use, the schema package is materially `real`, not merely copied

## Current scope — what is in v1 and what is reserved for future delivery

This process is current and real, and it is an intermediate install architecture.

Current scope:

- install scope covers repo-bootstrap and user-profile lanes; machine-level install is future work
- user-profile install is supported; true device-local install is future work
- packaged delivery is Windows-first
- Claude packaged adapter is future work; the contract model and MCP direction are shared today
- Cursor first-class delivery is future work
- host-native install suggestion chips fall outside the guaranteed install story — they exist when the host offers them, through a separate optional path
- GUI mode covers `Assess` and `Install` today; GUI `Update` is future work
- reflection (`assess the last exchange and do better`) remains a secondary workflow — first-class install targeting is future work
- public update depends on outbound access to the configured source zip and a working local trust/TLS path
- local-source update is the safer fallback when public HTTPS is unreliable on the machine
- runtime replacement requires the active Anarchy-AI process to be stopped first when the update touches `runtime/win-x64/AnarchyAi.Mcp.Server.exe` — updates that leave the runtime binary alone proceed in place

## Minimum checklist for another repo

1. Copy `plugins/AnarchyAi.Setup.exe` into the target repo `plugins/` folder.
2. Run `AnarchyAi.Setup.exe /install /repolocal` or `AnarchyAi.Setup.exe /install /userprofile`, or double-click it and choose the lane in the GUI.
3. Require install to provision or update the matching marketplace root with `INSTALLED_BY_DEFAULT`.
4. Run `AnarchyAi.Setup.exe /assess` with the same lane and require `bootstrap_state = ready`.
5. Install now seeds missing portable schema-family files by default; use `/refreshschemas` only when repo-root schema copies should be overwritten from the embedded payload.
6. Use `AnarchyAi.Setup.exe /update` when the carried bundle needs to be refreshed.
7. Add a startup/control-plane instruction that meaningful governed work starts with `preflight_session`.
8. Verify schema reality before trusting copied schema presence.
9. Use gov2gov planning where existing authority surfaces must be reconciled.

When every one of those nine items is true, the system counts as fully delivered, accessible, enforced, and real in the target repo. Any gap keeps the system in partial adoption.

## Portability evidence checklist

Use `ANARCHY_AI_ENVIRONMENT_TRUTH_MATRIX.md` as the evidence authority when describing portability.

Environment claims should be graded across:

1. application portability
   - Codex currently proven
   - other hosts inferred until equivalent install/mount evidence exists
2. session portability
   - require fresh-session repeat after identifiable setup change
3. device portability
   - require second-device repeat with equivalent artifacts

Do not mark portability as proven from single-session or single-device observations.
