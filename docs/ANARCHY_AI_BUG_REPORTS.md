# Anarchy-AI Bug Reports

## Purpose

Capture concrete defects observed during setup, mounting, and schema-reality operation so fixes are tracked with reproducible evidence and acceptance criteria.

## Scope

- Install and update lanes (`repo-local`, `user-profile`)
- Codex mount and registration surfaces
- Runtime-lock behavior
- Schema reality and divergence reporting
- Environment proof discipline

## Open Bug Reports

### AA-BUG-001: Mixed lane registration causes non-deterministic mount path

- Severity: High
- Status: Patched local (pending multi-session proof)
- Component: Setup / Registration
- Repro:
  - Install through `user-profile`.
  - Keep or reintroduce stale custom MCP config targeting legacy `~/plugins/anarchy-ai`.
  - Restart Codex and run harness calls.
- Expected:
  - A single authoritative lane is active and reported.
- Actual:
  - Runtime may mount from legacy and produce misleading integrity signals.
- Evidence:
  - `~/.codex/config.toml` had `[mcp_servers.anarchy-ai]` to `C:\Users\herri\plugins\anarchy-ai`.
  - `~/.agents/plugins/marketplace.json` pointed to `./.codex/plugins/anarchy-ai`.
- Acceptance:
  - Setup blocks or repairs mixed-lane state and reports the active lane explicitly.
  - Local patch notes:
    - assess now reports legacy custom MCP mixed-lane findings instead of hiding them
    - stale custom MCP lane blocks `ready` until remediated

### AA-BUG-002: User-profile install does not always remove stale custom MCP entry

- Severity: High
- Status: Patched local (pending multi-session proof)
- Component: Setup / Codex config migration
- Repro:
  - Have legacy `[mcp_servers.anarchy-ai]` in `~/.codex/config.toml`.
  - Run `AnarchyAi.Setup.exe /install /userprofile ...`.
- Expected:
  - Stale legacy block is removed or disabled automatically.
- Actual:
  - Stale block can persist and win routing.
- Evidence:
  - Manual cleanup of `~/.codex/config.toml` was required to stabilize lane behavior.
- Acceptance:
  - Post-install check confirms stale block absent (or marked disabled) when marketplace lane is ready.
  - Local patch notes:
    - user-profile install/update removes stale legacy `[mcp_servers.anarchy-ai]` entries pointing to `~/plugins/anarchy-ai`
    - install action includes `removed_stale_codex_custom_mcp_entry`

### AA-BUG-003: Published setup EXE can remain stale when destination file is locked

- Severity: High
- Status: Patched local (pending lock-path revalidation)
- Component: Build / Publish helper
- Repro:
  - Keep `plugins/AnarchyAi.Setup.exe` in use during publish.
  - Run `harness/setup/scripts/build-self-contained-exe.ps1`.
- Expected:
  - Build fails hard with clear lock error and no ambiguous success state.
- Actual:
  - Operators can continue using stale EXE copies and observe old switch behavior.
- Evidence:
  - Target repo EXE rejected new switches until manual replacement with fresh build.
- Acceptance:
  - Helper exits non-zero whenever copy-to-plugins step fails or is skipped unexpectedly.
  - Local patch notes:
    - helper already exits non-zero on copy failure and now carries explicit stale-handoff guidance

### AA-BUG-004: Shipped binary CLI contract is not fully guarded by release validation

- Severity: Medium
- Status: Patched local (pending CI wire-up)
- Component: CI / Packaging validation
- Repro:
  - Run an older packaged EXE in target repo.
  - Invoke `/?`, `/repolocal`, `/silent`.
- Expected:
  - Release validation guarantees published EXE matches current CLI contract.
- Actual:
  - Target EXE exposed legacy parser and unsupported switch errors.
- Evidence:
  - `Unsupported switch: /?` and `Unsupported switch: /repolocal` on stale binary.
- Acceptance:
  - Release lane executes smoke checks against the produced EXE artifact before publish complete.
  - Local patch notes:
    - build helper now validates `/?` help contract on both published EXE and copied `plugins/AnarchyAi.Setup.exe`

### AA-BUG-018: Setup deployable can package a stale embedded MCP runtime

- Severity: High
- Status: Fixed locally by `184a3a9` (pending independent release repeat)
- Component: Build / Publish helper / Runtime payload
- Repro:
  - Update harness server source or contracts.
  - Run the setup publish helper.
  - Inspect `plugins/AnarchyAi.Setup.exe` help or timestamp and treat the setup EXE as fresh.
  - Install into a target repo and inspect the extracted `AnarchyAi.Mcp.Server.exe`.
- Expected:
  - A rebuilt setup EXE carries the current MCP runtime payload.
  - The release helper fails before publish if it cannot build or stage the current runtime.
  - The repo does not require committing refreshed runtime binaries or the large setup EXE to make the deployable reproducible.
- Actual:
  - The setup EXE could be fresh while the embedded `plugins/anarchy-ai/runtime/win-x64/AnarchyAi.Mcp.Server.exe` payload remained stale.
  - The stale runtime lacked current harness tool strings such as `direction_assist_test` and `verify_config_materialization`.
- Evidence:
  - Local check on `2026-04-25` showed:
    - `plugins/AnarchyAi.Setup.exe` exposed current `/status` and install-state help text.
    - tracked runtime payload was still the older runtime and did not contain current harness tool strings.
  - Fix commit `184a3a9` changed setup publish to:
    - publish `AnarchyAi.Mcp.Server.exe` first
    - stage a temporary plugin payload
    - replace only the staged runtime
    - publish setup using `AnarchySetupPluginPayloadRoot`
    - leave tracked runtime binary and gitignored setup EXE out of source history
  - Smoke install after the fix returned `bootstrap_state = ready`, `install_state.state_valid = true`, and extracted a runtime containing current tool strings:
    - `direction_assist_test`
    - `verify_config_materialization`
    - `assess_harness_gap_state`
    - `preflight_session`
    - `compile_active_work_state`
    - `run_gov2gov_migration`
    - `is_schema_real_or_shadow_copied`
- Acceptance:
  - `build-self-contained-exe.ps1` publishes the MCP server before setup.
  - Setup payload is built from a staged plugin bundle carrying the freshly published runtime.
  - Build output reports `plugin_payload_staged = true` and `published_runtime_executable`.
  - Smoke install from `plugins/AnarchyAi.Setup.exe` extracts a runtime with current expected tool strings.
  - `git status --short` remains source-only after build; no large setup EXE or refreshed runtime binary is required for commit.

### AA-BUG-019: Codex home install and cache can diverge in exposed metadata

- Severity: Medium
- Status: Open
- Component: Codex plugin cache / User-profile install discipline / Host metadata
- Repro:
  - Install Anarchy-AI through the user-profile Codex lane with the current setup EXE.
  - Restart Codex and open a fresh session in Fissure / Docker-Builder-Project.
  - Ask the session to inspect whether Anarchy is visible and healthy.
- Expected:
  - Installed root, Codex cache root, exposed skill metadata, and callable runtime all report the same active version and source lane.
- Actual:
  - Fresh-session Anarchy tools were callable and the installed plugin root existed at `C:\Users\herri\.codex\plugins\anarchy-ai`.
  - The installed bundle and Codex cache on disk showed version `0.1.8`.
  - The session metadata still advertised an Anarchy skill path under cache version `0.1.7`.
- Evidence:
  - Setup install on `2026-04-25` against Fissure returned:
    - `bootstrap_state = "ready"`
    - `install_state.state_valid = true`
    - `actions_taken` included `materialized_plugin_bundle_from_embedded_payload` and `wrote_install_state`
  - Disk inspection after restart found:
    - `C:\Users\herri\.codex\plugins\anarchy-ai`
    - `C:\Users\herri\.codex\plugins\cache\anarchy-ai-user-profile\anarchy-ai\0.1.8`
  - Fissure-session report stated:
    - `assess_harness_gap_state` and `preflight_session` returned successfully
    - installed plugin manifest reports version `0.1.8`
    - exposed Anarchy skill metadata still referenced cache version `0.1.7`
  - Fissure arc-capture pass on `2026-04-26` exposed the same class again:
    - the chat carried a stale skill path under `C:\Users\herri\.codex\plugins\cache\anarchy-ai-user-profile\anarchy-ai\0.1.7\skills\anarchy-ai-harness\SKILL.md`
    - the active user-profile source/cache lane had already moved to `0.1.11`
    - the agent correctly fell back to direct repo/MCP inspection, but the stale path itself proved version-pinned cache skill paths can survive in chat context after the active harness lane changes
- Required product direction:
  - Do not treat this as full install proof until the active runtime path, skill metadata path, installed root, cache root, and install-state agree in the same fresh session.
  - Treat home-local install and Codex cache as separate evidence surfaces.
  - Treat versioned Codex cache skill paths as evidence, not authority. Before schema, arc, or gov2gov work, resolve the active user-profile source version, active Codex cache version, callable runtime provenance, schema bundle version, and skill path generation instead of trusting a copied `~\.codex\plugins\cache\...\0.1.x\...` path from prior chat context.
  - Installation discipline should account for cache invalidation or at least report cache-version disagreement explicitly.
- Acceptance:
  - A setup status/doctor or harness diagnostic reports:
    - installed user-profile plugin root
    - Codex cache roots and versions for the marketplace/plugin pair
    - install-state version and recorded runtime path
    - exposed active skill metadata path when the host makes it available
    - active runtime path when a live tool call can report it
  - Fresh-session proof requires metadata/runtime/install-state agreement or explicitly records the mismatch as a caveat.
  - Arc/gov2gov instructions explicitly reject stale hardcoded cache skill paths as authoritative startup context unless their version matches the observed active harness lane.
  - Truth matrix distinguishes "tools callable" from "cache/home install state fully understood."

