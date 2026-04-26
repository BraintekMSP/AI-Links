# Anarchy-AI Repo Installation Process

## Purpose

This document defines the exact current installation process for bringing Anarchy-AI into another repository.

This is the current real delivery path - a repo-bootstrap install.
Machine-level install is future work.
This process makes the harness:

- delivered
- accessible
- operational enough to influence work
- real enough to trust

Current reality:

- packaged delivery is Windows-first
- Codex is the current first-class packaged host
- Claude Code and Claude Desktop host-target lanes are implemented as opt-in setup targets, but remain inferred until the truth-matrix promotion tests are captured
- Cursor is future work for first-class install targeting
- current Codex plugin compatibility is being repaired separately; do not treat plugin visibility as the source of harness truth during that repair
- environment truth separation for Codex install/mount behavior is tracked in:
  - `ANARCHY_AI_ENVIRONMENT_TRUTH_MATRIX.md`

## Preferred current delivery surface

The preferred first-delivery surface is one file:

- `plugins/AnarchyAi.Setup.exe`

In the `AI-Links` source repo, that installer should be generated first with:

```powershell
powershell -ExecutionPolicy Bypass -File .\harness\setup\scripts\build-self-contained-exe.ps1
```

Build prerequisite rule:

- use a .NET SDK from a non-workspace user/machine-local path such as `%USERPROFILE%\.dotnet`, `%LOCALAPPDATA%`, or `C:\Program Files\dotnet`
- do not install .NET SDK/runtime files, NuGet caches, restore scratch, or package caches into the source repo or a target repo
- synced workspace trees such as OneDrive are especially bad lanes for SDKs and package caches because they add sync noise and raise corruption risk
- the setup EXE is self-contained for target install; target repos do not need a repo-local .NET installation

That file now handles:

- plugin bundle materialization into either:
  - a repo-scoped `plugins/anarchy-ai/`
  - or a Codex user-profile `~/.codex/plugins/anarchy-ai/`
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
  - `plugins/anarchy-ai/`
- user-profile:
  - `~/.codex/plugins/anarchy-ai/`

That bundle contains:

- `.codex-plugin/plugin.json`
- `.mcp.json`
- `contracts/`
- `runtime/win-x64/AnarchyAi.Mcp.Server.exe`
- `schemas/`
- `templates/narratives/`
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

- repo-local installs use the selected repo root as the filesystem scope and a repo-scoped marketplace identity while keeping the visible plugin identity stable as `anarchy-ai`
- user-profile installs use one stable user-profile plugin identity because the install root is intentionally shared for that user
- both lanes keep the plugin-local MCP server key stable as `anarchy-ai`

Codex is expected to treat each marketplace root as a separate plugin distribution. That is good host provenance, not a Codex error. Anarchy's hygiene rule is stricter: normal repos should not need a repo-local runtime distribution at all. Use `/underlay` for portable repo discipline, `/userprofile` for the normal runtime, and `/repolocal` only for proving or debugging repo-local plugin behavior.

Current repo-local shape:

- `name = anarchy-ai-repo-<repo-slug>`
- `interface.displayName = Anarchy-AI Repo (<RepoName>)`
- `plugins.<entry>.name = anarchy-ai`
- `plugins.<entry>.source.path = ./plugins/anarchy-ai`
- `.codex-plugin/plugin.json -> name = anarchy-ai`
- `.mcp.json -> mcpServers -> anarchy-ai`

Current user-profile shape:

- `name = anarchy-ai-user-profile`
- `interface.displayName = Anarchy-AI User Profile`
- `plugins.<entry>.name = anarchy-ai`
- `plugins.<entry>.source.path = ./.codex/plugins/anarchy-ai`
- `.codex-plugin/plugin.json -> name = anarchy-ai`
- `.mcp.json -> mcpServers -> anarchy-ai`
- Codex home readiness is plugin-marketplace-first; a custom `mcp_servers.anarchy-ai` block is optional fallback/debug only
- older legacy `mcp_servers.anarchy-ai-herringms` blocks are cleanup evidence only

Marketplace naming note:

