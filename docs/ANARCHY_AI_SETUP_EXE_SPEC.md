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
- setup keeps repo-local bundle materialization bounded by the selected repo root while keeping the visible plugin identity and MCP server key stable as `anarchy-ai`
- this preserves predictable tool syntax like `mcp__anarchy_ai__...` without carrying path-derived suffixes in the installed bundle directory

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
  - plugin bundle under `./plugins/anarchy-ai`
  - marketplace under `./.agents/plugins/marketplace.json`
- `user-profile`
  - plugin bundle under `~/.codex/plugins/anarchy-ai`
  - marketplace under `~/.agents/plugins/marketplace.json`

The generated marketplace root should be repo-scoped for repo-local installs, not globally reused.
Keep the top-level marketplace `name` branded and readable because current Codex plugin surfaces can expose that identifier directly even though the official docs describe `interface.displayName` as the marketplace title.

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

Source-authoring assessment boundary:

- when `/assess /repolocal` or `/status /repolocal` targets the `AI-Links` source repo, setup must detect `plugins/anarchy-ai` as a repo-authored source bundle
- source-authoring detection is read-only; it must not silently turn `AI-Links` into its own generated consumer install
- when the source bundle is complete, setup reports `source_authoring_bundle_present = true`, `source_authoring_bundle_state = complete`, and `bootstrap_state = source_authoring_bundle_ready`
- core source-bundle surfaces such as contracts, runtime, skill, and `schemas/schema-bundle.manifest.json` must not be reported missing merely because the generated consumer target directory does not exist
- the absent generated consumer marketplace is not a blocking missing component in this read-only source-authoring state
- `paths.origin` points at the source repo, and `paths.source` points at `plugins/anarchy-ai`
- because the plain repo-local consumer target is also `plugins/anarchy-ai`, setup must block `/install /repolocal` and `/update /repolocal` when the selected repo is the `AI-Links` source repo
- the next action for this state is source-build/user-profile-install guidance, not unbounded self-registration

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
- `/status`
- `/doctor`, `/selfcheck`, and `/self-check` as status aliases
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
AnarchyAi.Setup.exe /status /userprofile /silent /json
AnarchyAi.Setup.exe /update /userprofile /silent /json /sourcepath "C:\path\to\AI-Links"
AnarchyAi.Setup.exe /install /repolocal /repo "C:\path\to\other-repo" /silent /json
```

### CLI rules

- `/silent` suppresses all prompts and the GUI
- `/json` emits machine-readable result to stdout
- `/repo` overrides repo auto-detection
- `/repolocal` installs or assesses through the selected repo root
- `/userprofile` installs or assesses through the current user profile
- `/status` is read-only lifecycle inspection
- `/doctor`, `/selfcheck`, and `/self-check` are aliases for `/status`
- `/sourcepath` allows local source refresh without depending on public TLS
- install seeds missing portable root schema files when a workspace root is targeted (`/repolocal`, or `/userprofile` with explicit `/repo`)
- `/refreshschemas` means force-refresh the portable root schema family only when a workspace root is targeted
- help aliases should print plain-text usage plus a generated repo-availability summary instead of raw JSON
- the help summary should tell the actor what Anarchy-AI adds here and what it changes in the repo

## Result Contract

CLI output should remain consistent with the current bootstrap lane.

Expected result shape:

- `setup_operation`
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
- `source_authoring_bundle_present`
- `source_authoring_bundle_state` when source-authoring detection applies
- `actions_taken`
- `missing_components`
- `safe_repairs`
- `next_action`
- `install_state`
  - `schema_version`
  - `state_path`
  - `state_present`
  - `state_written`
  - `state_valid`
  - `findings`
  - `warnings`
  - `recorded_*` fields when a valid or readable install-state file exists
  - target fields such as `recorded_target_id`, `recorded_target_kind`, and `recorded_target_root`
  - workspace fields such as `recorded_workspace_root` and `recorded_workspace_schema_targeted`
  - operation evidence such as `recorded_managed_operation_count`
- `codex_materialization` when Codex is targeted
  - `marketplace_name`
  - `plugin_name`
  - `codex_config_path`
  - `config_plugin_key`
  - `codex_plugin_enabled`
  - `expected_cache_root`
  - `source_plugin_manifest_version`
  - `cache_entries`
  - `source_version_present_in_cache`
  - bounded findings such as `source_plugin_version_not_materialized_in_codex_cache`
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
- `paths.destination.files.install_state_file_path` points at the setup-owned lifecycle state file

Install lock semantics:

- when install finds one or more existing bundle files locked and skips their refresh, setup reports:
  - `bootstrap_state = registration_refresh_needed`
  - `missing_components` includes `locked_bundle_surface_write_skipped`
  - `next_action = release_runtime_lock_and_retry_install`
- this presents a bounded repair path instead of a hard crash and keeps `ready` state honest

Lifecycle state semantics:

- install/update writes `.anarchy-ai/install-state.json` inside the owned plugin bundle
- the install-state record is versioned as `anarchy.install-state.v2`
- the record separates `target` from `workspace`
- `target` is the install identity:
  - `kind = home` for `/userprofile`
  - `kind = project` for `/repolocal`
  - target root, plugin root, marketplace path, install-state path, runtime path, and MCP server name live under this object
- `workspace` is the optional repo/schema target used for portable schema seeding and adoption checks
- a `/userprofile` install is not invalid merely because a later `/status /userprofile /repo <different-repo>` points at a different workspace
- that different workspace is reported as a warning about the last workspace target, not as a broken home install
- the record includes `managed_operations` so future doctor/repair/uninstall work can inspect and replay setup-owned surfaces instead of guessing from file presence
- legacy flat fields remain in the state file for older readers, but v2 validation uses the target/workspace split
- `/status` reads that record and compares install-target facts to the current resolved destination paths
- when `/status` cannot find the record, it reports `install_state_missing` and `next_action = run_install_to_write_install_state`
- when `/status` finds a mismatched or invalid record, it reports bounded `install_state_*` findings and points to `rerun_install_to_refresh_install_state`
- this is lifecycle evidence, not host UI proof; fresh-session tool reachability still belongs in the truth matrix

ECC comparison note:

- ECC's useful lesson is install lifecycle discipline: manifest/request planning, target adapters, install-state records, doctor/repair/uninstall operations, and managed-operation tracking
- Anarchy should adopt that lifecycle shape without adopting ECC's product claims or treating instructions/skills as enforcement
- for Anarchy, target adapters describe delivery/adoption surfaces; the underlay still works by shaping the path before execution, not by acting as a control plane during execution

The setup exe preserves these semantics so:

- agents use one install contract for every lane
- docs describe one install lane shared by script and exe
- delivery gets cleaner while the meaning layer stays stable

## Agent-Facing Expectations

Agent use of the setup exe should remain operationally similar to the current bootstrap script.

That means:

- agents can assess readiness
- agents can inspect lifecycle status without trusting plugin UI visibility
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
- Codex may render a repo-local marketplace/plugin source in the Plugins UI before the matching plugin manifest version is materialized in the chat/runtime cache

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
- reject `.NET SDK` paths that live inside the source workspace
- regenerate `plugin.json` from branding/path canon, including the release-canon plugin manifest version
- sync `plugins/anarchy-ai/schemas/schema-bundle.manifest.json` hashes from current source schema files before publish
- carry concrete narrative register/record templates under `plugins/anarchy-ai/templates/narratives/`
- generate `plugins/anarchy-ai/README.md` from `docs/ANARCHY_AI_PLUGIN_README_SOURCE.md` using destination-relative install facts
- run both the path-canon audit and the documentation-truth audit before publish succeeds
- publish the MCP server runtime first
- stage a temporary plugin payload carrying that freshly published runtime
- publish the setup project with temp `obj/bin/publish` lanes under `AppData\Local\Temp`
- refresh the local generated `plugins/AnarchyAi.Setup.exe`

That keeps setup regeneration machine-local and avoids synced-workspace build churn.

Build prerequisite boundary:

- .NET SDK/runtime prerequisites belong in a non-workspace user/machine-local lane, such as `%USERPROFILE%\.dotnet`, `%LOCALAPPDATA%`, or `C:\Program Files\dotnet`
- NuGet/package caches and restore scratch also belong outside the synced repo/workspace
- do not install a .NET SDK, runtime, restore cache, or package cache into this repo or any consumer repo
- repo-local install means the Anarchy plugin bundle can live under the repo; it does not mean build prerequisites live there

## Short Release Checklist

Before handing out a local setup executable:

1. Build setup:
   - `powershell -ExecutionPolicy Bypass -File .\harness\setup\scripts\build-self-contained-exe.ps1 -Configuration Release`
2. Confirm build output:
   - `status = "completed"`
   - `plugin_payload_staged = true`
   - `published_runtime_executable` is populated
   - `copied_to_plugins = true`
3. Confirm the plugin manifest version moved when plugin-facing behavior changed:
   - `Get-Content .\plugins\anarchy-ai\.codex-plugin\plugin.json`
   - cache-sensitive releases must not reuse the prior published `version`
   - the `2026-04-25` cache-invalidation test release uses `0.1.9`
4. Smoke install into a throwaway repo with a `.git` marker:
   - run `plugins\AnarchyAi.Setup.exe /install /repolocal /repo <throwaway-repo> /silent /json`
   - require `bootstrap_state = "ready"`
   - require `install_state.state_valid = true`
5. Confirm the extracted runtime contains current tool strings:
   - `direction_assist_test`
   - `verify_config_materialization`
   - `assess_harness_gap_state`
   - `preflight_session`
   - `compile_active_work_state`
   - `run_gov2gov_migration`
   - `is_schema_real_or_shadow_copied`
6. Run setup tests:
   - `dotnet test .\harness\setup\tests\AnarchyAi.Setup.Tests.csproj --no-restore -p:RestoreFallbackFolders=`
7. Run path and documentation audits:
   - `powershell -ExecutionPolicy Bypass -File .\harness\pathing\scripts\test-path-canon-compliance.ps1`
   - `powershell -ExecutionPolicy Bypass -File .\docs\scripts\test-documentation-truth-compliance.ps1`
8. Confirm copied deployable timestamp:
   - `Get-Item .\plugins\AnarchyAi.Setup.exe`
   - do not distribute if the timestamp predates the latest successful build output
9. Confirm source-only repo status:
   - no large setup EXE should be staged
   - no refreshed runtime binary should be required for commit

## Current Distribution Caveats

These caveats do not block handing out the generated setup executable, but they define what the executable does and does not prove.

1. Runtime-lock release is not yet first-class.
   - Setup reports runtime lock failures with bounded repair names.
   - It does not yet provide `AssessRuntimeLock`, `SafeReleaseRuntimeLock`, or `ForceReleaseRuntimeLock` as setup actions.
   - If the runtime executable is in use during install/update, close the owning app/session or run the documented release flow before retrying.
2. Post-install active host verification is still outside install completion.
   - A successful install proves files, marketplace entries, install-state, and setup-readable readiness.
   - It does not prove a fresh host session has mounted the plugin or exposed the MCP tools.
   - Fresh-session host visibility still belongs in the environment truth matrix.
3. Codex plugin UI compatibility is a host-adapter lane.
   - Setup/runtime distribution can be valid while Codex plugin UI indexing or cache state is stale.
   - Do not use plugin-card visibility alone as proof that setup or runtime source is wrong.
   - Do not use plugin-card visibility alone as proof that the active chat/runtime cache matches the source bundle version.
   - Use setup status, smoke install, extracted runtime checks, and direct tool reachability as the stronger evidence path.
4. Codex cache invalidation depends on host behavior and manifest version movement.
   - The setup build now sources `plugin.json.version` from branding canon instead of a hard-coded build-script literal.
   - A plugin-facing release should bump that version before distribution.
   - When both repo-local and user-profile lanes are enabled in Codex, the active surfaced skill cache may still require fresh-session evidence rather than assumption.
   - Setup's `codex_materialization` report can show source/cache disagreement, but it still cannot prove which cache a running chat selected unless a live runtime/tool call reports that path.