### AA-BUG-020: User-profile install-state conflates home runtime with last workspace target

- Severity: High
- Status: Patched local (pending republish and Workorders/Fissure repeat)
- Component: Setup / Install lifecycle state
- Repro:
  - Install Anarchy-AI through `/userprofile /repo <RepoA>`.
  - Later run `/status /userprofile /repo <RepoB>`.
- Expected:
  - The user-profile runtime, plugin root, marketplace path, install-state path, and MCP runtime path are validated as the home install target.
  - The repo path is treated as a workspace/schema target and may differ without invalidating the home install.
  - Status still exposes the last recorded workspace target so adoption/schema checks can be reasoned about explicitly.
- Actual:
  - The single install-state record stored `workspace_root` as if it were part of the user-profile install identity.
  - A later status run against another repo reported `install_state_workspace_root_mismatch`, even when the home runtime and marketplace were otherwise valid.
- Evidence:
  - Workorders status on `2026-04-25` found user-profile runtime and marketplace present, but the install-state still recorded Fissure / Docker-Builder-Project as `recorded_workspace_root`.
  - The same Workorders repo-local status correctly reported `bootstrap_needed`, proving the confusion was lane/state modeling rather than a stale setup shortcut.
- Required product direction:
  - Follow ECC's lifecycle discipline shape: target adapter identity, request/plan/result state, managed operations, doctor/repair paths.
  - Do not copy ECC's product philosophy or overclaim enforcement.
  - User-profile install-state must separate install target from workspace/schema target.
- Local patch notes:
  - setup install-state version moved to `anarchy.install-state.v2`
  - state now writes nested `target`, `workspace`, `source`, and `managed_operations`
  - `/status` validates install-target facts and reports a different user-profile workspace target as warning `last_workspace_target_differs_from_current_request`
  - repo-local still treats workspace root as the project install target and keeps workspace mismatches blocking
  - regression test added for user-profile workspace mismatch as warning, not invalid state
- Acceptance:
  - Rebuild setup.
  - Install `/userprofile /repo <RepoA>`.
  - Run `/status /userprofile /repo <RepoB>`.
  - Confirm `install_state.state_valid = true`.
  - Confirm `install_state.warnings` includes `last_workspace_target_differs_from_current_request`.
  - Confirm `install_state.recorded_target_root` points at the user profile and `recorded_workspace_root` points at `<RepoA>`.
  - Confirm repo-local status for an uninstalled repo still reports `bootstrap_needed`.

### AA-BUG-021: Source repo assess reports generated consumer bundle surfaces as missing

- Severity: High
- Status: Patched local; superseded in part by `AA-BUG-022` plain repo-local path cleanup
- Component: Setup / Source-authoring assessment
- Repro:
  - Run `plugins/AnarchyAi.Setup.exe /assess /repolocal /repo <AI-Links> /codex /silent /json` from the AI-Links source repo.
- Expected:
  - Setup detects the repo-authored source bundle at `plugins/anarchy-ai`.
  - Source-owned contracts, runtime, skill, and `schemas/schema-bundle.manifest.json` are not reported missing.
  - The old generated repo-local consumer install target's absence does not erase source-bundle evidence.
  - Missing generated marketplace state is not treated as a blocking component for this read-only source-authoring assess.
  - Output names the source-authoring state so agents do not try to "fix" the source repo by inventing a self-consumer install.
- Actual:
  - Setup resolved only the generated repo-local target such as `plugins/anarchy-ai-local-ai-links-<hash>`.
  - Because that generated consumer directory did not exist, assess reported `schema_bundle_manifest_missing`, all core contracts missing, missing runtime, missing MCP declaration, missing skill surface, and missing plugin manifest.
  - This made the AI-Links source repo look broken even though `plugins/anarchy-ai` carried the current source bundle.
- Evidence:
  - `2026-04-25` AI-Links assess reported `bootstrap_state = bootstrap_needed`, `runtime_present = false`, and `schema_bundle_manifest_missing` while `plugins/anarchy-ai/schemas/schema-bundle.manifest.json` existed.
  - After the local patch and setup rebuild on `2026-04-25`, the same AI-Links assess returned `bootstrap_state = source_authoring_bundle_ready`, `source_authoring_bundle_present = true`, `source_authoring_bundle_state = complete`, `missing_components = []`, and exit code `0`.
- Required product direction:
  - Keep AI-Links source-authoring truth separate from consumer install readiness.
  - Do not expect schemas/harness to self-fulfill inside AI-Links.
  - Do not hide generated consumer install targets; show them as destination targets, not as source truth.
- Local patch notes:
  - setup now detects `plugins/anarchy-ai` as a source-authoring bundle during read-only repo-local assess/status
  - result includes `source_authoring_bundle_present` and `source_authoring_bundle_state`
  - complete source bundles produce `bootstrap_state = source_authoring_bundle_ready`
  - source bundle pathing now appears under `paths.origin` and `paths.source`
  - regression test added for source repo assess not reporting source-owned schema/contract/runtime surfaces as missing
- Acceptance:
  - Rebuild setup.
  - Run `/assess /repolocal /repo <AI-Links> /codex /silent /json`.
  - Confirm `source_authoring_bundle_present = true`.
  - Confirm `source_authoring_bundle_state = complete`.
  - Confirm `missing_components` does not include `schema_bundle_manifest_missing`, `bundled_runtime_missing`, or `missing_contract:*`.
  - Confirm `missing_components` does not include `repo_marketplace_missing`.
  - Confirm `paths.source.root_path` ends with `plugins/anarchy-ai`.

### AA-BUG-022: Repo-local plugin directory carries stale local/hash naming

- Severity: Medium
- Status: Patched local and republished in generated setup (pending commit/distribution)
- Component: Setup / Path canon / Install discipline
- Repro:
  - Run repo-local setup assessment or inspect setup path output.
  - Observe generated plugin roots such as `plugins/anarchy-ai-local-ai-links-bb85a60e`.
- Expected:
  - Repo-local install root is already scoped by the selected repo root.
  - The installed bundle path is plain and readable: `plugins/anarchy-ai`.
  - Repo-local marketplace identity uses an explicit repo label: `anarchy-ai-repo-<repo-slug>`.
  - Legacy `anarchy-ai-local-*` and `anarchy-local-*` names remain recognized only for cleanup, migration, and stale-state detection.
- Actual:
  - Repo-local bundle directory carried both ambiguous `local` language and a path-derived hash suffix.
  - Moving or cloning the same repo changed the generated path.
  - AI-Links source-authoring assessment became harder to reason about because source truth and generated consumer install naming used different mental models.
- Evidence:
  - `2026-04-25` AI-Links assess reported destination path `plugins/anarchy-ai-local-ai-links-bb85a60e`.
  - User challenged both the hash suffix and the `local` term as likely leftover hallucinated complexity.
  - After patch and setup rebuild, throwaway repo-local install returned `bootstrap_state = ready`, plugin root ending in `plugins/anarchy-ai`, marketplace name `anarchy-ai-repo-<throwaway-slug>`, and `plugins.<entry>.source.path = ./plugins/anarchy-ai`.
  - After patch and setup rebuild, AI-Links source repo install attempt returned `bootstrap_state = source_authoring_write_blocked` with only `source_authoring_repo_consumer_install_blocked` as the missing component.
- Required product direction:
  - Keep install paths legible.
  - Let roots define scope instead of adding path-derived suffixes.
  - Use source-repo marker detection to protect `AI-Links`, not strange folder names.
- Local patch notes:
  - path canon changed repo-local plugin directory template to `anarchy-ai`
  - path canon changed repo-local marketplace name template to `anarchy-ai-repo-<repo-slug>`
  - repo-local display name changed to `Anarchy-AI Repo (<RepoName>)`
  - setup reports `bootstrap_state = source_authoring_write_blocked` and blocks repo-local install/update when the selected repo is the AI-Links source repo
  - consumer repo installs still use `plugins/anarchy-ai` as a normal installed bundle path
- Acceptance:
  - Rebuild setup.
  - Run repo-local install into a throwaway repo.
  - Confirm `paths.destination.directories.plugin_root_directory_path` ends with `plugins/anarchy-ai`.
  - Confirm marketplace root name is `anarchy-ai-repo-<repo-slug>`.
  - Confirm `plugins.<entry>.source.path = ./plugins/anarchy-ai`.
  - Run AI-Links source repo assess and confirm it reports `source_authoring_bundle_ready`.
  - Run AI-Links source repo install attempt and confirm `bootstrap_state = source_authoring_write_blocked`.
  - Confirm the blocked source install does not report `repo_marketplace_missing` as the next repair.

### AA-BUG-023: Absent plugin root assessment reports noisy child-surface findings

- Severity: Medium
- Status: Patched local (pending rebuild and Workorders repeat)
- Component: Setup / Assessment findings
- Repro:
  - Run repo-local assess against a consumer repo with no `plugins/anarchy-ai` directory.
- Expected:
  - Setup reports the bundle/root installation gap and marketplace gap.
  - Setup does not list every child contract, runtime, schema manifest, skill, and MCP declaration as individually missing when the parent bundle root is absent.
  - Child-surface findings are reserved for partial bundles where `plugins/anarchy-ai` exists or at least one bundle marker exists.