- official Codex plugin docs describe `interface.displayName` as the marketplace title shown in Codex
- current Codex plugin surfaces can still expose the top-level marketplace `name`
- keep that `name` branded and stable across devices rather than leaking a path-derived hash

Setup writes and maintains this plugin entry shape:

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

If the target repo is adopting the AGENTS Heuristic Underlay, prefer the runtime-free underlay lane first:

```powershell
.\plugins\AnarchyAi.Setup.exe /underlay /repo "C:\path\to\target-repo" /silent /json
```

This seeds portable discipline without installing the runtime plugin, writing marketplace state, registering an MCP server, or touching host config.

Runtime install lanes still seed missing portable schema-family files into repo root when a workspace is targeted, but repo-local runtime install is now a proving/debug carrier rather than the default committed repo-truth lane.

It is valid for a consumer repo to have Anarchy underlay present while no repo-local Anarchy plugin is available. In that state, agents can still read the portable schemas, narrative register, triage guide, and AGENTS awareness note. Runtime harness tools remain optional and should usually come from the user's profile-level Anarchy install.

If those repo-root schema files already exist, install leaves them in place.

Use explicit schema refresh only when you want the embedded portable schema family to overwrite repo-root copies. Refresh is plan-first:

```powershell
.\plugins\AnarchyAi.Setup.exe /refresh /repo "C:\path\to\target-repo" /silent /json
```

Apply requires deliberate opt-in:

```powershell
.\plugins\AnarchyAi.Setup.exe /refresh /apply /repo "C:\path\to\target-repo" /silent /json
```

That set is:

- `AGENTS-schema-governance.json`
- `AGENTS-schema-1project.json`
- `AGENTS-schema-narrative.json`
- `AGENTS-schema-gov2gov-migration.json`
- `AGENTS-schema-triage.md`
- `Getting-Started-For-Humans.txt`

Keep harness delivery separate from schema reality.
Material governance requires the harness to be installed, callable, and used in the work path - copied schema files alone are delivery evidence, not proof of operative governance.

Schemas do not self-fulfill. They make the desired route legible, but setup, runtime tools, proof files, or human-confirmed verification must still materialize and observe the intended state.

### 4. Narrative arc templates

Because `AGENTS-schema-narrative.json` carries the narrative register/record shape, the installed plugin bundle also carries concrete templates:

- `templates/narratives/register.template.json`
- `templates/narratives/record.template.json`

These templates are not project arcs by themselves. They are the carried surface that lets runtime tools materialize a missing `.agents/anarchy-ai/narratives/register.json` without inventing the shape from chat.

`run_gov2gov_migration` now treats the narrative register and projects directory as non-destructive materialization targets when the narrative schema is present or planned for delivery. It seeds only missing surfaces and leaves existing narrative content untouched.

The underlay lane also seeds a missing narrative register from `register.template.json`. The seeded open-thread ids are made unique per workspace at materialization time, opened dates are written at materialization time, and owner is `consumer-workspace-owner` rather than a named AI-Links author.

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
  - `./plugins/anarchy-ai/.codex-plugin/plugin.json` exists
  - `./plugins/anarchy-ai/.mcp.json` exists
  - `./.agents/plugins/marketplace.json` contains the repo-scoped Anarchy-AI plugin entry
- user-profile:
  - `~/.codex/plugins/anarchy-ai/.codex-plugin/plugin.json` exists
  - `~/.codex/plugins/anarchy-ai/.mcp.json` exists
  - `~/.agents/plugins/marketplace.json` contains the `anarchy-ai` plugin entry
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
- when Codex is targeted:
  - `codex_materialization.source_plugin_manifest_version` reports the source bundle version when the plugin manifest exists
  - `codex_materialization.codex_plugin_enabled` reports whether `~/.codex/config.toml` has the matching `[plugins."plugin@marketplace"] enabled = true` entry when setup can read it
  - install/update sets an existing selected Anarchy plugin key to `enabled = true` and disables only non-selected Anarchy plugin keys
  - `codex_materialization.cache_entries` reports the Codex-owned cache entries visible to setup
  - `codex_materialization.source_version_present_in_cache` tells whether Codex has materialized the same version into its plugin cache

