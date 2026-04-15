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

## Inferred (Not Yet Fully Proven)

### A. Codex may materialize an installed-copy cache under `~/.codex/plugins/cache/...`

Why this remains inferred:

- OpenAI Codex docs describe a Codex-managed cache or installed-copy lane
- no Anarchy-specific cache directory was observed locally after install and before restart
- a fresh post-install Codex restart has not yet been captured in this repo as repeatable evidence

Promotion test:

1. restart Codex after the home install
2. observe whether a cache or installed-copy lane appears
3. capture the resulting path and repeat the observation

### B. Codex install-state materialization may still differ from the documented cache/config model

Why this remains inferred:

- OpenAI Codex docs say local plugins install into `~/.codex/plugins/cache/$MARKETPLACE_NAME/$PLUGIN_NAME/local/` and store on/off state in `~/.codex/config.toml`
- the current Anarchy-AI home-local install now resolves in a fresh session, but no Anarchy-specific cache directory has been observed locally
- the current `~/.codex/config.toml` still shows only the curated plugin enable-state entries, not an Anarchy-specific plugin state entry

Promotion test:

1. restart Codex from the current installed state
2. install or enable Anarchy-AI through the Codex Plugins UI if prompted
3. observe whether Codex materializes the documented cache path or config entry
4. repeat after a no-op reinstall if needed

## Portability Posture

### 1. Application portability

Claim:

- the install and control surface should be portable across compatible host applications

Current status:

- Codex: proven for the documented personal marketplace lane
- non-Codex hosts: inferred

Why:

- current evidence in this repo is from Codex only
- equivalent install or mount evidence is not yet captured for Claude or Cursor

### 2. Agent-session portability

Claim:

- an identifiable setup change should survive and surface in a fresh session

Current status:

- inferred

Why:

- current repo evidence proves the install outputs and on-disk state
- a fresh post-install Codex session verification is still pending capture in this matrix

### 3. Different-device portability

Claim:

- the same setup path should produce equivalent mount behavior on another machine

Current status:

- inferred

Why:

- current evidence is from one Windows profile only
- no second-device run has yet been captured in repo evidence

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
3. runtime and tool plane
   - bundled `AnarchyAi.Mcp.Server.exe` and contract surfaces

Optional fallback or debug plane:

- `~/.codex/config.toml` custom MCP registration when explicitly used

A deployment is considered coherent when the canonical authoring plane, published install surfaces, and observed destination behavior agree on the same lane.