- Actual:
  - Workorders assess returned `bootstrap_needed`, but also listed every core contract and every child bundle surface as missing.
  - The extra findings made a normal uninstalled repo look like a broken partial install.
- Evidence:
  - `2026-04-25` Workorders assess found no `plugins` directory and no repo marketplace, but still reported:
    - `schema_bundle_manifest_missing`
    - `bundled_runtime_missing`
    - `codex_plugin_manifest_missing`
    - `codex_mcp_declaration_missing`
    - `codex_skill_surface_missing`
    - `missing_contract:*`
    - `experimental_direction_assist_contract_missing_non_blocking`
- Local patch notes:
  - setup now computes `inspectChildBundleSurfaces`
  - child bundle missing findings only run when the plugin root exists or some bundle marker exists
  - fully absent bundles keep the assessment focused on install/marketplace repair
  - regression test added for absent repo-local bundle assessment not reporting child surfaces
- Acceptance:
  - Rebuild setup.
  - Re-run Workorders `/assess /repolocal`.
  - Confirm `bootstrap_state = bootstrap_needed`.
  - Confirm missing child-surface findings are absent.
  - Confirm `repo_marketplace_missing` still reports until install creates `.agents/plugins/marketplace.json`.

### AA-BUG-024: Plugin manifest version was hard-coded outside release canon

- Severity: High
- Status: Patched local (pending cross-device cache proof)
- Component: Build / Publish helper / Codex cache invalidation
- Repro:
  - Change plugin-facing surfaces such as skills, contracts, MCP tools, setup disclosure, or install pathing.
  - Rebuild setup.
  - Inspect `plugins/anarchy-ai/.codex-plugin/plugin.json`.
  - Observe Codex cache state on another device or session where an older home-profile plugin was already enabled.
- Expected:
  - The plugin manifest version is an explicit release-canon value.
  - Release builds move that version when plugin-facing behavior changes.
  - Codex has a clear new cache version to materialize instead of silently reusing a stale enabled cache lane.
- Actual:
  - `build-self-contained-exe.ps1` regenerated `plugin.json` with hard-coded `version = '0.1.8'`.
  - The work machine still exposed only `C:\Users\mherring\.codex\plugins\cache\anarchy-ai-user-profile\anarchy-ai\0.1.7` after the newer install work, while this machine's source bundle still declared `0.1.8`.
  - Manual edits to `plugin.json` would have been overwritten by the build helper.
- Evidence:
  - `2026-04-25` inspection found `plugins/anarchy-ai/.codex-plugin/plugin.json` at `0.1.8`.
  - The same inspection found `harness/setup/scripts/build-self-contained-exe.ps1` generated the plugin manifest with a literal `0.1.8`.
  - Work-machine session reported only cache version `0.1.7` under `anarchy-ai-user-profile/anarchy-ai`.
- Required product direction:
  - Treat plugin manifest version movement as cache-invalidation evidence, not cosmetic metadata.
  - Keep the version in repo-authored canon so generated plugin manifests, setup payloads, and release notes have one source.
  - Do not claim cross-device proof until the fresh session observes the expected marketplace/plugin/version cache lane.
- Local patch notes:
  - branding canon now carries `metadata.plugin_manifest_version`
  - branding generation mirrors that value into generated C#, PowerShell, and MSBuild artifacts
  - setup build helper now writes `plugin.json.version` from branding canon instead of a hard-coded literal
  - current release-canon plugin manifest version is bumped to `0.1.9`
- Acceptance:
  - Rebuild setup.
  - Confirm `plugins/anarchy-ai/.codex-plugin/plugin.json` reports `version = 0.1.9`.
  - Install user-profile on a machine with an older enabled cache.
  - Restart Codex and confirm a cache lane exists for `anarchy-ai-user-profile/anarchy-ai/0.1.9`.
  - If repo-local is enabled too, confirm whether Codex surfaces `anarchy-ai-repo-<repo-slug>/anarchy-ai/0.1.9` or the user-profile lane.
  - Record any active-lane mismatch under `AA-BUG-019` until runtime path, skill metadata path, installed root, and cache root agree.

### AA-BUG-025: Gov2gov ignored schema-carried narrative arc materialization

- Severity: High
- Status: Patched local (pending Fissure repeat)
- Component: Runtime / Gov2Gov / Installer payload
- Repro:
  - Install Anarchy-AI into a repo carrying the AGENTS schema family.
  - Run `run_gov2gov_migration` after `AGENTS-schema-narrative.json` is present or planned for delivery.
  - Ask whether the Anarchy narrative/arc register exists.
- Expected:
  - If a portable schema carries an artifact lane, the installer payload carries concrete templates for that lane.
  - Gov2gov includes the narrative register and projects directory in its non-gating inventory.
  - In `non_destructive_apply`, gov2gov seeds only missing narrative surfaces and leaves existing narrative content untouched.
- Actual:
  - `AGENTS-schema-narrative.json` carried register/record templates inside the schema, but the installed plugin bundle did not carry concrete narrative templates.
  - `run_gov2gov_migration` hash-checked `AGENTS-schema-narrative.json` as a file but ignored the `.agents/anarchy-ai/narratives` materialization surface implied by that schema.
  - Agents had to infer and manually create `.agents/anarchy-ai/narratives/register.json` and a project arc file.
- Evidence:
  - Fissure session on `2026-04-25` created `.agents/anarchy-ai/narratives/register.json` and `.agents/anarchy-ai/narratives/projects/fissure-jan-provider-lifecycle-arc.json` manually after the user clarified that arc is part of Anarchy.
  - The prior gov2gov `plan_only` result only reported canonical schema divergence/audit work and did not mention narrative register materialization.
- Required product direction:
  - Schema-carried artifact lanes should travel with the installer as concrete bundle surfaces.
  - Gov2gov should not own arbitrary story writing, but it should own missing-register/project-directory materialization when the narrative schema is present or planned.
  - Existing narrative artifacts are workspace-specific truth; never hash-compare or overwrite them as canonical payload.
- Local patch notes:
  - added `plugins/anarchy-ai/templates/narratives/register.template.json`
  - added `plugins/anarchy-ai/templates/narratives/record.template.json`
  - path canon now includes the `templates` bundle surface and narrative template paths
  - setup assessment reports missing narrative templates as bundle gaps
  - `run_gov2gov_migration` inventories `narrative_arc_structure`
  - `non_destructive_apply` seeds missing `.agents/anarchy-ai/narratives/register.json` and creates the missing `.agents/anarchy-ai/narratives/projects` directory
  - gov2gov contract version bumped to `0.1.2`
- Acceptance:
  - Rebuild setup.
  - Install into a throwaway repo and confirm `plugins/anarchy-ai/templates/narratives/register.template.json` and `record.template.json` are present.
  - Run gov2gov `plan_only` on a consumer workspace with narrative schema present and no narrative register.
  - Confirm planned actions include `seed_missing_narrative_register:.agents/anarchy-ai/narratives/register.json`.
  - Run gov2gov `non_destructive_apply`.
  - Confirm `.agents/anarchy-ai/narratives/register.json` exists, parses as JSON, and contains `records: []`.
  - Confirm existing narrative artifacts are not overwritten.

### AA-BUG-026: .NET prerequisites can drift into synced repo workspaces

- Severity: Medium
- Status: Patched local (pending release repeat)
- Component: Build / Install discipline / Workspace hygiene
- Repro:
  - Treat repo-local install as permission to place .NET SDK/runtime files, restore output, NuGet caches, or package caches under the source repo or a target repo.
  - Build or install from a synced workspace such as OneDrive.
- Expected:
  - Repo-local install only means the Anarchy-AI plugin bundle may be materialized under the selected repo.
  - .NET SDK/runtime prerequisites and package caches live in non-workspace user/machine-local lanes such as `%USERPROFILE%\.dotnet`, `%LOCALAPPDATA%`, or `C:\Program Files\dotnet`.
  - The setup EXE remains self-contained for target installs, so consumer repos do not need repo-local .NET.
- Actual:
  - Prior install/build discussion drifted toward treating repo-local .NET as acceptable.
  - That creates synced workspace noise and raises corruption risk in OneDrive-backed repos.
- Evidence:
  - User correction on `2026-04-25`: installing `.NET` to the repo is not the way; use non-workspace paths like AppData because repo-local SDK files create sync noise and contribute to corruptions.
  - Current build helper publishes temporary build output under `%LOCALAPPDATA%\Anarchy-AI\AI-Links\setup-build`, proving the correct lane exists.
  - Follow-up OneDrive sync inventory on `2026-04-26` showed old repo-local `bin/obj` and `.tmp` outputs still producing large synced files, including setup/server DLL and EXE artifacts.
- Local patch notes:
  - `build-self-contained-exe.ps1` now rejects a resolved `.NET SDK` path that lives inside the source workspace.
  - `Directory.Build.props` now redirects ordinary .NET `bin/obj` output for repo-local build/test/restore commands to `%LOCALAPPDATA%\Anarchy-AI\AI-Links\dotnet` by default.
  - `build-self-contained-exe.ps1` now uses `%LOCALAPPDATA%\Anarchy-AI\AI-Links\setup-build` rather than a repo-local or generic temp lane.
  - Removal-safety test fixtures now use `%LOCALAPPDATA%\Anarchy-AI\AI-Links\test-fixtures` rather than `.tmp` under the repo.
  - Setup/repo install docs now state the .NET prerequisite boundary directly.