Any other result shape means the harness is partially delivered â€” files present, accessibility incomplete. Count installation as complete only when the expected good result shape is reached.

Host proof note:

- Codex Plugins UI visibility proves source/catalog recognition, not active chat/runtime cache selection.
- A repo-local plugin card can appear before `~/.codex/plugins/cache/<marketplace>/<plugin>/<version>` catches up.
- Treat setup readiness, plugin-card visibility, Codex enable-state, cache materialization, and live harness tool calls as separate evidence surfaces.

Install lock behavior:

- if install runs while one or more existing user-profile bundle files are locked by another process, setup can return:
  - `bootstrap_state = registration_refresh_needed`
  - `missing_components` contains `locked_bundle_surface_write_skipped`
  - `next_action = release_runtime_lock_and_retry_install`
- in that case, run runtime-lock release and retry install:
  - `SafeReleaseRuntimeLock` first
  - `ForceReleaseRuntimeLock` only if needed

### Step 4. Default schema seeding and optional schema refresh

Plain install still seeds missing portable schema-family files by default, but `/underlay` is the cleaner repo-travel lane when the runtime is not being installed.

Scope note:

- `/repolocal` seeds into the selected repo root.
- `/userprofile` seeds only when `/repo "<path>"` is provided explicitly.
- default `/userprofile` with blank `/repo` keeps portable schema seeding out of scope, reports `portable_schema_family_not_targeted`, and still returns `paths.destination` for the user-profile target.

If you want to inspect schema drift without writing, run:

```powershell
.\plugins\AnarchyAi.Setup.exe /refresh /repo "C:\path\to\target-repo" /silent /json
```

If you want to overwrite repo-root schema files from the embedded portable schema family, run:

```powershell
.\plugins\AnarchyAi.Setup.exe /refresh /apply /repo "C:\path\to\target-repo" /silent /json
```

Deprecated compatibility:

```powershell
.\plugins\AnarchyAi.Setup.exe /install /repolocal /refreshschemas /apply
```

`/refreshschemas` used to write by default. It is deprecated because that convenience path bypassed the newer plan-first safety discipline.

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

This command updates the runtime bundle and returns a portable schema refresh plan. Add `/apply` only when update should actually overwrite portable schema files:

```powershell
.\plugins\AnarchyAi.Setup.exe /update /userprofile /repo "C:\path\to\target-repo" /refreshschemas /apply /sourcepath "C:\path\to\AI-Links"
```

Current update behavior:

- refreshes the selected plugin bundle surfaces in either:
  - repo-local `./plugins/anarchy-ai/`
  - user-profile `~/.codex/plugins/anarchy-ai/`
- seeds missing portable root schema files during install by default
- plans portable schema refresh when `/refreshschemas` is passed
- applies portable schema overwrite only when `/apply` is also passed
- returns bounded update state in the JSON result
- replacing a running `AnarchyAi.Mcp.Server.exe` in place requires stopping the active runtime first â€” update the bundled runtime only after release

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
  - `.\plugins\anarchy-ai`
- user-profile:
  - `~\.codex\plugins\anarchy-ai`

If Anarchy-AI needs to be retired cleanly instead of repaired, use the dedicated retirement script rather than ad hoc file deletion:

Human-friendly Windows quick cleanup:

```text
double-click plugins\Remove Anarchy-AI.cmd
```

That path runs safe quarantine-first cleanup, removes every reachable live Anarchy-AI surface for the current repo/user context, preserves repo-authored source truth, and keeps the console open with a plain-language summary.
It rewrites shared `~/.codex/config.toml` only for Anarchy-owned Codex plugin enable-state such as `[plugins."anarchy-ai@anarchy-ai-repo-<repo-slug>"]`; unrelated Codex plugin, window, and project trust sections are preserved.
Legacy custom-MCP blocks such as `[mcp_servers.anarchy-ai]` remain opt-in cleanup only.

```powershell
powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\remove-anarchy-ai.ps1 -Mode Assess
```

```powershell
powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\remove-anarchy-ai.ps1 -Mode Quarantine -Targets repo_local,user_profile,device_app
```

