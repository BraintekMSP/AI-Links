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
  - `published_runtime_executable = "C:\\Users\\herri\\AppData\\Local\\Temp\\ai-links-setup-build\\server-publish\\AnarchyAi.Mcp.Server.exe"`
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

## Inferred (Not Yet Fully Proven)

### A. Codex materializes an installed-copy cache under `~/.codex/plugins/cache/...`, but active-lane selection remains unresolved

Observed on `2026-04-25` during the Fissure / Docker-Builder-Project proving run:

- setup installed the user-profile bundle into `C:\Users\herri\.codex\plugins\anarchy-ai`
- after Codex restart, an Anarchy cache copy existed at `C:\Users\herri\.codex\plugins\cache\anarchy-ai-user-profile\anarchy-ai\0.1.8`
- a Fissure-session agent reported callable Anarchy tools and an installed plugin root at `C:\Users\herri\.codex\plugins\anarchy-ai`
- the same Fissure-session report said exposed skill metadata still referenced cache version `0.1.7` while installed/cache state on disk was `0.1.8`

What this proves:

- Codex's home-local plugin cache is materially involved after restart.
- Fresh-session host surfacing worked in Fissure after the user-profile install.

What remains unresolved:

- whether Codex selects runtime, skills, and plugin metadata from the same cache generation in every session
- whether stale skill metadata can persist while runtime/tool calls use a newer cache or installed root
- which host-owned state invalidates or refreshes stale cache metadata
- whether this behavior is stable across devices or only this Windows profile

Promotion test:

1. capture installed root, cache root, exposed skill metadata path, active runtime path, and successful tool call from the same fresh session
2. repeat after a no-op reinstall and another Codex restart
3. confirm the exposed skill metadata, callable runtime, and installed/cache versions agree
4. repeat on a second device before treating cache selection as portable proof

### B. Codex install-state materialization may still differ from the documented cache/config model

Why this remains inferred:

- OpenAI Codex docs say local plugins install into `~/.codex/plugins/cache/$MARKETPLACE_NAME/$PLUGIN_NAME/local/` and store on/off state in `~/.codex/config.toml`
- the current Anarchy-AI home-local install now resolves in a fresh session and an Anarchy-specific cache directory has been observed, but the observed cache path uses a versioned folder (`0.1.8`) rather than the documented `local` suffix
- the current `~/.codex/config.toml` still shows only the curated plugin enable-state entries, not an Anarchy-specific plugin state entry
- Fissure evidence indicates the home install and cache can disagree at the metadata layer, so config/cache/install-state relationships remain under-specified

Promotion test:

1. restart Codex from the current installed state
2. install or enable Anarchy-AI through the Codex Plugins UI if prompted
3. observe whether Codex materializes the documented cache path or config entry
4. repeat after a no-op reinstall if needed

### C. Repo-local Codex lane has not been observed producing a callable plugin

Why this remains inferred:

- OpenAI Codex docs describe `$REPO_ROOT/.agents/plugins/marketplace.json` + `$REPO_ROOT/plugins/` as a peer install scope to home-local
- the Anarchy-AI installer writes that repo-local shape correctly on disk
- the repo-local lane has NOT been observed surfacing the Anarchy-AI plugin in Codex's plugin UI on the installing machine; the only observed working surface for repo-local is the direct MCP server (runtime callable as an MCP endpoint), not a Codex-native plugin entry
- the last observation of any repo-local working behavior was roughly one week prior to this entry, on a single local-host deploy; nothing more recent and nothing cross-machine has been captured
- the documentation describes the layout as a scope option, not explicitly as an auto-discovery rule

Status summary: **repo-local is currently treated as unproven.** The installer disclosure text reflects this.

Promotion test:

1. install repo-local only on a machine with no prior home-local Anarchy install
2. launch Codex with the repo as working directory in a fresh session
3. observe whether the Anarchy-AI plugin appears in Codex's plugin surface (plugins list, plugin-UI enablement, or equivalent Codex-native plugin recognition), not just whether an MCP endpoint is callable
4. if only the MCP endpoint is callable, record that as a partial result and continue treating the plugin lane as unproven
5. repeat with a second contributor cloning the repo after the repo-local marketplace is committed

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
- Codex repo-local: Codex-documented on disk, plugin surface unobserved (see inferred item C -- currently treated as unproven)
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
- this proves session surfacing occurred, but does not yet prove clean active-lane selection or repeatable cache invalidation behavior

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
3. runtime and tool plane
   - bundled `AnarchyAi.Mcp.Server.exe` and contract surfaces

Optional fallback or debug plane:

- `~/.codex/config.toml` custom MCP registration when explicitly used

A deployment is considered coherent when the canonical authoring plane, published install surfaces, and observed destination behavior agree on the same lane.