- Acceptance:
  - Running the build helper with a normal user/machine-local SDK still succeeds.
  - Running the build helper with `-DotnetPath` pointing inside the repo fails before restore/publish work begins.
  - Running setup/server tests does not create new repo-local `bin/obj` outputs.
  - Release/docs language does not imply that target repos should install .NET locally to use the self-contained setup EXE.

### AA-BUG-027: Codex repo-local UI visibility can outrun cache/runtime materialization

- Severity: High
- Status: Patched diagnostic surface locally; host behavior still open
- Component: Setup / Codex host adapter / Install-state diagnostics
- Repro:
  - Install or sync a repo-local Anarchy-AI bundle into `plugins/anarchy-ai` with plugin manifest `version = 0.1.9`.
  - Ensure the repo-local marketplace points at `./plugins/anarchy-ai`.
  - Open Codex Plugins UI and select the repo-local marketplace.
  - Inspect `~/.codex/plugins/cache` from the same machine/session.
- Expected:
  - If Codex shows the repo-local plugin as installed/enabled, the cache/runtime materialization lane either agrees with the source manifest version or the mismatch is explicitly reported.
- Actual:
  - The Workorders repo-local source bundle existed at version `0.1.9`.
  - Codex Plugins UI showed `Anarchy-AI Repo (Workorders)` and the Anarchy-AI detail page with MCP server plus harness, structured-commit, and structured-review skills.
  - The Codex cache still showed repo-scoped `anarchy-ai-repo-workorders/anarchy-ai/0.1.8` and user-profile `anarchy-ai-user-profile/anarchy-ai/0.1.7`; no `0.1.9` cache directory was present.
- Evidence:
  - User screenshots on `2026-04-25` showed:
    - marketplace dropdown entries for Codex official, Anarchy-AI Repo (Workorders), and Anarchy-AI User Profile
    - plugin detail page with `Remove from Codex`, `Try in chat`, one MCP server, and three skills
    - information row `Category = anarchy-ai-repo-workorders, Productivity`
  - Local Codex config inspection showed plugin enable-state is stored separately from source and cache:
    - `[plugins."anarchy-ai@anarchy-ai-repo-workorders"] enabled = true`
    - `[plugins."anarchy-ai@anarchy-ai-user-profile"] enabled = true`
  - Follow-up filesystem inspection on the work machine reported:
    - repo-local source manifest `version = 0.1.9`
    - repo-local marketplace pointing to that source
    - Codex cache versions only `0.1.8` and `0.1.7`
  - Rebuilt setup assess against local Workorders repo-local install reported:
    - `codex_materialization.source_plugin_manifest_version = 0.1.9`
    - `codex_materialization.config_plugin_key = anarchy-ai@anarchy-ai-repo-workorders`
    - `codex_materialization.codex_plugin_enabled = true`
    - `codex_materialization.cache_entries = [0.1.8]`
    - `codex_materialization.source_version_present_in_cache = false`
    - finding `source_plugin_version_not_materialized_in_codex_cache`
- Required product direction:
  - Model at least five separate surfaces:
    - setup-owned source bundle
    - marketplace/UI source visibility
    - Codex plugin enable-state
    - Codex-owned cache materialization
    - active chat/runtime/tool selection
  - Do not use plugin-card visibility, `Try in chat`, or `Remove from Codex` alone as runtime-version proof.
  - Keep the repo-local path and marketplace shape; the missing piece is host materialization diagnostics, not another path suffix.
- Local patch notes:
  - setup JSON now includes `codex_materialization` for Codex-targeted assess/install/status calls
  - the report names the Codex config path, plugin enable key/state, marketplace/plugin cache root, source plugin manifest version, cache entries, and whether the source version is present in cache
  - repo-local disclosure/help now separates repo-local source readiness from Codex cache/runtime proof
- Acceptance:
  - Run setup assess/status against Workorders after repo-local `0.1.9` source sync.
  - Confirm `codex_materialization.source_plugin_manifest_version = 0.1.9`.
  - Confirm `codex_materialization.config_plugin_key = anarchy-ai@anarchy-ai-repo-workorders`.
  - Confirm `codex_materialization.codex_plugin_enabled = true` when Codex shows `Remove from Codex`.
  - Confirm `codex_materialization.cache_entries` reports the currently materialized cache versions.
  - Confirm `source_plugin_version_not_materialized_in_codex_cache` appears until Codex materializes `0.1.9`.
  - After Codex materializes `0.1.9`, confirm the finding clears without changing repo-local source paths.

### AA-BUG-028: Codex uninstall failure left plugin enable-state outside retirement coverage

- Severity: High
- Status: Patched local (pending rebuild/distribution and Workorders repeat)
- Component: Removal / Codex host adapter / Install lifecycle discipline
- Repro:
  - Enable both Anarchy repo-local and user-profile plugin lanes in Codex.
  - Let repo-local source sync to `plugins/anarchy-ai` at manifest `version = 0.1.9` while Codex cache remains on older versions.
  - Click Codex UI `Remove from Codex` for the repo-local Anarchy plugin.
- Expected:
  - Removal or repair tooling accounts for Codex's host-owned enable-state in `~/.codex/config.toml`.
  - Anarchy-owned plugin enable entries are removable without touching unrelated Codex plugin settings.
  - Legacy custom-MCP blocks remain opt-in cleanup because they are fallback/debug state, not the Codex plugin enable lane.
- Actual:
  - Codex surfaced `Failed to uninstall plugin`.
  - Repo-local source remained at `0.1.9`, Codex repo-scoped cache remained at `0.1.8`, and user-profile cache remained older in the reported session.
  - The Anarchy retirement helper covered marketplace files, plugin roots, cache directories, and opt-in legacy custom-MCP config, but did not inventory `[plugins."anarchy-ai@..."]` enable-state sections.
- Evidence:
  - User screenshot on `2026-04-25` showed the Codex plugin detail page toast `Failed to uninstall plugin`.
  - Local config shape uses plugin enable-state sections such as:
    - `[plugins."anarchy-ai@anarchy-ai-repo-workorders"] enabled = true`
    - `[plugins."anarchy-ai@anarchy-ai-user-profile"] enabled = true`
  - Removal safety tests previously asserted default cleanup should not inventory shared Codex config at all, which was correct for legacy custom MCP blocks but incomplete for Codex's current plugin enable-state.
- Required product direction:
  - Treat Codex plugin enable-state as its own install lifecycle surface.
  - Keep it separate from legacy `mcp_servers.*` fallback cleanup.
  - Remove only owned Anarchy plugin enable sections where both the plugin id and marketplace id are Anarchy-owned.
  - Preserve curated or unrelated sections such as `[plugins."teams@openai-curated"]`, `[windows]`, and `[projects.*]`.
- Local patch notes:
  - `remove-anarchy-ai.ps1` now detects owned `[plugins."anarchy-ai@<owned-marketplace>"]` sections.
  - device-app cleanup inventories `codex_plugin_enable_state_file` when Anarchy plugin enable-state is present.
  - quarantine/remove rewrites the Codex config after backup to remove only the owned plugin enable-state sections.
  - legacy custom-MCP cleanup remains gated behind `-IncludeLegacyCustomMcpConfig`.
  - removal safety regression fixtures now prove Anarchy plugin enable-state removal while preserving unrelated Codex plugin and workspace config.
- Acceptance:
  - Run `docs/scripts/test-removal-safety-compliance.ps1`.
  - Confirm default assess reports `codex_plugin_enable_state_file` when owned plugin enable-state exists.
  - Confirm default quarantine removes Anarchy `[plugins."anarchy-ai@..."]` sections.
  - Confirm default quarantine preserves legacy `[mcp_servers.anarchy-ai]`.
  - Confirm opt-in cleanup with `-IncludeLegacyCustomMcpConfig` removes the legacy MCP block as well.
  - Rebuild setup and repeat on a Workorders-style stale Codex cache/uninstall-failure state.

### AA-BUG-029: Runtime install and portable underlay were conflated

- Severity: High
- Status: Patched local (underlay smoke observed through setup DLL; direct windowless EXE smoke remains AA-BUG-030)
- Component: Setup / Install lifecycle discipline / Repo hygiene
- Repro:
  - Run repo-local install in a consumer repo.
  - Inspect the working tree before commit.
- Expected:
  - Portable schemas and narrative discipline can travel with the repo without committing runtime bundles, marketplace pointers, PDB/EXE/runtime files, or host-owned cache state.
  - Schema refresh is deliberate and plan-first.
  - Duplicate Anarchy Codex lanes are detected and bounded to one selected primary runtime lane.
- Actual:
  - `/repolocal` installed the runtime bundle and seeded portable schemas in one lane.
  - Consumer repos then saw large untracked runtime artifacts under `plugins/anarchy-ai/**` plus `.agents/plugins/marketplace.json`.
  - `/refreshschemas` existed as a write-by-default convenience path.
  - Multiple enabled Anarchy Codex lanes surfaced duplicate skills.
- Evidence:
  - Fissure reported 41 untracked Anarchy install artifacts including a large runtime executable and PDB.
  - Codex screenshots showed duplicate Anarchy skills from user-profile and repo-scoped plugin caches.
  - Workorders and Docker-Builder-Project evidence showed repo-local and user-profile lanes could both be enabled while cache materialization lagged source.
- Product direction:
  - Split `/underlay` from `/repolocal`.
  - Treat `/repolocal` as proving/debug runtime carrier, not committed repo truth.
  - Make `/refresh` plan-first and require `/apply`.
  - Keep deprecated `/refreshschemas` accepted but plan-first unless `/apply` is supplied; the old write-by-default behavior was the defect.
  - Auto-disable duplicate Anarchy Codex lanes only during primary runtime install/update, never during `/underlay`, `/refresh`, `/assess`, or `/status`.