```powershell
powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\remove-anarchy-ai.ps1 -Mode Remove -Targets repo_local,user_profile,device_app
```

```powershell
powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\remove-anarchy-ai.ps1 -Mode Quarantine -Targets user_profile -IncludeLegacyCustomMcpConfig
```

In repos carrying the published delivery folder, the human-friendly Windows front door is surfaced at `plugins/Remove Anarchy-AI.cmd`. The canonical advanced/scriptable helper stays at `<installed-plugin-root>\scripts\remove-anarchy-ai.ps1`.

Recommended defaults for the retirement helper:

- omit `-RepoRoot` to auto-detect the current repo context when the helper is being run from a repo-local source or installed bundle
- omit `-UserProfileRoot` to auto-detect the current shell/user home directory
- omit `-Targets` to use the recommended current-context scope set:
  - `repo_local` when a repo context is detected
  - `user_profile,device_app` when only a home-local context is detected
- omit `-QuarantineRoot` to use a temp-directory quarantine lane outside the workspace

Use the `.ps1` lane when an agent or power user needs exact control over mode, targets, or automation. Use `Remove Anarchy-AI.cmd` when a human simply wants the plugin gone responsibly.

That script inventories first, preserves repo-authored source truth, backs up mutable files before rewrite or retirement, removes Anarchy-only marketplace files after backup instead of leaving empty branded shells behind, detects both current and legacy installed plugin roots, removes owned Codex plugin enable-state, and retires the documented plugin-cache lane without guessing at broader Codex-private app databases.

Legacy custom-MCP fallback blocks such as current `mcp_servers.anarchy-ai` and older `mcp_servers.anarchy-ai-herringms` entries are now an explicit advanced cleanup path only. They are not touched by the human click-once flow or the helper defaults.

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

- `.\plugins\anarchy-ai`

## How to make the system accessible

Accessible means the host and agent can actually reach the harness without ceremony.

For the current Codex-first path, accessibility requires all of the following:

- `plugins/AnarchyAi.Setup.exe` or the already materialized plugin bundle is present
- plugin bundle exists in either:
  - `./plugins/anarchy-ai`
  - `~/.codex/plugins/anarchy-ai`
- marketplace entry exists in either:
  - `./.agents/plugins/marketplace.json`
  - `~/.agents/plugins/marketplace.json`
- when using user-profile lane with Codex:
  - `~/.agents/plugins/marketplace.json` points at `./.codex/plugins/anarchy-ai`
  - `~/.codex/plugins/anarchy-ai` contains the bundled runtime and plugin surfaces
  - `~/.codex/config.toml` custom MCP registration is optional fallback/debug only and is not required for `ready`
- policy is `INSTALLED_BY_DEFAULT`
- bundled runtime exists
- `.mcp.json` points at the bundled runtime
- skill exists in the plugin bundle
- setup or bootstrap assessment returns `ready`

When any of those are missing, the harness remains partially delivered â€” files present, accessibility incomplete. Count the harness as accessible only when every item above is satisfied.

## How to make the system operational enough to matter

Installed and operational are separate states. Installed covers delivery and accessibility; operational covers whether the harness actually shapes how work begins and how gaps stay visible.

For the current architecture, operational influence means the harness changes the terrain before work hardens into action. It does not mean the host is compelled to obey every instruction.

### 1. Agent startup expectation

The target repo should treat this as the default rule:

- complex changes start with `preflight_session`

That is the current harness posture.
Without it, the harness remains optional utilities.

### 2. Target repo startup or control-plane instructions

To make that rule operational, add a direction in the target repo's startup surface or control-plane prompt packet that says the agent should:

- run `preflight_session` before complex changes
- use `assess_harness_gap_state` when install/runtime/schema/adoption state is unclear
- use `is_schema_real_or_shadow_copied` before trusting copied schema presence

A paste-ready block that satisfies this requirement lives in:

- `templates/ANARCHY_AI_STARTUP_INSTRUCTION_TEMPLATE.md`

Drop it into the target repo's `AGENTS.md`, control-plane prompt packet, or primary startup surface. The one-line minimum viable adoption is:

> Complex changes in this repo start with `preflight_session`.

