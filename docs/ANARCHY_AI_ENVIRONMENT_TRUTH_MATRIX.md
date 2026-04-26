# Anarchy-AI Environment Truth Matrix

## Purpose

This file separates what is proven from what is still inferred in the current Codex plus Anarchy-AI environment.

Use this as the environment companion to:

- `ANARCHY_AI_SETUP_EXE_SPEC.md`
- `ANARCHY_AI_REPO_INSTALL_PROCESS.md`
- `ANARCHY_AI_HARNESS_ARCHITECTURE.md`

Date baseline for this matrix: `2026-04-15`.

## Headline

All schema-family artifacts, contracts, docs, disclaimers, and install assertions remain authored in the repo and are published into the standalone installer from that repo-authored source.

Install surfaces are then resolved relative to the destination:

- the repo is the canonical source of authored truth
- the installer is a published carrier
- observed installed machine state is separate evidence, not a replacement source of truth

## Current Plugin Compatibility Override

As of `2026-04-24`, the installed Anarchy-AI Codex plugin surface is treated as stale and incompatible with current Codex behavior until the plugin adapter repair is completed and re-proven.

Implications:

- do not use current Codex plugin visibility as evidence for or against harness source correctness
- do not let plugin mount failures drive schema or core harness design changes
- keep the April 15 Codex plugin observations as historical evidence for the old app/plugin boundary, not as current proof
- current source work should target the harness, setup lifecycle, contracts, docs, and proof discipline directly
- promotion back to `proven` requires the same identifiable-change plus fresh-session repeatability standard defined below

## Evidence Qualification Model

Use these evidence levels for environment claims.

### Proven

A claim is `proven` only when all are true:

1. identifiable change
   - the mutation is concrete and named (file, setting, command, or installer action)
2. observable effect in a fresh boundary
   - the expected behavior appears in a new app or session boundary, not only the same live context
3. repeatable effect
   - the behavior can be reproduced again from the same mutation path
4. evidence artifacts
   - at least one durable artifact exists (config, file, log, or command output)

### Inferred

A claim is `inferred` when it explains observations but does not satisfy the full proven promotion test above.

Promotion rule:

- do not upgrade inferred claims to proven until the session-boundary repeatability check is captured

## Proven Facts

### 1. Codex personal marketplace path is real

Proven by local file presence and current install output:

- user-profile marketplace path: `C:\Users\mherring\.agents\plugins\marketplace.json`
- repo-local marketplace path: `<repo>\.agents\plugins\marketplace.json`

### 2. Codex user-profile plugin root for this install is `~/.codex/plugins/anarchy-ai`

Proven by controlled local install on `2026-04-15`:

- command:
  - `.\plugins\AnarchyAi.Setup.exe /install /userprofile /silent /json`
- resulting bundle root:
  - `C:\Users\mherring\.codex\plugins\anarchy-ai`