- Local patch notes:
  - setup parser now accepts `/underlay`, `/refresh`, and `/apply`.
  - `/underlay` seeds missing portable schemas, narrative register/project directory, AGENTS.md awareness stub when absent, and Anarchy-scoped `.gitignore` lines without runtime or marketplace writes.
  - `/refresh` reports planned schema drift by default and writes timestamped `.bak` files only with `/apply`.
  - duplicate Codex lane cleanup sets other owned Anarchy plugin sections to `enabled = false` and preserves unrelated plugins.
- Acceptance:
  - Setup tests cover underlay seeding, AGENTS.md non-modification, refresh plan/apply, deprecated alias plan-first behavior, and duplicate-lane text rewriting.
  - Smoke `/underlay` into a throwaway repo and confirm no runtime bundle, marketplace, MCP, or host config writes.
  - Smoke `/refresh /apply` and confirm only canonical portable schema files change with backups.
  - Repeat on a Workorders-style duplicate-lane state and confirm only non-selected Anarchy lanes are disabled.
- Local smoke evidence:
  - Built setup DLL `/underlay /repo <temp-git-repo> /silent /json` returned `setup_operation = underlay`, `bootstrap_state = ready`, `install_scope = repo_underlay`, `runtime_present = false`, `marketplace_registered = false`, and `host_config_modified = false`.
  - Filesystem check confirmed no `plugins/anarchy-ai` root and no `.agents/plugins/marketplace.json`.
  - Filesystem check confirmed all six portable files, narrative register/projects, AGENTS.md awareness note, and Anarchy `.gitignore` block were materialized.

### AA-BUG-030: Plan-only refresh returned a failure exit code and direct EXE smoke surfaced UI

- Severity: Medium
- Status: Patched local for exit-code contract; direct windowless EXE smoke proof pending
- Component: Setup / CLI automation / Release proof
- Repro:
  - Run `/refresh /repo <repo> /silent /json` against a repo with portable schema drift.
  - Observe JSON output with `bootstrap_state = refresh_plan_ready`.
  - Automation receives exit code `1` even though the plan was produced successfully.
- Expected:
  - Plan-only refresh is a successful CLI operation.
  - `/silent /json` release smoke should be safe for automation and should not require closing a GUI.
- Actual:
  - `refresh_plan_ready` returned exit code `1`, causing smoke harnesses to treat a valid plan as failure.
  - During manual deployable smoke attempts, the user observed the setup UI launching and closed it, so direct EXE smoke was stopped rather than normalized as proof.
- Local patch notes:
  - `Program.IsSuccessfulCliBootstrapState` now treats `ready`, `source_authoring_bundle_ready`, and `refresh_plan_ready` as successful CLI states.
  - Setup tests now cover `refresh_plan_ready` as a successful CLI state while preserving `bootstrap_needed` as non-success.
- Acceptance:
  - Setup tests pass.
  - Rebuilt deployable carries the exit-code fix.
  - Before release promotion, run a controlled windowless direct EXE smoke for `/underlay`, `/refresh`, and `/refresh /apply` without launching the GUI.

### AA-BUG-031: Runtime install selected a Codex lane without re-enabling it

- Severity: High
- Status: Patched local (Workorders and Fissure config retests passed; Codex cache materialization still host-owned)
- Component: Setup / Codex host adapter / Duplicate-lane repair
- Repro:
  - Disable or remove the repo-local Anarchy plugin through Codex UI so `~/.codex/config.toml` carries `[plugins."anarchy-ai@anarchy-ai-repo-workorders"] enabled = false`.
  - Run repo-local Anarchy install/update for Workorders.
  - Inspect setup JSON.
- Expected:
  - A runtime install/update that selects `anarchy-ai@anarchy-ai-repo-workorders` as the primary Codex lane should make that selected lane enabled.
  - Non-selected Anarchy lanes may be disabled to prevent duplicate skills.
  - Non-Anarchy plugin config must remain untouched.
- Actual:
  - Setup reported `selected_codex_primary_lane = anarchy-ai@anarchy-ai-repo-workorders`.
  - `codex_materialization.codex_plugin_enabled = false`.
  - `bootstrap_state = ready` and `next_action = use_preflight_session`, which overstated active Codex usability.
- Evidence:
  - Workorders redeploy output on `2026-04-26` showed source plugin manifest `0.1.9`, Codex cache still `0.1.8`, and findings:
    - `source_plugin_version_not_materialized_in_codex_cache`
    - `codex_plugin_disabled`
  - Retesting Workorders after the patch showed:
    - `host_config_modified = true`
    - `actions_taken` included `enabled_selected_anarchy_codex_plugin_lane`
    - `codex_materialization.codex_plugin_enabled = true`
    - `source_plugin_version_not_materialized_in_codex_cache` remained because Codex had not refreshed the cache from `0.1.8` to `0.1.9`
  - Retesting Fissure / Docker-Builder-Project after the patch showed:
    - `selected_codex_primary_lane = anarchy-ai@anarchy-ai-repo-docker-builder-project`
    - `disabled_duplicate_codex_lanes = ["anarchy-ai@anarchy-ai-repo-workorders"]`
    - `duplicate_codex_skill_lanes_detected = true`
    - `codex_materialization.codex_plugin_enabled = true`
  - Retesting the user-profile lane on the second computer (`C:\Users\mherring`) showed:
    - pre-install assess had `codex_materialization.codex_plugin_enabled = false`, install-state missing, source/cache manifest `0.1.7`
    - install selected `anarchy-ai@anarchy-ai-user-profile`
    - `actions_taken` included `enabled_selected_anarchy_codex_plugin_lane`
    - post-install `codex_materialization.codex_plugin_enabled = true`
    - source manifest moved to `0.1.9` while Codex cache stayed at `0.1.7`, leaving only `source_plugin_version_not_materialized_in_codex_cache`
- Local patch notes:
  - Primary-lane reconciliation now sets the selected Anarchy Codex plugin key to `enabled = true` when an existing disabled section is present.
  - The same pass still disables only non-selected Anarchy plugin keys.
  - `host_config_modified` now reflects selected-lane enablement as well as duplicate-lane disablement.
- Acceptance:
  - Setup tests prove selected-lane re-enable, non-selected Anarchy disable, and unrelated plugin preservation.
  - Rebuild setup and repeat Workorders install/update.
  - Confirm Workorders returns `codex_materialization.codex_plugin_enabled = true`.
  - Confirm Fissure install enables the selected Fissure lane, disables the Workorders Anarchy lane, and preserves unrelated plugins.
  - Treat cache refresh to `0.1.9` as separate host-owned evidence until Codex materializes the matching cache entry.

### AA-BUG-032: Repo-local runtime marketplaces are correct host provenance but poor default repo-travel hygiene

- Severity: Medium
- Status: Documented local; product posture clarified
- Component: Setup / Marketplace discipline / Repo hygiene
- Repro:
  - Install Anarchy through the user-profile lane.
  - Install repo-local runtime bundles into Workorders and Fissure / Docker-Builder-Project.
  - Restart Codex and inspect the Plugins UI.
- Expected:
  - Normal consumer repos carry portable underlay truth without creating a durable Codex plugin distribution per repo.
  - User-profile is the normal runtime plugin distribution.
  - Repo-local runtime remains available for proving/debug only.
  - A repo can have Anarchy underlay present while the repo-local plugin distribution is unavailable.
- Actual:
  - Codex correctly listed separate Anarchy distributions because they came from separate marketplace roots:
    - `anarchy-ai@anarchy-ai-user-profile`
    - `anarchy-ai@anarchy-ai-repo-workorders`
    - `anarchy-ai@anarchy-ai-repo-docker-builder-project`
  - After selecting Fissure / Docker-Builder-Project, the Plugins UI showed that distribution checked while Workorders and user-profile were visible but not selected.
- Evidence:
  - Workorders and Fissure install outputs show separate `selected_codex_primary_lane` values derived from repo-scoped marketplace names.
  - Codex cache materialization still lagged source manifest `0.1.9`, proving marketplace visibility, enable-state, cache, and runtime activation are separate surfaces.
  - User observation after Codex restart: all three distributions were listed, but only Docker-Builder-Project was checked.
- Product direction:
  - Do not fight Codex's marketplace separation. It is good host provenance for different marketplace roots to remain distinct.
  - Do not normalize repo-local runtime as the default consumer repo shape.
  - `/underlay` is the repo-travel lane: portable schema, narrative, triage, guide, and hygiene only; no runtime, marketplace, MCP, or host config.
  - `/userprofile` is the normal runtime distribution lane.
  - `/repolocal` is explicit proving/debug runtime placement and may create a separate visible Codex distribution.
  - `plugin unavailable, underlay present` is an acceptable normal consumer state.
- Acceptance:
  - Setup and repo-install docs steer normal adoption to `/underlay` plus optional `/userprofile`.
  - Docs state that multiple visible Anarchy distributions are technically valid host provenance, but multiple active Anarchy runtime lanes are bad hygiene.
  - Consumer repo hygiene guidance keeps committed truth to portable underlay artifacts and excludes repo-local plugin bundles, marketplace pointers, EXE/PDB/runtime files, caches, and JSONL residue.

### AA-BUG-033: Selected Codex lane is not materialized when config key is absent