Agents familiar with the harness recognize this entry; agents new to the harness follow the link chain to the harness docs.

### 3. Keep the plugin installed by default

Keep the marketplace policy set to `INSTALLED_BY_DEFAULT` when the goal is harness behavior.

`AVAILABLE` means:

- the harness exists as an available plugin
- the host may defer presenting it to the agent at startup â€” that is a weaker operational state than installed-by-default

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

That means material governance â€” copied schema files are delivery evidence, governance requires the harness running against them.

Use:

- `is_schema_real_or_shadow_copied`
- `assess_harness_gap_state`
- `preflight_session`

Expected stable state for a governed repo:

- `schema_reality_state = real`
- `integrity_state = aligned`
- `possession_state = unpossessed`
- `adoption_state = fully_adopted` or at minimum `partially_adopted` with named gaps being actively resolved

A repo in any of these schema states stays in the "delivered, pre-governance" category â€” trust as governed only when schema reality reaches `real` / `aligned` / `unpossessed`:

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
  - `plugins/anarchy-ai/` is present
  - or `~/.codex/plugins/anarchy-ai/` is present
- either:
  - `.agents/plugins/marketplace.json` contains the Anarchy-AI entry for the repo
  - or `~/.agents/plugins/marketplace.json` contains the Anarchy-AI entry for the current user
- installation policy is `INSTALLED_BY_DEFAULT`
- bundled runtime exists
- assess returns `ready`
- `preflight_session` is callable
- `assess_harness_gap_state` is callable
- the repo's agent-facing startup/control-plane direction expects preflight-first for complex changes
- if the schema family is in use, the schema package is materially `real`, not merely copied

## Current scope â€” what is in v1 and what is reserved for future delivery

This process is current and real, and it is an intermediate install architecture.

Current scope:

- install scope covers repo-bootstrap and user-profile lanes; machine-level install is future work
- user-profile install is supported; true device-local install is future work
- packaged delivery is Windows-first
- Claude Code and Claude Desktop packaged lanes are implemented as opt-in host targets and remain inferred until fresh-session/fresh-app verification is captured
- Cursor first-class delivery is future work
- host-native install suggestion chips fall outside the declared install story - they exist when the host offers them, through a separate optional path
- GUI mode covers `Assess` and `Install` today; GUI `Update` is future work
- reflection (`assess the last exchange and do better`) remains a secondary workflow â€” first-class install targeting is future work
- public update depends on outbound access to the configured source zip and a working local trust/TLS path
- local-source update is the safer fallback when public HTTPS is unreliable on the machine
- runtime replacement requires the active Anarchy-AI process to be stopped first when the update touches `runtime/win-x64/AnarchyAi.Mcp.Server.exe` â€” updates that leave the runtime binary alone proceed in place

## Minimum checklist for another repo

1. Copy `plugins/AnarchyAi.Setup.exe` into the target repo `plugins/` folder.
2. Run `AnarchyAi.Setup.exe /underlay` when the repo should carry portable discipline without runtime install.
3. Run `AnarchyAi.Setup.exe /install /repolocal` only for proving/debug runtime placement, or `AnarchyAi.Setup.exe /install /userprofile` for the normal host runtime lane.
4. Do not commit repo-local plugin bundles, marketplace pointers, EXE/PDB/runtime files, cache output, or test JSONL residue as repo truth.
5. Use runtime install to provision or update the matching marketplace root with `INSTALLED_BY_DEFAULT` only when a runtime lane is intended.
6. Run `AnarchyAi.Setup.exe /assess` with the same runtime lane and require `bootstrap_state = ready` when runtime was installed.
7. Use `/refresh` to inspect schema drift and `/refresh /apply` only when repo-root schema copies should be overwritten from the embedded payload.
8. Use `AnarchyAi.Setup.exe /update` when the carried bundle needs to be refreshed.
9. Add startup guidance that complex changes start with `preflight_session` when the runtime is available.
10. Verify schema reality before trusting copied schema presence.
11. Use gov2gov planning where existing authority surfaces must be reconciled.

When every applicable item is true, the system counts as fully delivered, accessible, operational, and real in the target repo. Any gap keeps the system in partial adoption.

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