- resulting plugin surfaces observed on disk:
  - `.codex-plugin\plugin.json`
  - `.mcp.json`
  - `runtime\win-x64\AnarchyAi.Mcp.Server.exe`
  - `contracts\`
  - `schemas\`

### 3. Personal marketplace `source.path` is marketplace-root-relative

Proven by the controlled local install output and resulting marketplace file:

- file: `C:\Users\mherring\.agents\plugins\marketplace.json`
- observed Anarchy user-profile entry:
  - `name = "anarchy-ai"`
  - `source.path = "./.codex/plugins/anarchy-ai"`
  - `policy.installation = "INSTALLED_BY_DEFAULT"`

### 4. Codex home readiness is plugin-marketplace-first

Proven by current setup behavior and assess output:

- command:
  - `.\plugins\AnarchyAi.Setup.exe /assess /userprofile /silent /json`
- observed behavior:
  - `paths.destination.directories.plugin_root_directory_path = "C:\\Users\\mherring\\.codex\\plugins\\anarchy-ai"`
  - `registration_mode = "plugin_marketplace"`
- readiness model in current local setup build does not require a custom MCP config block to describe the intended ready lane

### 5. Successful user-profile install did not require or write `[mcp_servers.anarchy-ai]`

Proven by direct file inspection after controlled install:

- file: `C:\Users\mherring\.codex\config.toml`
- observed contents remained limited to existing app settings such as model, windows, and playwright configuration
- no `[mcp_servers.anarchy-ai]` block was present after the successful `userprofile` install
- older legacy `[mcp_servers.anarchy-ai-herringms]` blocks remain cleanup evidence rather than the intended ready lane

### 6. `resources/list` and `resources/templates/list` are not valid Anarchy presence checks

Proven by current runtime design and companion docs:

- the runtime is tools-first
- empty resource or template discovery is not itself evidence of missing runtime presence

### 7. Repo-authored publish flow is now a first-class install truth source

Proven by current local source and build behavior:

- canonical installed README source doc:
  - `docs/ANARCHY_AI_PLUGIN_README_SOURCE.md`
- generated published README:
  - `plugins/anarchy-ai/README.md`
- current build helper:
  - `harness/setup/scripts/build-self-contained-exe.ps1`
- the build helper renders destination-relative install facts into the published README and rejects stale source-layout-relative hops such as `../../../`
- setup/bootstrap/health outputs now use one nested `paths.origin|source|destination` model instead of mixing flat `workspace_root` / `repo_root` / `plugin_root` fields

### 8. Current Anarchy runtime binary and source surface still expose the five harness tools

Proven by current source registration and packaged runtime delivery:

- `preflight_session`
- `compile_active_work_state`
- `is_schema_real_or_shadow_copied`
- `assess_harness_gap_state`
- `run_gov2gov_migration`

Test-lane addition:

- `direction_assist_test` is intentionally outside the five-core model and should be treated as experimental unless explicitly expected.

### 9. Fresh-session plugin mention resolution now works for the user-profile install

Proven by a fresh Codex session on `2026-04-15` after the rebuilt no-BOM home-local reinstall:

- user mention:
  - `[@anarchy-ai](plugin://anarchy-ai@anarchy-ai-user-profile)`
- observed effect:
  - Codex resolved the user-profile plugin reference and exposed Anarchy-AI plugin-associated capabilities in-session
- what this proves:
  - the current personal marketplace entry and home-local plugin manifest are loadable enough for Codex to resolve the plugin in a fresh session boundary
- what this does not yet prove:
  - Codex-managed cache materialization under `~/.codex/plugins/cache/...`
  - persistent plugin enable-state entries in `~/.codex/config.toml`

### 10. The `2026-04-25` setup deployable carries the current staged MCP runtime

Proven by local build output and a throwaway-repo smoke install on `2026-04-25`:

- build command:
  - `powershell -ExecutionPolicy Bypass -File .\harness\setup\scripts\build-self-contained-exe.ps1 -Configuration Release`
- build output:
  - `status = "completed"`
  - `plugin_payload_staged = true`
  - historical run: `published_runtime_executable = "C:\\Users\\herri\\AppData\\Local\\Temp\\ai-links-setup-build\\server-publish\\AnarchyAi.Mcp.Server.exe"`
  - current build helper lane: `%LOCALAPPDATA%\Anarchy-AI\AI-Links\setup-build`
  - `target_executable = "C:\\Users\\herri\\OneDrive - Braintek LLC\\Documents\\GitHub\\AI-Links\\plugins\\AnarchyAi.Setup.exe"`
- smoke install:
  - setup EXE installed into a generated throwaway repo with a `.git` marker
  - `bootstrap_state = "ready"`
  - `install_state.state_valid = true`
  - extracted runtime length was `70808918` bytes, matching the freshly published staged runtime rather than the stale tracked runtime
- extracted runtime contained current expected tool strings:
  - `direction_assist_test`
  - `verify_config_materialization`
  - `assess_harness_gap_state`
  - `preflight_session`
  - `compile_active_work_state`
  - `run_gov2gov_migration`
  - `is_schema_real_or_shadow_copied`
- what this proves:
  - the local generated setup EXE can carry the current runtime payload without committing the large setup EXE or refreshing the tracked runtime binary
  - the staged-payload publish path fixed the stale-runtime packaging class recorded as `AA-BUG-018`
- what this does not yet prove:
  - fresh Codex plugin surfacing from that throwaway repo
  - cross-device repeatability
  - Claude Code or Claude Desktop host surfacing

### 11. Setup source now separates install target from workspace target

Proven at source/test level on `2026-04-25` after comparing ECC's lifecycle model:

- ECC uses target adapters and install-state records keyed to the actual install target, not to arbitrary caller context.
- Anarchy setup now writes `anarchy.install-state.v2` with:
  - `target` for the install identity
  - `workspace` for the optional repo/schema target
  - `source` for payload/update provenance
  - `managed_operations` for setup-owned surfaces
- New setup regression test:
  - constructs a user-profile install-state with one recorded workspace
  - inspects it while passing a different workspace
  - asserts `install_state.state_valid = true`
  - asserts warning `last_workspace_target_differs_from_current_request`

What this proves:

- source behavior no longer treats a different `/repo` argument as a broken user-profile runtime.

What this does not yet prove:

- rebuilt setup EXE behavior after republish
- Workorders/Fissure repeat behavior from the generated deployable
- Codex cache behavior after install-state v2 is present

### 12. Setup source now distinguishes AI-Links authoring bundle from generated consumer install target

Proven at source/test and local deployable-smoke level on `2026-04-25` after `AA-BUG-021`:

- A setup regression test creates a repo with `.git` plus an AI-Links-style `plugins/anarchy-ai` source bundle.
- It does not run a repo-local install or mutate the plain repo-local consumer target (`plugins/anarchy-ai`).
- `/assess /repolocal` returns:
  - `source_authoring_bundle_present = true`
  - `source_authoring_bundle_state = complete`
  - `bootstrap_state = source_authoring_bundle_ready`
  - `runtime_present = true` from the source bundle
  - no `schema_bundle_manifest_missing`
  - no `bundled_runtime_missing`
  - no `missing_contract:*`
  - no `repo_marketplace_missing`
- Path roles stay separated:
  - `paths.origin` points at the source repo
  - `paths.source` points at `plugins/anarchy-ai`
- `paths.destination` may resolve to the same plain `plugins/anarchy-ai` path in AI-Links, so source-repo markers are what keep read-only source assessment distinct from consumer install mutation.
- After rebuild, the generated `plugins/AnarchyAi.Setup.exe` was run against the AI-Links source repo and returned:
  - `bootstrap_state = source_authoring_bundle_ready`
  - `source_authoring_bundle_present = true`
  - `source_authoring_bundle_state = complete`
  - `missing_components = []`
  - `next_action = use_source_build_lane_or_user_profile_install`
  - process exit code `0`

What this proves:

- source-authoring assessment no longer erases real source bundle evidence just because the old generated consumer install directory is absent.
- the plain repo-local bundle path requires source-repo marker detection so consumer repos can still use `plugins/anarchy-ai` normally.

What this does not yet prove:

- host UI surfacing
- cross-device repeatability

### 13. Repo-local setup uses plain plugin path and repo-scoped marketplace identity

Proven at source/test and local deployable-smoke level on `2026-04-25` after `AA-BUG-022`:

- Path canon now defines:
  - repo-local plugin directory template: `anarchy-ai`
  - repo-local marketplace name template: `anarchy-ai-repo-<repo-slug>`
  - repo-local display-name template: `Anarchy-AI Repo (<RepoName>)`
- Setup tests assert:
  - repo-local `source.path = ./plugins/anarchy-ai`
  - repo marketplace name `anarchy-ai-repo-ai-links`
  - consumer repo install remains a normal install and does not trigger source-authoring mode
  - AI-Links source repo install is blocked before it can overwrite `plugins/anarchy-ai`
- After rebuild, generated `plugins/AnarchyAi.Setup.exe` installed into a throwaway repo and returned:
  - `bootstrap_state = ready`
  - `paths.destination.directories.plugin_root_directory_path` ended with `plugins/anarchy-ai`
  - marketplace root name was `anarchy-ai-repo-<throwaway-slug>`
  - marketplace display name was `Anarchy-AI Repo (<throwaway-name>)`
  - `plugins.<entry>.source.path = ./plugins/anarchy-ai`
  - `source_authoring_bundle_present = false`
  - `missing_components = []`
- After rebuild, generated `plugins/AnarchyAi.Setup.exe /install /repolocal /repo <AI-Links>` returned:
  - process exit code `1`
  - `bootstrap_state = source_authoring_write_blocked`
  - `source_authoring_bundle_present = true`
  - `source_authoring_bundle_state = complete`
  - `missing_components = [source_authoring_repo_consumer_install_blocked]`
  - `next_action = use_source_build_lane_or_user_profile_install`

What this proves:

- repo-local install no longer depends on `local` or path-hash folder naming
- the AI-Links source repo is protected by source-repo markers, not by a weird destination folder name

What this does not yet prove:

- active chat/runtime selection from that repo-local bundle
- cache materialization at the same plugin manifest version
- cross-device repeatability beyond filesystem and UI source visibility

Additional field observation on `2026-04-25`:

- a Workorders repo-local source bundle synced to a second device at `plugins/anarchy-ai`
- that source bundle's plugin manifest reported `version = 0.1.9`
- Codex Plugins UI showed the marketplace dropdown entry `Anarchy-AI Repo (Workorders)`
- the plugin detail page showed the Anarchy-AI MCP server plus the harness, structured-commit, and structured-review skills
- the same machine's Codex plugin cache still showed `anarchy-ai-repo-workorders/anarchy-ai/0.1.8` and user-profile cache `anarchy-ai-user-profile/anarchy-ai/0.1.7`, with no `0.1.9` cache directory
- the plugin detail information row showed `Category = anarchy-ai-repo-workorders, Productivity`, which indicates Codex is exposing marketplace root identity/category separately from plugin manifest `interface.category`
- local Codex config stores plugin enable-state as `[plugins."anarchy-ai@anarchy-ai-repo-workorders"] enabled = true`, which explains `Remove from Codex` as a host-owned state separate from source files and cache directories
- attempting Codex UI `Remove from Codex` for the repo-local Workorders plugin produced a `Failed to uninstall plugin` toast, proving host-side uninstall success is a separate observation from source sync, marketplace visibility, and setup cleanup coverage
- rebuilt setup assess against Workorders reported the same split directly:
  - source plugin manifest version `0.1.9`
  - config plugin key `anarchy-ai@anarchy-ai-repo-workorders`
  - Codex plugin enabled `true`
  - cache entries `[0.1.8]`
  - `source_version_present_in_cache = false`
  - finding `source_plugin_version_not_materialized_in_codex_cache`

Interpretation:

- repo-local source and marketplace visibility can update before Codex materializes the matching chat/runtime cache
- plugin UI visibility is evidence of source/catalog recognition, not proof that the active chat session is using the same version
- setup/status diagnostics must treat source bundle, marketplace source visibility, Codex enable-state, Codex cache, and active runtime as separate surfaces
- failed Codex UI uninstall is repair evidence, not proof that repo-local source or setup payload is invalid; cleanup must handle owned plugin enable-state separately from legacy custom-MCP fallback config

### 14. Plugin manifest version is now release-canon driven and smoke-installed as `0.1.9`

Proven at source/build and local deployable-smoke level on `2026-04-25` after `AA-BUG-024`:

- Branding canon now defines:
  - `metadata.plugin_manifest_version = 0.1.9`
- The branding generator mirrors that value into generated C#, PowerShell, and MSBuild artifacts.
- The setup build helper now writes `plugins/anarchy-ai/.codex-plugin/plugin.json.version` from branding canon instead of the old hard-coded build-script literal.
- After rebuild, generated `plugins/AnarchyAi.Setup.exe` installed into a throwaway repo and the extracted plugin manifest reported:
  - `version = 0.1.9`
  - `bootstrap_state = ready`
  - `install_state.state_valid = true`
  - plugin root ending in `plugins/anarchy-ai`

What this proves:

- the generated deployable carries the new plugin manifest version into installed repo-local bundles
- a cache-sensitive release now has an explicit version signal for Codex to materialize

What this does not yet prove:

- Codex will invalidate an older enabled home-profile cache on every device
- Codex will prefer repo-local skill metadata over home-profile skill metadata when both lanes are enabled
- the work computer will materialize `anarchy-ai-user-profile/anarchy-ai/0.1.9` or `anarchy-ai-repo-<repo-slug>/anarchy-ai/0.1.9` without additional host-side cache refresh behavior
- a Codex UI card showing the 0.1.9 source bundle implies that chat/runtime cache activation has also moved to 0.1.9

### 15. Gov2gov now inventories and seeds schema-carried narrative arc surfaces

Proven at source/test and local deployable-smoke level on `2026-04-25` after `AA-BUG-025`:

- `AGENTS-schema-narrative.json` carries record and register templates.
- The installed plugin bundle now carries concrete templates:
  - `templates/narratives/register.template.json`
  - `templates/narratives/record.template.json`
- Path canon now treats `templates` as a plugin bundle surface.
- `run_gov2gov_migration` now includes `narrative_arc_structure` in `post_migration_inventory`.
- Server tests prove:
  - `plan_only` reports missing narrative register as `planned_to_deliver`
  - `non_destructive_apply` creates missing `.agents/anarchy-ai/narratives/register.json`
  - `non_destructive_apply` creates missing `.agents/anarchy-ai/narratives/projects`
  - the seeded register parses as JSON and contains a `records` array
- Setup tests prove the source plugin bundle carries the narrative templates.
- Rebuilt `plugins/AnarchyAi.Setup.exe` installed into a throwaway repo with:
  - `bootstrap_state = ready`
  - `install_state.state_valid = true`
  - both narrative template files present in the extracted plugin bundle

What this proves:

- the schema-described narrative/arc lane now travels with the installer as concrete template surfaces
- gov2gov no longer ignores the missing narrative register/project-directory materialization surface
- existing narrative artifacts remain workspace-specific and are not hash-compared as canonical payload

What this does not yet prove:

- a Fissure fresh-session run has observed the new `narrative_arc_structure` output from the installed runtime
- existing narrative register preservation has been field-tested against a real non-empty arc register

### 16. Build prerequisites are machine-local, not repo-local install payload

Proven at source/build-helper level on `2026-04-25` after `AA-BUG-026`:

- `build-self-contained-exe.ps1` resolves the .NET SDK from user/machine-local paths such as:
  - `dotnet.exe` on `PATH`
  - `%USERPROFILE%\.dotnet\dotnet.exe`
  - `C:\Program Files\dotnet\dotnet.exe`
- the build helper stages build output under `%LOCALAPPDATA%\Anarchy-AI\AI-Links\setup-build`
- `Directory.Build.props` redirects ordinary repo-local .NET `bin/obj` output to `%LOCALAPPDATA%\Anarchy-AI\AI-Links\dotnet` unless a developer explicitly opts into repo-local artifacts
- removal-safety test fixtures now use `%LOCALAPPDATA%\Anarchy-AI\AI-Links\test-fixtures`
- the build helper now rejects a resolved `.NET SDK` path inside the source workspace
- setup/repo install docs state that .NET SDK/runtime prerequisites, NuGet caches, restore scratch, and package caches must not live inside the source repo or target repo

What this proves:

- repo-local install means Anarchy plugin bundle placement, not .NET SDK placement
- the current source build path has a guard against accidentally using a repo-local SDK path and a default redirect for .NET build/test outputs

What this does not yet prove:

- every developer machine has a correctly installed user/machine-local SDK
- external operators will not manually place SDK/cache folders under target repos outside the build helper

### 17. Underlay and refresh lanes are source-tested; underlay smoke is locally observed

Proven at setup-test/source level on `2026-04-25` after `AA-BUG-029`:

- CLI parsing accepts `/underlay`, `/refresh`, and `/apply`
- deprecated `/refreshschemas` remains accepted but is plan-first unless `/apply` is supplied
- `/underlay` into a throwaway repo seeds:
  - canonical portable schema files
  - `.agents/anarchy-ai/narratives/register.json`
  - `.agents/anarchy-ai/narratives/projects/`
  - AGENTS.md awareness stub only when missing
  - Anarchy-scoped `.gitignore` lines
- `/underlay` does not create:
  - runtime plugin bundle
  - marketplace file
  - MCP declaration
  - host config modification
- `/refresh` reports schema drift without overwriting by default
- `/refresh /apply` overwrites only portable schema files and creates timestamped `.bak` files
- `refresh_plan_ready` is treated as a successful CLI state instead of an automation failure
- duplicate Codex lane logic enables the selected owned Anarchy lane, disables only owned Anarchy non-selected plugin lanes in config text, and preserves unrelated plugin sections
- Workorders live retest after rebuild returned `codex_materialization.codex_plugin_enabled = true` for the selected Workorders lane while the Codex cache remained at `0.1.8`
- Fissure / Docker-Builder-Project live retest after rebuild enabled the selected Fissure lane and disabled the Workorders Anarchy lane as a duplicate
- local `/underlay` smoke through the built setup DLL into `%TEMP%\anarchy-underlay-smoke-*` returned:
  - `setup_operation = underlay`
  - `bootstrap_state = ready`
  - `install_scope = repo_underlay`
  - `runtime_present = false`
  - `marketplace_registered = false`
  - `host_config_modified = false`
  - no `plugins/anarchy-ai` root
  - no `.agents/plugins/marketplace.json`
  - all six portable files seeded
  - narrative register and projects directory seeded
  - AGENTS.md awareness note and Anarchy `.gitignore` block seeded

What this proves:

- the source implementation no longer requires repo-local runtime install for portable repo discipline
- `/underlay` can materialize repo-portable discipline without creating runtime, marketplace, MCP, or host config surfaces in a clean throwaway repo
- the old write-by-default `/refreshschemas` surface is treated as a safety defect and requires `/apply`
- duplicate-lane repair is bounded to Anarchy-owned plugin enable-state
- a selected disabled Anarchy Codex lane is not treated as ready until install/update re-enables it
- runtime install/update can select one Anarchy Codex primary lane while disabling non-selected Anarchy lanes
- Workorders runtime/cache retest on `2026-04-26` proved Codex eventually materialized `0.1.9` into both repo-scoped and user-profile cache lanes after repeated refresh/restart attempts
- Workorders schema-pack refresh then aligned `AGENTS-schema-governance.json` and `AGENTS-schema-narrative.json` with canonical `0.1.9`; remaining gov2gov partiality was not schema divergence
- Workorders gov2gov apply exposed `AA-BUG-036`; the runtime now has posture-aware semantics so `repo_underlay` does not require repo-local marketplace discovery while `repo_local_runtime` still does, and completed GOV2GOV can remain in reference mode without root `GOV2GOV-*` packet files

What this does not yet prove:

- direct windowless EXE smoke remains pending after the UI surfaced during manual smoke attempts; current proof is source/test/build-level
- a long-lived consumer repo has committed only portable underlay truth
- cross-device cache invalidation to `0.1.9` is observed on Workorders after refresh/restart, but the host timing and trigger remain host-owned rather than setup-owned
- rebuilt EXE/plugin redeploy proof for the AA-BUG-036 runtime patch is still pending

### 18. Codex cache invalidation follows plugin manifest version after restart, but version is doing double duty

Proven by user-profile and cache observations on `2026-04-26`:

- after the `0.2.0` user-profile install, Codex initially kept older cache materialization until restart/reindex
- after restart, the user-profile source and Codex cache both exposed `anarchy-ai-user-profile/anarchy-ai/0.2.0`
- the currently installed user-profile source can move ahead again (`0.2.1` observed by runtime provenance) while session skill metadata may still come from the older materialized cache path until the next host refresh boundary

What this proves:

- the version bump path is a real Codex cache invalidation signal once Codex crosses a restart/reindex boundary
- cache materialization is host-owned evidence and must still be inspected separately from the installed source bundle

Design caveat:

- `plugin.json.version` is currently serving two purposes: semantic release identity and Codex cache invalidation key
- this is acceptable for the current release lane, but if semantic version and cache refresh needs diverge, add an explicit `cache_key` or `cache_generation` field rather than making build helpers silently overload version meaning

## Inferred (Not Yet Fully Proven)

### A. Codex materializes an installed-copy cache under `~/.codex/plugins/cache/...`, but active-lane selection remains unresolved

Observed on `2026-04-25` during the Fissure / Docker-Builder-Project proving run:

- setup installed the user-profile bundle into `C:\Users\herri\.codex\plugins\anarchy-ai`
- after Codex restart, an Anarchy cache copy existed at `C:\Users\herri\.codex\plugins\cache\anarchy-ai-user-profile\anarchy-ai\0.1.8`
- a Fissure-session agent reported callable Anarchy tools and an installed plugin root at `C:\Users\herri\.codex\plugins\anarchy-ai`
- the same Fissure-session report said exposed skill metadata still referenced cache version `0.1.7` while installed/cache state on disk was `0.1.8`

Observed again on `2026-04-26` during a second-computer user-profile install:

- pre-install assess on `C:\Users\mherring` found the user-profile source and cache still at `0.1.7`, install-state missing, and the Codex plugin lane disabled
- install materialized user-profile source `0.1.9`, wrote install-state, and enabled `anarchy-ai@anarchy-ai-user-profile`
- post-install cache still contained only `anarchy-ai-user-profile/anarchy-ai/0.1.7`, so `source_plugin_version_not_materialized_in_codex_cache` remained

Observed again on `2026-04-26` during Fissure arc-capture work:

- the chat-supplied skill path pointed at `anarchy-ai-user-profile/anarchy-ai/0.1.7`
- the active user-profile source/cache lane had already materialized `0.1.11`
- the agent fell back to direct repo/MCP inspection, but the stale path proved that versioned cache skill paths can remain in conversation context after the active harness lane changes

What this proves:

- Codex's home-local plugin cache is materially involved after restart.
- Fresh-session host surfacing worked in Fissure after the user-profile install.
- Setup can repair user-profile source/install-state/config on a second profile while Codex cache materialization still lags.
- Versioned Codex cache skill paths are evidence surfaces, not authority for schema, arc, or gov2gov work.

What remains unresolved:

- whether Codex selects runtime, skills, and plugin metadata from the same cache generation in every session
- whether stale skill metadata can persist while runtime/tool calls use a newer cache or installed root
- which host-owned state invalidates or refreshes stale cache metadata
- whether this behavior is stable across devices or only this Windows profile
- whether keeping both repo-local and user-profile Anarchy plugin lanes enabled causes Codex to prefer one cache lane for skill metadata even when the other lane was more recently installed
- whether chat-carried skill paths should be ignored automatically when they do not match the observed active harness lane

Promotion test:

1. capture installed root, cache root, exposed skill metadata path, active runtime path, and successful tool call from the same fresh session
2. repeat after a no-op reinstall and another Codex restart
3. confirm the exposed skill metadata, callable runtime, and installed/cache versions agree
4. after a manifest-version bump, confirm the expected cache version appears; the `2026-04-25` release-canon test version is `0.1.9`
5. repeat on a second device before treating cache selection as portable proof

### B. Codex install-state materialization may still differ from the documented cache/config model

Why this remains inferred:

- OpenAI Codex docs say local plugins install into `~/.codex/plugins/cache/$MARKETPLACE_NAME/$PLUGIN_NAME/local/` and store on/off state in `~/.codex/config.toml`
- the current Anarchy-AI home-local install now resolves in a fresh session and an Anarchy-specific cache directory has been observed, but the observed cache path uses a versioned folder (`0.1.8`) rather than the documented `local` suffix
- the current `~/.codex/config.toml` still shows only the curated plugin enable-state entries, not an Anarchy-specific plugin state entry
- Fissure evidence indicates the home install and cache can disagree at the metadata layer, so config/cache/install-state relationships remain under-specified
- the bundled Codex `plugin-creator` skill describes a generic home-local plugin convention as `~/.agents/plugins/marketplace.json` plus marketplace-relative `./plugins/<plugin-name>`, while Anarchy's tested user-profile lane uses `./.codex/plugins/anarchy-ai`
- Codex has changed rapidly during these tests, so Anarchy's observed-good lane may reflect a valid workaround for earlier host behavior rather than the host's intended canonical method

Promotion test:

1. restart Codex from the current installed state
2. install or enable Anarchy-AI through the Codex Plugins UI if prompted
3. observe whether Codex materializes the documented cache path or config entry
4. compare current `plugin-creator` scaffold output, official Codex documentation, and fresh-session host behavior for the home-local source path convention
5. repeat after a no-op reinstall if needed

### C. Repo-local Codex UI source visibility is observed, but cache/runtime activation remains unresolved

Why this remains inferred:

- OpenAI Codex docs describe `$REPO_ROOT/.agents/plugins/marketplace.json` + `$REPO_ROOT/plugins/` as a peer install scope to home-local
- the Anarchy-AI installer writes that repo-local shape correctly on disk
- a Workorders repo-local install/source sync has now been observed in Codex's Plugins UI on a second device
- that UI observation did not update the Codex plugin cache to the same manifest version (`0.1.9`), so source visibility and cache/runtime activation are not the same proof surface
- local config evidence shows the UI install/remove state is a `[plugins."anarchy-ai@anarchy-ai-repo-workorders"] enabled = true` entry, not merely the marketplace file existing
- after restarting Codex with user-profile, Workorders repo-local, and Fissure repo-local Anarchy marketplaces visible, the Plugins UI listed all three distributions and showed the selected Fissure distribution checked while Workorders and user-profile were not selected
- a second-computer BrainyMigrator repo-local install observed the crossed-profile path problem directly: copied install-state from `C:\Users\herri\...` was invalid under `C:\Users\mherring\...`, and the installer rewrote install-state for the current profile
- that same BrainyMigrator run also exposed `AA-BUG-033`: selecting a repo-local lane with no existing Codex config section disabled duplicate Anarchy lanes but did not create the selected section before the local patch
- a second-computer TheLinks repo-local install confirmed the same `AA-BUG-033` class in the no-duplicate case: setup created the plugin bundle and repo marketplace, selected `anarchy-ai@anarchy-ai-repo-thelinks`, but left `codex_plugin_enabled = null` because the selected Codex config section was absent and no duplicate-lane rewrite occurred
- the documentation describes the layout as a scope option, not explicitly as an auto-discovery rule

Status summary: **repo-local source visibility is observed; active chat/runtime activation from the matching repo-local cache remains unproven.** The installer disclosure text reflects this distinction.

Marketplace hygiene interpretation:

- Codex is making a reasonable host-provenance decision when it treats each marketplace root as a separate Anarchy distribution.
- For Anarchy, that means repo-local runtime installs are valid proving/debug evidence but poor default repo-travel hygiene.
- Normal repo travel should use `/underlay` without marketplace registration; normal runtime should come from `/userprofile`.
- `plugin unavailable, underlay present` is acceptable for a consumer repo because the portable discipline still travels without host-owned runtime state.
- Gov2gov must preserve this split. A missing repo-local marketplace is a startup-discovery gap only when the repo has chosen `repo_local_runtime`, not when it has chosen `repo_underlay`.

Promotion test:

1. install repo-local only on a machine with no prior home-local Anarchy install
2. launch Codex with the repo as working directory in a fresh session
3. observe whether the Anarchy-AI plugin appears in Codex's plugin surface
4. inspect `~/.codex/config.toml` for `[plugins."anarchy-ai@anarchy-ai-repo-<repo-slug>"] enabled = true`
5. inspect `~/.codex/plugins/cache/anarchy-ai-repo-<repo-slug>/anarchy-ai/<version>` and confirm it matches the source manifest version
6. run a harness tool call from the fresh chat and capture the active runtime/version evidence when the tool can report it
7. repeat with a second contributor cloning the repo after the repo-local marketplace is committed

### D. Claude Code MCP registration via user-scope `~/.claude.json`

Status: **implemented, pending fresh-session verification.** The installer lane exists in code (`ClaudeCodeUserScopeLane.Register`) and is gated on the `/claudecode` or `/allhosts` host-target flag. It remains `inferred` under the matrix taxonomy until the fresh-session repeatability check is captured.

Why this remains inferred:

- the installer now writes `mcpServers.<anarchy-ai server name>` directly into `~/.claude.json` via a read-merge-write path (`.bak` on first modification, UTF-8 no-BOM, `File.Replace` atomic swap); this avoids the `claude` CLI PATH dependency described in earlier passes
- baseline captures at `docs/EVIDENCE/claude-baseline/` (gitignored; see `docs/scripts/capture-claude-baseline.ps1`) established that `mcpServers` is absent pre-install on this machine and that `.claude.json` churn is confined to `cachedGrowthBookFeatures` across app restarts
- no post-install capture of the resulting `~/.claude.json` shape has been taken yet on a fresh Claude Code session; Claude Code restart is a documented prerequisite and is not yet exercised against this lane

Promotion test:

1. run `.\plugins\AnarchyAi.Setup.exe /install /userprofile /claudecode /silent /json` on a machine with a captured pre-install `~/.claude.json` baseline
2. diff pre/post `~/.claude.json` and confirm exactly one new `mcpServers.<name>` entry with the expected `command`/`args`, no other user-owned `mcpServers` entries mutated, and a sibling `.claude.json.bak` present
3. restart Claude Code and confirm the MCP server launches and harness tools are listed in the fresh session
4. re-run the installer and confirm the action surfaces as `claude_code_user_scope_registration_noop` (dedup-by-name proves idempotency)

### E. Claude Desktop MCP registration via `claude_desktop_config.json`

Status: **implemented, pending fresh-session verification.** The installer lane exists in code (`ClaudeDesktopLane.Register` plus `ClaudeDesktopInstallDetector`) and is gated on the `/claudedesktop` or `/allhosts` host-target flag. It remains `inferred` under the matrix taxonomy until the fresh-app repeatability check is captured on both MSIX and classic installs.

Why this remains inferred:

- the installer auto-detects MSIX vs classic Claude Desktop by directory existence (`%LOCALAPPDATA%\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\Claude` and `%APPDATA%\Claude`) with MSIX-preferred tie-break, then merges into the active `claude_desktop_config.json` via the same read-merge-write path as lane D
- baseline captures confirm that on this machine both paths are populated with byte-identical content (file redirection from a single MSIX install, not two independent installs) and that `claude_desktop_config.json` is stable across restarts
- no post-install capture has been taken yet on either a fresh MSIX app restart or on a machine with a classic (non-MSIX) install; Claude Desktop has no hot-reload for `mcpServers`, and an open upstream issue can cause older MSIX builds to ignore `mcpServers` entries even when correctly placed -- the installer surfaces this caveat in the disclosure, but its reach on this machine's MSIX build is untested

Promotion test:

1. on an MSIX Claude Desktop machine: run `.\plugins\AnarchyAi.Setup.exe /install /userprofile /claudedesktop /silent /json`, diff pre/post `claude_desktop_config.json` at the resolved active path, confirm the new `mcpServers.<name>` entry coexists with any pre-existing unrelated entry, and confirm a sibling `.bak` exists
2. fully quit Claude Desktop (including the tray process) and relaunch; confirm Anarchy tools appear; if they do not, capture the current MSIX build version and cross-reference the upstream ignore-`mcpServers` issue
3. repeat on a classic (non-MSIX) Claude Desktop machine and confirm the detector selects the `%APPDATA%\Claude` path
4. re-run the installer on each machine and confirm the action surfaces as `claude_desktop_registration_noop`; on a machine with no Claude Desktop install, confirm the action surfaces as `claude_desktop_registration_skipped_no_install_detected`

## Portability Posture

### 1. Application portability

Claim:

- the install and control surface should be portable across compatible host applications

Current status:

- Codex home-local: proven for the documented personal marketplace lane
- Codex repo-local: Codex-documented on disk and UI source visibility observed on Workorders, but matching cache/runtime activation remains unresolved (see inferred item C)
- Claude Code: Pass 2 implemented, pending verification (see inferred item D -- installer writes `~/.claude.json` but fresh-session check is not yet captured)
- Claude Desktop: Pass 2 implemented, pending verification (see inferred item E -- installer writes the detected `claude_desktop_config.json` but fresh-app check on both MSIX and classic is not yet captured)
- Cursor and other hosts: out of current scope

Why:

- current proven evidence in this repo is from Codex home-local only
- Claude Code and Claude Desktop installer lanes now ship as opt-in host targets (`/claudecode`, `/claudedesktop`, `/allhosts`) and are selectable from live GUI checkboxes; they remain inferred until promotion tests D and E are captured on representative machines

### 2. Agent-session portability

Claim:

- an identifiable setup change should survive and surface in a fresh session

Current status:

- partially observed, not fully proven

Why:

- Fissure / Docker-Builder-Project fresh-session report on `2026-04-25` confirmed Anarchy tools were callable after a user-profile install and Codex restart
- setup status for the same run reported `bootstrap_state = "ready"` and `install_state.state_valid = true`
- however, the same report exposed a version mismatch between skill metadata (`0.1.7`) and installed/cache state (`0.1.8`)
- second-computer user-profile install on `2026-04-26` repaired install-state and selected-lane enablement, but did not yet include a fresh-session tool-call proof after Codex cache refresh
- this proves session surfacing occurred, but does not yet prove clean active-lane selection or repeatable cache invalidation behavior

### 3. Different-device portability

Claim:

- the same setup path should produce equivalent mount behavior on another machine

Current status:

- partially observed, not fully proven

Why:

- BrainyMigrator repo-local install output from a second Windows profile (`C:\Users\mherring`) has been captured
- the second-computer assess correctly invalidated copied install-state from the first profile (`C:\Users\herri`) through target/path/root mismatch findings
- the second-computer install rewrote install-state for the current profile and selected the BrainyMigrator repo-local lane
- the same run did not prove fresh Codex runtime availability because the selected Codex config section was missing before `AA-BUG-033` and the Codex cache root was still absent
- user-profile install output from the same second profile has also been captured
- the second-computer user-profile install repaired missing install-state, upgraded source plugin manifest from `0.1.7` to `0.1.9`, and enabled `anarchy-ai@anarchy-ai-user-profile`
- the same user-profile run did not prove cache refresh because Codex cache remained at `0.1.7`

Promotion test for device portability:

1. run setup on a second device with the same install lane
2. verify mount in a fresh session on that device
3. capture equivalent artifacts:
   - marketplace file
   - installed plugin bundle path
   - successful tool call or readiness assessment

## Operational Verification Rules

1. Presence checks should use direct Anarchy tool reachability or setup assess results, not resource or template listing.
2. If tool visibility looks stale, verify current installer and bundle identity first:
   - assess JSON
   - bundle path
   - marketplace file
   - Codex plugin enable-state in `~/.codex/config.toml`
   - Codex cache path and version
   - exposed skill metadata path when available
3. If bundle identity is current but surface visibility still looks stale:
   - treat that as a host indexing or cache investigation
   - do not rewrite core runtime truth based only on stale visibility symptoms
4. Keep config snapshots and install artifacts when changing Codex home registration behavior.
5. Distinguish these three evidence lanes explicitly:
   - repo-authored canonical source
   - published installer payload
   - observed installed destination behavior

## Current Environment Model

Treat the environment as three cooperating planes:

1. canonical authoring plane
   - repo-authored schema family, contracts, docs, disclaimers, and install assertions
2. Codex plugin-marketplace install plane
   - `~/.agents/plugins/marketplace.json`
   - `~/.codex/plugins/anarchy-ai`
   - `~/.codex/config.toml` plugin enable-state under `[plugins."anarchy-ai@<marketplace>"]`
3. runtime and tool plane
   - bundled `AnarchyAi.Mcp.Server.exe` and contract surfaces

Optional fallback or debug plane:

- `~/.codex/config.toml` custom MCP registration when explicitly used

A deployment is considered coherent when the canonical authoring plane, published install surfaces, and observed destination behavior agree on the same lane.