- Severity: High
- Status: Patched local
- Component: Setup / Codex host adapter / Duplicate-lane reconciliation
- Repro:
  - On a second computer, assess a repo-local BrainyMigrator install that was previously written on another Windows profile.
  - The copied install-state still records the prior profile path (`C:\Users\herri\...`) while the current machine path is `C:\Users\mherring\...`, so install-state is correctly invalid.
  - Run repo-local install for the current machine.
  - Or install a repo-local runtime into a repo with no existing selected Anarchy Codex config section and no enabled duplicate Anarchy sections to force a config rewrite.
- Expected:
  - Runtime install/update that selects `anarchy-ai@anarchy-ai-repo-<repo-slug>` as primary creates or enables the matching Codex plugin config section.
  - Post-install materialization should report `codex_materialization.codex_plugin_enabled = true`.
- Actual:
  - Install selected `anarchy-ai@anarchy-ai-repo-brainymigrator`.
  - It disabled duplicate Anarchy lanes for Workorders and user-profile.
  - It did not create the missing selected config section, so post-install `codex_materialization.codex_plugin_enabled = null` and findings still included:
    - `codex_plugin_enable_state_missing`
    - `codex_cache_root_missing`
    - `source_plugin_version_not_materialized_in_codex_cache`
  - `bootstrap_state = ready` therefore again overstated active Codex usability for the selected lane.
- Evidence:
  - BrainyMigrator second-computer output on `2026-04-26`:
    - pre-install assess correctly reported install-state path/root mismatches from the prior `herri` profile
    - install output selected `anarchy-ai@anarchy-ai-repo-brainymigrator`
    - `disabled_duplicate_codex_lanes = ["anarchy-ai@anarchy-ai-repo-workorders", "anarchy-ai@anarchy-ai-user-profile"]`
    - `actions_taken` included `disabled_duplicate_anarchy_codex_plugin_lanes` but not `enabled_selected_anarchy_codex_plugin_lane`
    - post-install `codex_plugin_enabled = null`
  - TheLinks second-computer output on `2026-04-26` showed the minimal no-duplicate variant:
    - pre-install assess reported `bootstrap_state = bootstrap_needed`, missing repo marketplace, no install-state, no runtime, and no Codex enable-state
    - install created the repo marketplace and plugin bundle, selected `anarchy-ai@anarchy-ai-repo-thelinks`, and wrote install-state
    - `disabled_duplicate_codex_lanes = []` and `duplicate_codex_skill_lanes_detected = false`, so no duplicate-lane write occurred
    - `actions_taken` did not include `enabled_selected_anarchy_codex_plugin_lane`
    - post-install `codex_plugin_enabled = null` with `codex_plugin_enable_state_missing`
- Local patch notes:
  - Primary-lane reconciliation now creates the selected `[plugins."anarchy-ai@..."] enabled = true` section when missing.
  - The same logic still enables an existing selected section when `enabled = false` or no `enabled` value is present.
  - The selected key can be materialized even when the Codex config file does not exist yet; parent directories are created as needed.
- Acceptance:
  - Setup tests prove a missing selected repo-local key is appended with `enabled = true`.
  - Re-run BrainyMigrator and TheLinks repo-local installs and confirm `codex_materialization.codex_plugin_enabled = true`.
  - Treat cache materialization (`codex_cache_root_missing` / source version missing in cache) as separate host-owned evidence until Codex creates the matching cache directory.

### AA-BUG-034: Setup GUI still presented repo-local runtime as the default lane

- Severity: Medium
- Status: Patched local
- Component: Setup GUI / Marketplace discipline / Product posture
- Repro:
  - Launch `AnarchyAi.Setup.exe` with no arguments.
  - Observe the default GUI lane and explanatory copy.
- Expected:
  - The GUI presents only the normal product lanes:
    - `Repo underlay` for repo adoption/travel
    - `User-profile install` for runtime tools
  - Repo-local runtime remains available through CLI for proving/debug, but is not the default visual path.
  - Repo-underlay UI text states that it does not install a runtime, create marketplace entries, register MCP, or touch host config.
- Actual:
  - The GUI defaulted to `Repo-local install`.
  - The intro text said repo-local install keeps the harness inside the repo so the delivery surface travels with the workspace.
  - The primary button said `Install Repo-Local`, which contradicted the clarified `/underlay` plus `/userprofile` product posture and made normal repo-travel look like repo-local runtime installation.
- Evidence:
  - Operator screenshot on `2026-04-26` showed:
    - lane group labeled `Install Lane`
    - selected `Repo-local install`
    - buttons `Assess Repo-Local` and `Install Repo-Local`
    - copy describing repo-local harness install for a selected repo
- Local patch notes:
  - GUI lane group now reads `Setup Lane`.
  - Default repo lane is `Repo underlay`.
  - Repo-underlay primary action runs `OperationMode.Underlay`.
  - Repo-underlay secondary action runs read-only schema refresh planning.
  - User-profile runtime install remains the only runtime install lane visible in the GUI.
  - Host target controls are disabled for repo underlay because no host runtime is registered.
  - GUI write disclosure now has a separate underlay disclosure that explicitly says no runtime bundle, marketplace, plugin registration, host config write, or runtime process is involved.
- Acceptance:
  - Setup tests cover underlay disclosure separation from runtime install wording.
  - Rebuild setup and launch the GUI.
  - Confirm visible lanes are `Repo underlay` and `User-profile install`.
  - Confirm repo-underlay write action returns `setup_operation = underlay`, `install_scope = repo_underlay`, `runtime_present = false`, `marketplace_registered = false`, and `host_config_modified = false`.

### AA-BUG-035: Codex scaffold convention may differ from Anarchy's observed user-profile path

- Severity: Medium
- Status: Open; documented as host-contract caveat
- Component: Setup / Codex plugin host contract / User-profile install
- Repro:
  - Compare current Anarchy user-profile install shape to Codex's bundled `plugin-creator` skill.
  - Anarchy user-profile currently writes:
    - marketplace: `~/.agents/plugins/marketplace.json`
    - source path: `./.codex/plugins/anarchy-ai`
    - plugin root: `~/.codex/plugins/anarchy-ai`
  - `plugin-creator` describes generic home-local plugin scaffolding as:
    - marketplace: `~/.agents/plugins/marketplace.json`
    - source path: `./plugins/<plugin-name>`
- Expected:
  - Installer docs distinguish evidence-backed local behavior from Codex's intended canonical plugin-scaffold convention.
  - A moving Codex host surface is not treated as settled doctrine without fresh proof.
- Actual:
  - Anarchy's current lane is backed by observed installs and fresh-session evidence, but may have been shaped by testing against earlier or buggy Codex behavior.
  - Without an explicit caveat, future sessions could treat `./.codex/plugins/anarchy-ai` as unquestionably canonical rather than evidence-backed and subject to host-contract verification.
- Evidence:
  - User supplied `plugin-creator` on `2026-04-26`; the skill states home-local plugins use `~/.agents/plugins/marketplace.json` plus marketplace-relative `./plugins/<plugin-name>`.
  - Prior Anarchy tests showed `./.codex/plugins/anarchy-ai` working for the user-profile lane, but also showed repeated Codex cache/materialization drift across `0.1.7`, `0.1.8`, and `0.1.9`.
  - User noted Codex has had frequent recent updates, so earlier observed behavior may not represent the intended long-term method.
- Required product direction:
  - Keep current Anarchy user-profile lane until fresh tests prove a better host-native method.
  - Treat `plugin-creator` as scaffold convention evidence, not as an automatic migration command.
  - If current Codex docs/scaffold/fresh smoke converge on another home-local layout, migrate explicitly with install-state and retirement handling rather than silently changing paths.
- Acceptance:
  - Truth matrix records the scaffold-vs-observed-path caveat.
  - Setup spec states the current lane is evidence-backed but not proven canonical.
  - A future verification pass compares official Codex docs, `plugin-creator` output, fresh-session plugin resolution, cache materialization, and config enable-state on the current Codex version.

### AA-BUG-036: Gov2gov requires repo-local marketplace discovery for underlay-only repos

- Severity: Medium
- Status: Patched in source; rebuild/redeploy proof pending
- Component: Runtime / Gov2Gov / Underlay posture / Startup discovery
- Repro:
  - Retire repo-local Anarchy marketplace registrations from a consumer repo that is intended to be underlay-only.
  - Keep the user-profile runtime lane active and Codex cache materialized at the current plugin version.
  - Run `run_gov2gov_migration` against the consumer repo after portable schema files are aligned with the canonical bundle.
- Expected:
  - Gov2gov distinguishes the repo's intended Anarchy posture:
    - `repo_underlay`: portable schema, narrative, triage, guide, and hygiene surfaces travel; no repo-local runtime marketplace is required.
    - `repo_local_runtime`: `.agents/plugins/marketplace.json` is expected startup discovery metadata for a repo-local plugin distribution.
  - For `repo_underlay`, missing or ignored `.agents/plugins/marketplace.json` does not produce `startup_discovery_path_weakened`.
  - Root `GOV2GOV-*` files are active migration packet files only. Completed migrations should transition back to reference mode with `AGENTS-schema-gov2gov-migration.json` present and root packet files absent.
- Actual:
  - Workorders gov2gov still treated missing `.agents/plugins/marketplace.json` as a startup discovery weakness after the repo was intentionally moved toward underlay-only posture.
  - `non_destructive_apply` refused to write because the only planned action was startup-discovery realignment, even though repo-local marketplace restoration would violate the current `/underlay` plus `/userprofile` product discipline.
- Evidence:
  - Workorders on `2026-04-26` after Codex finally materialized `0.1.9` in both repo-scoped and user-profile caches:
    - repo-local source and active Codex cache all reported plugin manifest `0.1.9`
    - schema pack refresh updated only `AGENTS-schema-governance.json` and `AGENTS-schema-narrative.json`
    - schema reality then reported `integrity = aligned` and no diverged canonical schema files
    - remaining partial posture was missing/ignored `.agents/plugins/marketplace.json` plus active `GOV2GOV-*` packet interpretation drift
    - subsequent gov2gov apply still refused because restoring startup discovery was the only planned action
- Required product direction:
  - Do not make Workorders reintroduce `.agents/plugins/marketplace.json` just to satisfy gov2gov if the repo is intended to be underlay-only.
  - Add posture-aware gov2gov semantics so underlay-only repos can complete or continue GOV2GOV work without repo-local runtime marketplace restoration.
  - Preserve the schema's active/reference mode distinction so completed migrations do not recreate root `GOV2GOV-*` packet files.
  - Keep repo-local marketplace restoration reserved for repos deliberately choosing `/repolocal` runtime proving/debug or repo-local runtime distribution.
- Acceptance:
  - Gov2gov plan/apply can identify or accept `repo_underlay` posture.
  - In `repo_underlay`, missing `.agents/plugins/marketplace.json` is informational or ignored, not `startup_discovery_path_weakened`.
  - `non_destructive_apply` can preserve reference mode with root `GOV2GOV-*` files absent after completion.
  - `non_destructive_apply` can materialize missing `GOV2GOV-*` companion files only when active artifact mode is explicitly requested or observed.
  - In `repo_local_runtime`, gov2gov still reports missing marketplace discovery as a real startup-discovery gap.
- Patch notes:
  - `is_schema_real_or_shadow_copied` and `run_gov2gov_migration` now accept optional `workspace_posture` values: `auto`, `repo_underlay`, `repo_local_runtime`, and `undetermined`.
  - Auto posture can infer `repo_underlay` from underlay `.gitignore` policy or portable underlay surfaces, while explicit `repo_underlay` remains available for caller-owned posture truth.
  - In `repo_underlay`, a missing repo-local `.agents/plugins/marketplace.json` startup surface is listed as ignored by posture and does not create `startup_discovery_path_weakened`.
  - In `repo_local_runtime`, the same missing marketplace surface remains a startup discovery gap.
  - `run_gov2gov_migration` now accepts `gov2gov_artifact_mode`: `auto`, `active`, or `reference`.
  - In `auto`, existing root `GOV2GOV-*` files resolve to active mode; absent root `GOV2GOV-*` files resolve to reference mode and are not recreated.
  - Explicit active mode can still materialize missing `GOV2GOV-hello.md`, `GOV2GOV-source-target-map.md`, `GOV2GOV-registry.json`, `GOV2GOV-rules.md`, and `GOV2GOV-pitfalls.md` in `non_destructive_apply` without creating repo-local marketplace discovery.
  - Plugin manifest release identity moved to `0.1.11` so Codex has a fresh cache key for the runtime contract change.
  - Added runtime tests for underlay posture suppression, repo-local-runtime preservation, reference-mode packet absence, and explicit active-mode GOV2GOV packet materialization.

### AA-BUG-005: Missing setup `self-check` command for active mount diagnostics

- Severity: Medium
- Status: Partially patched in setup source; host surfacing proof still pending
- Component: Setup / Diagnostics
- Repro:
  - Attempt to diagnose lane confusion across sessions.
- Expected:
  - Single diagnostic command reports active plugin root, marketplace path, config lane, and server identity.
- Actual:
  - Diagnosis requires manual file inspection and ad hoc shell checks.
- Evidence:
  - Multiple manual checks were required (`config.toml`, marketplace, plugin roots, manifests).
- Acceptance:
  - `AnarchyAi.Setup.exe /selfcheck /json` returns bounded diagnostics for mount truth.
  - Local patch notes:
    - setup parser now accepts `/status`, `/doctor`, `/selfcheck`, and `/self-check`
    - setup install/update now writes a versioned `.anarchy-ai/install-state.json` record into the owned plugin bundle
    - setup JSON now includes `install_state` with bounded missing/drift findings
    - current status output is lifecycle evidence, not proof that a given host has surfaced the plugin or MCP tools

### AA-BUG-006: Runtime lock recovery is not first-class in setup retry flow

- Severity: Medium
- Status: Open
- Component: Setup / Runtime lock handling
- Repro:
  - Update while `AnarchyAi.Mcp.Server.exe` is in use.
- Expected:
  - Setup offers guided lock release (`AssessRuntimeLock`, `SafeReleaseRuntimeLock`, `ForceReleaseRuntimeLock`) and retry path.
- Actual:
  - Error is returned, and operator must manually run stop script patterns.
- Evidence:
  - Access-denied lock incidents on runtime EXE during update/install attempts.
- Acceptance:
  - Setup can invoke/offer bounded release modes and immediately retry with explicit status.

### AA-BUG-007: Divergence reporting does not clearly separate canonical drift from repo breathing

- Severity: Medium
- Status: Open
- Component: Runtime / Schema reality UX
- Repro:
  - Keep richer `AGENTS.md` and companion startup documents.
  - Run schema-reality check.
- Expected:
  - Output clearly states only canonical bundle files are integrity-compared.
- Actual:
  - Divergence can be interpreted as objection to normal repo-level documentation growth.
- Evidence:
  - Operator confusion around why `AGENTS.md` richness appeared coupled to integrity signals.
- Required product direction:
  - Schema integrity comparison should name the canonical source used for the comparison.
  - Normal distribution default should require no extra local repo clone or operator step.
  - Canonical source priority should be:
    - current public AI-Links release by default
    - explicit local AI-Links source path only when the caller provides one
    - embedded bundle as offline fallback and installed-payload evidence
  - Embedded bundle comparison remains useful for answering "what did this installer carry?" but should not silently become the only answer to "current canonical schema" when public release lookup is available.
- Acceptance:
  - Result payload and docs include explicit "canonical surfaces compared" section.
  - Result payload includes the canonical source lane used for schema comparison (`local_source`, `public_release`, or `embedded_bundle`) and the source path or release reference when known.

### AA-BUG-008: No intentional-divergence allowlist for canonical schema evolution

- Severity: Medium
- Status: Open
- Component: Runtime / Policy surface
- Repro:
  - Intentionally evolve schema files in workspace while keeping operability.
- Expected:
  - Optional policy surface can classify approved divergence as expected.
- Actual:
  - All drift is reported uniformly as divergence/possession pressure.
- Evidence:
  - Plan-only migration repeatedly lists canonical mismatch with no "approved local divergence" lane.
- Acceptance:
  - Optional allowlist/policy file supports bounded intentional drift annotations.

### AA-BUG-009: Cross-session proven-state criteria are not enforced as automated environment tests

- Severity: Medium
- Status: Open
- Component: Test strategy
- Repro:
  - Apply install/config changes, restart session, compare visibility.
- Expected:
  - Automated proof lane validates repeatable behavior across fresh sessions.
- Actual:
  - Proof is mostly manual and chat-driven.
- Evidence:
  - Repeated restart-and-check cycles were needed to verify actual mount behavior.
- Acceptance:
  - Add environment proof script/tests for "change in session A, visible in session B."

### AA-BUG-010: Evidence qualification (`proven` vs `inferred`) is not enforced by doc update workflow

- Severity: Low
- Status: Open
- Component: Documentation governance
- Repro:
  - Update environment docs during rapid iteration.
- Expected:
  - Claims require explicit evidence tags and timestamps.
- Actual:
  - Drift risk remains when updates are made faster than proof captures.
- Evidence:
  - Multiple rounds of claim correction were needed after live behavior checks.
- Acceptance:
  - Doc update checklist requires evidence lane, artifact reference, and proof timestamp.

### AA-BUG-011: Setup disclosure/help can drift from current runtime behavior and tool posture

- Severity: Medium
- Status: Open
- Component: Setup / Generated disclosure text
- Repro:
  - Rebuild and compare help/disclosure text to actual mounted tool surface and lane behavior.
- Expected:
  - Help/disclosure are generated from current runtime facts and versioned surfaces.
- Actual:
  - Prior stale builds emitted old capability statements.
- Evidence:
  - Difference observed between stale target EXE text and current source behavior.
- Acceptance:
  - Build-time validation fails when disclosure/help output diverges from runtime/tool metadata.

### AA-BUG-012: Install completion does not require post-install active verification

- Severity: Medium
- Status: Open
- Component: Setup / Completion criteria
- Repro:
  - Install completes and reports ready; mount path may still be legacy or inconsistent.
- Expected:
  - Completion requires immediate post-install assess + route verification.
- Actual:
  - "Ready" can be reported before lane conflicts are fully surfaced.
- Evidence:
  - Additional manual checks were required after reported success to validate true active lane.
- Acceptance:
  - Install success path includes post-install assess and lane-consistency verification.

### AA-BUG-013: Missing harness-level mount diagnostics/action item method

- Severity: High
- Status: Open
- Component: Harness API / Operator diagnostics
- Repro:
  - Install and registration surfaces are healthy on disk.
  - New or restarted Codex thread cannot see Anarchy MCP server/tools (`unknown MCP server 'anarchy-ai'` / empty resources).
- Expected:
  - Harness stack provides an explicit diagnostic action path that the agent can invoke or route to when session-level mount visibility is inconsistent.
- Actual:
  - Diagnosis requires manual multi-file inspection and ad hoc shell steps; no single harness-directed diagnostic contract exists.
- Evidence:
  - User-profile and repo-local marketplace/runtime files present.
  - Session still returned unknown/empty MCP visibility in current thread.
  - Deployment change report (2026-04-14) confirmed: `list_mcp_resources(server="anarchy-ai")` returned unknown MCP server; global resource/template lists returned empty arrays; runtime executables verified present in both `~/.codex/plugins/anarchy-ai/` and repo-local `plugins/anarchy-ai-*/`; marketplace entries verified in both `.agents/plugins/marketplace.json` locations.
- Required product direction:
  - Add a bounded diagnostic method in harness scope (or setup-backed harness companion) that returns:
    - active lane intent (`repo_local` vs `user_profile`)
    - expected plugin root and marketplace path
    - legacy custom MCP block presence
    - runtime executable presence checks
    - explicit next action item when mount mismatch is detected
- Proposed method surface:
  - `diagnose_harness_mount_state` (or equivalent stable name)
  - read-only
  - structured output for agent-safe action routing
- Acceptance:
  - One callable method returns actionable mount-state diagnostics with a single recommended next action.
  - Output distinguishes:
    - install/config healthy but session not mounted
    - stale config/path mismatch
    - runtime missing/corrupt
  - Result is documented in skill/setup guidance so agents stop guessing.

### AA-BUG-014: Missing harness method to trace execution path step-by-step

- Severity: High
- Status: Open
- Component: Harness API / Debuggability
- Repro:
  - Agent spends extended time attempting fixes without isolating the true contradiction.
  - Manual line-by-line code/comment walkthrough makes the fix obvious quickly.
- Expected:
  - Harness provides a structured execution-path tracer that externalizes hidden assumptions and identifies the first failing step.
- Actual:
  - No single harness method emits a bounded, stepwise intent-vs-observed-state trace.
- Evidence:
  - User-reported case: prolonged failed troubleshooting resolved after explicit line-by-line documentation pass.
- Required product direction:
  - Add a bounded read-only harness method that:
    - enumerates execution steps in order
    - records expected condition per step
    - records observed condition per step
    - marks first contradiction pivot
    - returns one primary corrective action item
- Proposed method surface:
  - `trace_execution_path` (renamed from `explain_failure_path` - the word "failure" activates the wrong concept first per the negation-mitigation research; "trace" and "execution" are affirmative action words)
  - read-only
  - deterministic structured response
- Required output fields (these are required, not optional - a trace without a next action becomes a satisfying stopping point instead of a repair action):
  - `path_name`
  - `steps[]` with:
    - `step_id`
    - `intent`
    - `expected_state`
    - `observed_state`
    - `status` (`pass|fail|blocked|unknown`)
    - `evidence`
  - `first_contradiction_step_id` (renamed from `first_failure_step_id`)
  - `root_contradiction`
  - `recommended_next_action` (REQUIRED - the tool always produces an action, even when that action is report-to-human)
  - `recommended_next_call` (REQUIRED - the tool always names the next callable method)
- Relationship to AA-BUG-013:
  - `diagnose_harness_mount_state` is a narrow diagnostic for mount-layer issues.
  - `trace_execution_path` is a broader tracer for any harness lane, including mount flow.
- Acceptance:
  - Method consistently identifies the first contradiction in known broken scenarios.
  - Output always includes `recommended_next_action` and `recommended_next_call` - a trace that ends at diagnosis without an action is incomplete.
  - Output is concise enough for agent decisioning and detailed enough for human validation.
  - Skill/setup docs reference it as the default "stop guessing" lane before destructive retries.

### AA-BUG-015: Codex Plugins UI card title can drift from current marketplace and plugin metadata

- Severity: Medium
- Status: Patched repo metadata (pending Codex state refresh proof)
- Component: Setup / Marketplace identity / Codex plugin catalog state
- Repro:
  - Open the new Codex Plugins view after a repo-local and user-profile Anarchy-AI marketplace are both present.
  - Open the marketplace dropdown and compare the section titles to the visible plugin card title.
- Expected:
  - The marketplace dropdown shows branded marketplace titles.
  - The plugin card title also stays branded and does not expose stale internal identifiers.
- Actual:
  - The marketplace dropdown can show the correct branded titles such as `Anarchy-AI Local (AI-Links)` and `Anarchy-AI User Profile`, while the plugin card still shows a stale hash-shaped identifier such as `anarchy-local-ai-links-dae7a4e7`.
- Evidence:
  - Official Codex plugin docs say marketplace `interface.displayName` is the marketplace title shown in Codex and plugin `interface.displayName` controls install-surface presentation.
  - Live repo marketplace now uses `name = "anarchy-ai-herringms-local-ai-links"` with `displayName = "Anarchy-AI Local (AI-Links)"`.
  - Live user-profile marketplace now uses `name = "anarchy-ai-herringms-user-profile"` with `displayName = "Anarchy-AI User Profile"`.
  - Live repo-local and home-local plugin manifests both use stable plugin identity `name = "anarchy-ai-herringms"` and `interface.displayName = "Anarchy-AI"`.
  - The stale `anarchy-local-ai-links-dae7a4e7` label was no longer present in the current marketplace files or live plugin manifests after the metadata patch.
  - Official docs also say local plugins install into `~/.codex/plugins/cache/$MARKETPLACE_NAME/$PLUGIN_NAME/local/`, but no Anarchy-AI cache copy was observed on this machine during the same check.
- Acceptance:
  - Repo-local marketplace root uses a branded repo-slug identifier without a path-hash suffix.
  - User-profile marketplace root also uses a branded stable identifier.
  - After Codex restart or install-state refresh, plugin cards no longer show stale path-derived marketplace identifiers.
  - If Codex is sourcing card titles from a separate cached catalog/install state, the truth matrix documents that surface explicitly instead of attributing it to current repo-authored manifests.

### AA-BUG-016: Codex plugin detail page can fail on BOM-prefixed generated manifests

- Severity: High
- Status: Patched locally, fresh-session plugin resolution proven, full Codex install-state proof still pending
- Component: Build / Setup payload / Plugin manifest generation
- Repro:
  - Install the generated home-local plugin bundle.
  - Open the Anarchy-AI plugin card in the Codex Plugins UI.
- Expected:
  - Codex loads the plugin detail page and can proceed to install/enable the plugin.
- Actual:
  - Codex reports `Failed to load plugin` with `missing or invalid .codex-plugin/plugin.json` even though the file exists on disk.
- Evidence:
  - The live generated `plugins/anarchy-ai/.codex-plugin/plugin.json`, `plugins/anarchy-ai/.mcp.json`, and `plugins/anarchy-ai/schemas/schema-bundle.manifest.json` were emitted with a UTF-8 byte-order mark (`EF BB BF`) while the working OpenAI plugin manifests under `~/.codex/.tmp/plugins/` were plain UTF-8 without BOM.
  - The plugin card itself can still appear because Codex can read the marketplace entry without successfully parsing the plugin-local manifest.
  - After updating the build and bootstrap writers to emit UTF-8 without BOM, the repo-source and home-local plugin manifests were regenerated and confirmed to start directly with `{` instead of the BOM prefix.
  - After reinstall and restart, the fresh-session mention `plugin://anarchy-ai@anarchy-ai-user-profile` resolved successfully, which did not happen while the invalid-manifest failure was active.
- Acceptance:
  - Repo-source `plugin.json`, `.mcp.json`, and `schema-bundle.manifest.json` are emitted as UTF-8 without BOM.
  - The rebuilt setup EXE carries those normalized files into the home-local install.
  - Codex can open the Anarchy-AI plugin detail page without the `missing or invalid .codex-plugin/plugin.json` error.

### AA-BUG-017: Product language can overclaim enforcement where the harness only provides influence, materialization, or proof

- Severity: Medium
- Status: Open
- Component: Documentation / Product language / Setup disclosure
- Repro:
  - Rapidly update setup, harness, schema, and plugin docs during install-lane work.
  - Reuse terms such as `enforced`, `guaranteed`, or `default` after live host behavior proves weaker than the wording.
- Expected:
  - Product language distinguishes:
    - influence surfaces such as skills, startup prompts, and AGENTS guidance
    - materialization surfaces such as files, manifests, marketplace entries, and installed bundles
    - proof surfaces such as fresh-session reachability, tool calls, install-state records, and diagnostics
- Actual:
  - Older docs can imply host-level enforcement or guaranteed install behavior when the proven state is narrower.
- Evidence:
  - Codex plugin registration changed across app versions and broke previously working assumptions.
  - Skills and startup instructions remained useful for consistent team experience but did not guarantee behavior.
  - Repo-local install remains documented and writable, while Codex-native plugin surfacing for repo-local remains unproven in the truth matrix.
  - Earlier install docs used `enforced enough to matter` and `guaranteed install story` language despite the scratchpad doctrine that the underlay conditions rather than compels.
- Acceptance:
  - Docs and setup disclosures use `operational`, `callable`, `materialized`, `proven`, `inferred`, or `influence` instead of enforcement language unless a real blocking mechanism exists.
  - Architecture docs state that the setup EXE is a carrier/operator surface, not the sole install truth authority.
  - Future installer lifecycle work adds declared install plans, target adapters, install-state records, doctor/status diagnostics, repair actions, and catalog validation before claiming lifecycle maturity.
  - Truth matrix promotion remains the only path from inferred host behavior to proven host behavior.

## Notes

- These bug reports are about deployment and harness ergonomics, not about constraining repo expression in `AGENTS.md` or companion docs.
- Rich repo-level guidance is expected; integrity checks should remain bounded to canonical schema surfaces.
