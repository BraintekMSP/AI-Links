# Changelog - AI-Links

## 2026-04-13

### Setup lane hardening and shipped-EXE contract checks

- Hardened user-profile lane behavior in `harness/setup/dotnet/Program.cs`:
  - user-profile `install` and `update` now remove stale legacy custom MCP blocks in `~/.codex/config.toml` when they target `~/plugins/anarchy-ai`
  - stale custom-MCP mixed-lane state now blocks `ready` until remediated
  - leftover legacy plugin-root presence is still reported for inventory but no longer blocks readiness by itself once routing is clean
- Added setup publish-time binary contract checks in `harness/setup/scripts/build-self-contained-exe.ps1`:
  - validates `AnarchyAi.Setup.exe /?` output for required CLI/help contract snippets
  - validates both the published EXE and the copied repo handoff EXE (`plugins/AnarchyAi.Setup.exe`)
  - emits explicit result fields:
    - `published_help_contract_validated`
    - `target_help_contract_validated`
- Re-ran regression against `Docker-Builder-Project` using silent CLI:
  - update/install/assess returned `bootstrap_state: ready` and `missing_components: []` for repo-local lane with latest setup binary

### Deployment lessons captured as actionable bug reports

- Added `docs/ANARCHY_AI_BUG_REPORTS.md` with 12 open tickets covering:
  - lane-selection authority and stale config remediation
  - setup EXE freshness and release-contract validation
  - runtime-lock recovery ergonomics
  - divergence semantics clarity and intentional-drift policy
  - cross-session proof and documentation evidence discipline
  - post-install verification as a completion requirement
- Linked the bug register in:
  - `docs/README_ai_links.md`
- Added active tracking pointer in:
  - `docs/TODO_ai_links.md`

### Direction assist test module (prime-ready architecture seam)

- Added a new experimental MCP tool:
  - `direction_assist_test`
- Added mirrored contract surfaces:
  - `harness/contracts/direction-assist-test.contract.json`
  - `plugins/anarchy-ai/contracts/direction-assist-test.contract.json`
- Implemented a modular runtime runner in the server:
  - evaluates `word_count > 30 OR sentence_count > 2`
  - emits fixed choice options:
    - `I need to ask clarification on a few things`
    - `Do your best with what I gave you`
  - returns explicit linguistic findings plus cleaned direction text
  - appends local test telemetry to:
    - `<workspace_root>/.agents/anarchy-ai/direction-assist-test.jsonl`
- Kept the five-core tool model intact:
  - core default sequencing is unchanged
  - `direction_assist_test` is documented as test-lane only
- Updated setup disclosure/help wording to represent:
  - `5 core + 1 test` tool posture
- Kept test-lane missing-surface behavior non-blocking by default in setup assessment.
- Added server test coverage for threshold behavior, fixed options, cleaned output, and register append behavior.
- Added setup regression checks for the updated `5 core + 1 test` wording.
- Recorded promotion rule in companion docs:
  - prime insertion should reuse `DirectionAssistRunner` instead of reimplementing logic inside preflight or active-work flows.

### Codex-native home install truth realignment

- Rebased the Codex user-profile install lane onto the Codex-native plugin path:
  - `~/.codex/plugins/anarchy-ai`
- Stopped treating a custom `[mcp_servers.anarchy-ai]` block in `~/.codex/config.toml` as the primary home-install truth.
- Setup assess/install results now emit:
  - `registration_mode`
- Codex home readiness is now plugin-marketplace-first and requires:
  - bundle present under `~/.codex/plugins/anarchy-ai`
  - personal marketplace entry in `~/.agents/plugins/marketplace.json`
  - `plugins.<entry>.source.path = ./.codex/plugins/anarchy-ai`
  - bundled runtime/plugin identity alignment
- Added legacy-state detection for failed PoC home installs so setup reports bounded manual cleanup guidance instead of silently normalizing drift:
  - legacy `~/plugins/anarchy-ai`
  - stale custom `mcp_servers.anarchy-ai`
- Added repo-authored publish flow for installed README truth:
  - new canonical source doc at `docs/ANARCHY_AI_PLUGIN_README_SOURCE.md`
  - build helper now generates `plugins/anarchy-ai/README.md` from that source with destination-relative paths
- Added setup tests that reuse production path/registration rules instead of a second handwritten truth table:
  - `harness/setup/tests/SetupEngineTests.cs`
- Fixed setup publish drift under redirected temp `obj` lanes by excluding repo-local `obj\\**` items from the setup project.
- Recorded controlled local evidence:
  - `AnarchyAi.Setup.exe /assess /userprofile /silent /json` reports `registration_mode = plugin_marketplace`
  - `AnarchyAi.Setup.exe /install /userprofile /silent /json` materializes the home bundle under `~/.codex/plugins/anarchy-ai`
  - the successful install created `~/.agents/plugins/marketplace.json`
  - the successful install did not require or write `[mcp_servers.anarchy-ai]` into `~/.codex/config.toml`

### Setup installer payload integrity rescan

- Re-ran `harness/setup/scripts/build-self-contained-exe.ps1` and refreshed `plugins/AnarchyAi.Setup.exe`.
- Verified embedded setup payload fidelity by hashing source files against published setup assembly resources:
  - checked pairs: `32`
  - matched pairs: `32`
  - mismatches: `0`
- Corrected stale canonical hash entries in:
  - `plugins/anarchy-ai/schemas/schema-bundle.manifest.json`
  - updated hash values for:
    - `AGENTS-schema-1project.json`
    - `AGENTS-schema-gov2gov-migration.json`
    - `AGENTS-schema-governance.json`
- Updated `harness/setup/scripts/build-self-contained-exe.ps1` so setup builds now sync schema-bundle manifest hashes before publish.
- Hardened setup install behavior under active file locks:
  - install no longer hard-crashes with raw access-denied JSON when existing user-profile bundle surfaces are locked
  - install now reports bounded lock state via:
    - `bootstrap_state = registration_refresh_needed`
    - `missing_components = ["locked_bundle_surface_write_skipped"]`
    - `next_action = release_runtime_lock_and_retry_install`
- Updated installer disclosure/help wording so workspace/schema impact is conditional on whether a workspace target is present.
- Updated installer companion docs so user-profile lane behavior is described accurately:
  - `docs/ANARCHY_AI_REPO_INSTALL_PROCESS.md`
  - `docs/ANARCHY_AI_SETUP_EXE_SPEC.md`
- Clarified that default `/userprofile` with no explicit `/repo` keeps:
  - `workspace_root = ""`
  - `repo_root = ""`
  - portable schema seeding not targeted (`portable_schema_family_not_targeted`)

### Anarchy-AI environment truth matrix and consistency pass

- Added `docs/ANARCHY_AI_ENVIRONMENT_TRUTH_MATRIX.md` to separate:
  - proven environment behavior
  - inferred/not-yet-proven environment behavior
- Captured proven Codex mount facts with explicit evidence lanes:
  - custom MCP UI persistence to `~/.codex/config.toml` under `[mcp_servers.*]`
  - app-server `config/read` and `config/batchWrite` lifecycle around that config lane
  - current Anarchy runtime symbol presence for all five harness tools
  - `resources/templates/list` not being a valid presence check for this tools-only runtime
- Updated companion docs to reference the truth matrix and stop mixing assumptions with verified behavior:
  - `docs/README_ai_links.md`
  - `docs/ANARCHY_AI_SETUP_EXE_SPEC.md`
  - `docs/ANARCHY_AI_REPO_INSTALL_PROCESS.md`
  - `docs/ANARCHY_AI_HARNESS_ARCHITECTURE.md`
- Clarified current user-profile Codex accessibility requirements around the Codex-native plugin marketplace lane instead of treating custom MCP registration as required.
- Added an explicit evidence-qualification rubric for environment claims:
  - `proven` now requires identifiable change + fresh-session effect + repeatability + artifacts
- Added explicit portability claim grading across:
  - application portability
  - agent-session portability
  - different-device portability

## 2026-04-12

### Anarchy-AI repo-local and user-profile install lanes

- Added explicit install-lane selection to `AnarchyAi.Setup.exe`:
  - `repo-local`
  - `user-profile`
- The setup GUI now exposes:
  - install-lane radios for `repo-local` versus `user-profile`
  - placeholder platform radios with only the Windows payload enabled
- The installer and runtime discovery now treat the two lanes differently on purpose:
  - repo-local installs materialize under the selected repo and register in `./.agents/plugins/marketplace.json`
  - user-profile installs materialize under `~/.codex/plugins/anarchy-ai` and register in `~/.agents/plugins/marketplace.json`
- The stable callable MCP server key remains `anarchy-ai` in both lanes.
- The user-profile lane is now part of the primary setup executable behavior instead of being treated as an implied future direction.
- Updated the setup spec, install runbook, repo runbook, and plugin README so they describe both supported lanes honestly.
- Kept the PowerShell bootstrap lane documented as a repo-local compatibility surface rather than pretending it is already widened for user-profile installs.

### Anarchy-AI stable MCP server key

- Changed setup and bootstrap to keep the installed plugin identity repo-scoped while restoring a stable callable MCP server key of `anarchy-ai`.
- This preserves predictable tool syntax like `mcp__anarchy_ai__...` without collapsing plugin install identity back to the old global `anarchy-ai` plugin name.
- The supported split is now:
  - repo-scoped plugin folder and plugin identifier
  - stable `.mcp.json -> mcpServers -> anarchy-ai`
- This replaces the earlier repo-scoped MCP server key direction.

### Anarchy-AI repo-scoped plugin identity

- Changed setup and fallback bootstrap so the installed plugin identity is now repo-scoped instead of reusing the global `anarchy-ai` plugin key across repo-local installs.
- Setup now materializes repo-local installs into:
  - `plugins/anarchy-ai-<repo-slug>-<stable-path-hash>/`
- Setup and bootstrap now align all three installed identity seams to that same repo-scoped key:
  - `plugins.<entry>.name`
  - `plugins.<entry>.source.path`
  - `.codex-plugin/plugin.json -> name`
  - `.mcp.json -> mcpServers -> anarchy-ai-<repo-slug>-<stable-path-hash>`
- This closes the remaining static identity collision that could survive after a Codex-side uninstall even when marketplace root identity and MCP server identity had already been refreshed.
- Setup and bootstrap now flag stale plugin identity as a registration problem instead of pretending the repo is fully ready.
- Updated the install and setup specifications to describe repo-scoped plugin materialization instead of the old fixed `plugins/anarchy-ai/` path.
- Bumped the source plugin manifest version to `0.1.3`.

### Anarchy-AI repo-scoped marketplace identity

- Changed repo-local marketplace registration so the marketplace root `name` is now repo-scoped instead of reusing the old global `ai-links-local` identity across every repo.
- Setup and bootstrap now generate:
  - `name = anarchy-local-<repo-slug>-<stable-path-hash>`
  - `interface.displayName = Anarchy-AI Local (<RepoName>)`
- Setup and bootstrap now also generate a repo-scoped MCP server key:
  - `.mcp.json -> mcpServers -> anarchy-ai-<repo-slug>-<stable-path-hash>`
- This is intended to reduce Codex host-side install and uninstall collisions between different repo-local Anarchy-AI installs.
- Updated the checked-in `AI-Links` marketplace file to the new repo-scoped identity.
- Tightened the fallback bootstrap script so it now:
  - detects outdated marketplace identity
  - returns `registration_refresh_needed` instead of pretending the repo is fully ready
  - suggests `refresh_repo_marketplace_identity` as a bounded repair
- Fixed the fallback PowerShell hashing path to stay compatible with Windows PowerShell 5.1 by using `SHA256.Create().ComputeHash(...)` instead of a newer framework-only helper.
- Reduced the plugin manifest `defaultPrompt` surface to the 3 prompts Codex actually supports and bumped the plugin version through `0.1.2` while tightening the repo-scoped registration surfaces.

### Anarchy-AI setup EXE direction

- Tightened `AnarchyAi.Setup.exe` repo-root auto-detection so it no longer treats generic parent folders as repos merely because they contain `plugins/` or `.agents/`.
- Auto-detection now requires a real repo marker, currently `.git`; otherwise setup requires `/repo`.

- Stopped treating `plugins/AnarchyAi.Setup.exe` as a committed source artifact.
- Added `.gitignore` coverage for `plugins/AnarchyAi.Setup.exe` so the self-contained installer stays a generated local build output.
- Added `harness/setup/scripts/build-self-contained-exe.ps1` as the canonical one-command build helper for regenerating the self-contained setup executable.
- Kept `publish-anarchy-ai-setup.ps1` only as a compatibility wrapper so older references do not break immediately.
- Added `docs/ANARCHY_AI_SETUP_EXE_SPEC.md` to define the preferred next delivery surface:
  - `AnarchyAi.Setup.exe` as installer/bootstrap
  - `AnarchyAi.Mcp.Server.exe` as the long-running MCP runtime
- Preserved the desired GUI/CLI split:
  - no-argument launch opens a simple Windows installer UI
  - switch-driven launch remains silent/JSON-friendly for agents and automation
- Recorded the explicit installer command surface around:
  - `/assess`
  - `/install`
  - `/update`
  - `/silent`
  - `/json`
  - `/repo`
  - `/sourcepath`
  - `/sourceurl`
  - `/refreshschemas`
- Preserved the stronger delivery decision that agent use of the installer should continue to behave like the current bootstrap lane rather than inventing a second install semantics.
- Updated the harness architecture note and runbook hub so the repo now states the intended split between setup executable and runtime executable explicitly.
- Added the same direction to the implementation gap register so script-first bootstrap is treated as an interim lane rather than the preferred long-term user-facing install surface.
- Implemented the first real `AnarchyAi.Setup.exe` project under `harness/setup/dotnet/` and published the single-file Windows executable to `plugins/AnarchyAi.Setup.exe`.
- The setup executable now:
  - embeds the current `plugins/anarchy-ai` bundle
  - materializes the plugin bundle into a target repo
  - creates or updates `.agents/plugins/marketplace.json`
  - supports CLI `/assess`, `/install`, and `/update`
  - supports `/repo`, `/sourcepath`, `/sourceurl`, and `/refreshschemas`
  - provides a minimal no-argument GUI for `Assess` and `Install`
- Added a GUI install disclosure page that appears before interactive install proceeds and summarizes:
  - repo changes
  - product behavior
  - human impact
  - AI impact
- Kept the disclosure concise and mostly generated from current installer facts so it stays aligned with rebuilds instead of drifting into stale hand-written claims.
- Added `harness/setup/scripts/publish-anarchy-ai-setup.ps1` as the one-command publish helper for regenerating `plugins/AnarchyAi.Setup.exe`.
- The helper:
  - resolves a usable SDK path
  - publishes with machine-local temp `obj/bin/publish` lanes
  - refreshes the repo-carried setup executable directly
- Added real CLI help alias support to `AnarchyAi.Setup.exe` for:
  - `/?`
  - `-?`
  - `/h`
  - `-h`
  - `/help`
  - `-help`
  - `--help`
  - `--?`
- The help output is now a generated plain-text summary instead of a parser failure, and it tells the actor:
  - that Anarchy-AI is available in the repo
  - that install provides preflight, gap assessment, and schema reality checks
  - what repo-local changes install will make
- Changed install semantics so setup now seeds missing portable schema-family files into repo root by default.
- Kept `/refreshschemas` as the explicit force-refresh lane when repo-root schema files should be overwritten from the embedded payload.
- Validated locally against disposable temp repos:
  - repo-local install
  - repo-local assess
  - local-source update
  - optional portable schema family materialization
- Added `docs/VISION_REGISTER_MODEL.md` to define the missing structured vision-register direction.
- Recorded two new implementation gaps:
  - vision capture still lacks a structured register with traceable implementation and detractor fields
  - vision qualification, capture, and detractor notes are not yet bounded harness-control surfaces

### Anarchy-AI repo install runbook

- Added `docs/ANARCHY_AI_REPO_INSTALL_PROCESS.md` as the exact current repo-bootstrap install process for bringing Anarchy-AI into another repository.
- The runbook explicitly separates:
  - delivery of files
  - accessibility
  - enforcement
  - realness
- It records the current honest posture:
  - repo bootstrap first
  - Windows-first packaged delivery
  - Codex-first packaged host
  - Claude and Cursor still limited by adapter gaps
- Extended the repo-bootstrap update story so it now documents both:
  - public zip refresh through `-Update`
  - local source fallback through `-UpdateSourcePath` when machine trust/TLS is unreliable
- Tightened the same update story so it now distinguishes:
  - plugin-bundle refresh from optional root schema refresh
  - public-access failures from live-runtime file-lock failures
- Added `plugins/anarchy-ai/scripts/stop-anarchy-ai.ps1` as a bounded runtime-lock command for repo-local Anarchy-AI processes so live-runtime update failures have an explicit repair lane.
- Refined the stop command into explicit operator-facing modes:
  - `AssessRuntimeLock`
  - `SafeReleaseRuntimeLock`
  - `ForceReleaseRuntimeLock`
- Kept the underlying release behavior modular while making UAC-triggering force behavior explicit to the user instead of bundling it into one ambiguous stop action.
- Preserved the stronger rationale for that split:
  - better feedback to the agent about the actual blocking condition
  - a bounded repair lane before broader file/path actions
  - a clearer user-visible choice before UAC-backed force behavior

### Scratchpad #2 negation refinement

- Replaced the narrower "negation as a wrapper over prior context" framing in Scratchpad #2 with a broader rule: negation is better treated as a contextual relationship inflection that may operate across prior, current, or future context spans.
- Preserved the stronger claim that negation is usually lossy and underdetermined because it reshapes relationships without fully restating positive structure.
- Separated that semantic-loss claim from the distinct human-vs-AI problem where humans intuit logical inversion while models resolve negation statistically.

### Vision artifacts for Anarchy-AI harness direction

- Captured the April 12, 2026 harness prompt series into two explicit vision artifacts:
  - `docs/VISION_anarchy_ai_harness_core.md`
  - `docs/VISION_anarchy_ai_delivery_and_access.md`
- Kept both in an `AGENTS-Vision`-style structure:
  - `scope-statement`
  - `what-done-looks-like-at-this-scope`
  - `commitments-that-must-survive-scope-evolution`
- Added `docs/VISION_negation_context_span_verbatim.md` to preserve the user's exact negation wording separately from the cleaned Scratchpad #2 theory language.
- Split them intentionally so the repo now carries separate durable vision for:
  - shared harness core and first-class contract boundaries
  - compatibility, accessibility, delivery, and adoption expectations across User, Agent, and Environment

### Anarchy-AI preflight, gap assessment, and repo bootstrap

- Added two new first-class harness contracts:
  - `preflight_session`
  - `assess_harness_gap_state`
- Implemented both tools in the shared `.NET` MCP runtime by composing existing shared logic instead of creating a second host-specific logic path.
- Kept the core/runtime split explicit:
  - shared logic still lives in contracts and runtime code
  - host surfaces still live in adapters such as MCP, skill packaging, and future App Server / SDK orchestration
- Added `plugins/anarchy-ai/scripts/bootstrap-anarchy-ai.ps1` as the first repo-bootstrap lane so installation has one obvious assess/install command before machine-level rollout exists.
- Reworked harness, plugin, skill, and repo docs so the current public surface now teaches:
  - preflight-first for meaningful governed work
  - explicit install/runtime/schema/adoption gap assessment
  - repo bootstrap as the first install path
  - reflection as a secondary workflow rather than a core contract
- Published the updated Windows-first `net8.0` runtime bundle to `plugins/anarchy-ai/runtime/win-x64/`.
- Verified the packaged `net8.0` lane and recorded that the legacy `net48` target remains source-only/provisional rather than a validated packaged path.
- Added `docs/ANARCHY_AI_HARNESS_ARCHITECTURE.md` to preserve the current actor split:
  - User
  - Agent
  - Environment
  and the current adapter split:
  - MCP
  - App Server
  - SDK

### Anarchy-AI delivery alignment review

- Created `docs/IMPLEMENTATION_GAP_REGISTER.md` as a stable place for semi-functional implementation gaps that do not belong in research scratchpads, samples, or one-pass changelog notes.
- Corrected the rough architecture phrasing toward:
  - schema family = canonical layer
  - AGENTS Heuristic Underlay = operative layer
  - Anarchy-AI = runtime framework harness
- Collapsed the plugin / MCP / skill review into 4 high-level problems with 9 immediate action items covering:
  - trust and user-delivery surfaces
  - deployment-path consistency
  - stale scaffold / tool-count framing
  - the underdescribed user-facing promise
- Expanded the same register to also hold:
  - residual governance-language gaps
  - delivery-surface gaps
  - claim-discipline gaps
- Added two more preserved gaps so they do not get lost in chat:
  - narrative schema still needs its own deeper review pass
  - a journal / accounting capture lane may eventually be warranted as a more rigid cousin of narrative, but should remain a guarded future distinction rather than a new schema right now
- Added a further delivery-gap distinction:
  - `INSTALLED_BY_DEFAULT` appears to solve harness presence better than `AVAILABLE`
  - but default presence still does not make Anarchy-AI feel like a coherent harness if invocation remains manual and fragmented
- Added another packaging-direction note to the implementation-gap register:
  - prefer a self-contained `.exe` as the delivery center of gravity
  - keep a heavy Windows-first bias for now
  - expect some form of trust/signing story such as a self-signed cert after real install testing
- Removed the operational Anarchy-AI delivery checklist from scratchpad `#2` so that scratchpad can remain theory-shaped instead of becoming an implementation residue bin.

### Anarchy-AI delivery and schema cleanup implementation

- Reworked the Anarchy-AI plugin manifest so the user-facing surface now:
  - uses `Anarchy-AI` as the display name
  - identifies the repo owner instead of placeholder trust metadata
  - links to real repository/plugin notice pages instead of placeholder URLs
  - describes the three-tool runtime promise instead of only schema-reality plumbing
  - states the current packaged delivery honestly as Windows-first
- Added plugin-local `PRIVACY.md` and `TERMS.md` so the manifest can point to real trust surfaces instead of placeholders.
- Updated plugin, harness, MCP-server, repo README, and runbook docs so they now share one cleaner architecture model:
  - schema family = canonical layer
  - AGENTS Heuristic Underlay = operative layer
  - Anarchy-AI = runtime framework harness
- Replaced stale maturity wording such as:
  - `scaffold`
  - `first two harness tools`
  where the current local state already has three implemented bounded tools.
- Updated the MCP server docs so the direct bundled runtime is the primary packaged launch path and `start-anarchy-ai.cmd` is treated as a development helper / fallback path instead of the default install story.
- Tightened the Anarchy-AI skill so it now distinguishes:
  - `schema_reality_state`
  - `integrity_state`
  - `possession_state`
  instead of compressing them into one result axis.
- Softened the overstrong scratchpad `#2` working claim from `can reliably improve` to `may improve` so the theory note stays closer to its current evidence standard.
- Cleaned the most concentrated residual exit-language in the schema family:
  - `invalid -- do not proceed` -> `invalid -- resolve before continuing` where that exact required-field pattern existed
  - governance measurement/migration prose now routes through reporting, direction, and retention language rather than `block on discrepancy`, `sign-off`, `approval`, or `do not discard`
- Synced the changed canonical schema files into `plugins/anarchy-ai/schemas/` and regenerated the schema-bundle manifest so the bundled canonical surface still matches the repo canon.

### Anarchy AI active work compilation

- Added `compile_active_work_state` as the third implemented harness tool.
- The new tool compiles current work into a bounded operational packet covering:
  - active objective
  - active lane
  - current status
  - next required action
  - ordered remaining steps
  - blockers
  - stop point
  - evidence status
  - session degradation signals
- Kept the implementation intentionally bounded:
  - no hidden persistence
  - no planner theatrics
  - no fake long-term memory surface
- Updated the plugin README, skill, and plugin manifest prompts so the delivery surface now teaches all three implemented harness tools.

### Anarchy AI canonical bundle and possession detection

- Bundled the canonical schema family into `plugins/anarchy-ai/schemas/` and added a hash manifest so the plugin now carries:
  - runtime
  - contracts
  - skill
  - canonical schema files
- Extended `is_schema_real_or_shadow_copied` so it now returns:
  - `integrity_state`
  - `possession_state`
  - `integrity_findings`
- Added canonical hash comparison so a workspace can be materially `real` while still being flagged as `possessed` when canonical schema files diverge from the trusted bundle.
- Implemented the first real `run_gov2gov_migration` lane:
  - `plan_only` proposes non-destructive reconciliation
  - `non_destructive_apply` can copy missing canonical schema files from the bundled schema set into the target workspace
  - canonical divergence is surfaced as audit pressure instead of being silently overwritten
- Updated the plugin skill and plugin README so the public delivery surface now matches the implemented bundle and possession behavior.

## 2026-04-11

### Anarchy AI delivery-path correction

- Corrected the `Anarchy AI` plugin delivery path so the plugin MCP declaration now launches the bundled self-contained Windows executable directly instead of chaining through `cmd.exe`.
- Changed the repo-local marketplace policy for `anarchy-ai` from `AVAILABLE` to `INSTALLED_BY_DEFAULT` so repo-local delivery tests exercise default installation behavior instead of optional listing behavior.
- Updated plugin and runbook docs to distinguish:
  - direct bundled-exe delivery as the intended product path
  - `start-anarchy-ai.cmd` as a development helper only

### Anarchy AI schema-reality implementation

- Implemented real workspace inspection behind `is_schema_real_or_shadow_copied` in `harness/server/dotnet/Program.cs`.
- The tool now classifies AGENTS-family workspaces into the bounded contract states:
  - `real`
  - `partial`
  - `copied_only`
  - `fully_missing`
- The result now includes:
  - bounded active reasons
  - recommended next action
  - safe repair suggestions
  - inspection details covering package files, governed files, startup surfaces, and startup discovery alignment
- Validated the implemented tool against:
  - `AI-Links` -> `copied_only`
  - `Docker-Builder-Project` -> `real`
- `run_gov2gov_migration` remains scaffold-only.

## 2026-04-10

### Scratchpad #2 governance-language note

- Added a new scratchpad `#2` note distinguishing adversarial human governance from interpretation-first governance.
- Preserved the observation that traditional governance often behaves like a reactive punishment surface masquerading as enforcement, and rarely seems concerned with helping the individual succeed.
- Added the counter-framing for the underlay:
  - assume little to no trust and build trust anyway
  - treat misunderstanding as a design condition
  - let faulty information and weak processes become enriched and valuable information
- Added a second scratchpad `#2` note placing taxonomy as a backstage build-time layer that shapes the delivery but should not dominate the canonical operating artifacts.
- Added the frontier/horse/harness metaphor as an explicitly separate paper-language formulation, preserving the stronger line that the execution harness keeps rider and horse from training each other into worse habits and the closing phrase `safe, useful anarchy`.
- Added the first harness scaffold under `harness/`:
  - `harness/README.md`
  - `harness/contracts/schema-reality.contract.json`
- Added the second harness contract:
  - `harness/contracts/gov2gov-migration.contract.json`
- Added the first launchable harness server scaffold:
  - `harness/server/README.md`
  - `harness/server/dotnet/AnarchyAi.Mcp.Server.csproj`
  - `harness/server/dotnet/Program.cs`
- Defined the first harness function as `is_schema_real_or_shadow_copied`, with a bounded status model:
  - `real`
  - `partial`
  - `copied_only`
  - `fully_missing`
- Defined the second harness function as `run_gov2gov_migration`, with bounded inputs from the first function:
  - `partial`
  - `copied_only`
- Bound the second contract to non-destructive reconciliation and a re-evaluated resulting schema reality state.
- Kept the first server pass intentionally honest:
  - the MCP entrypoint loads the contracts and exposes the tool names
  - the real evaluation and reconciliation logic are not implemented yet
- The active runtime strategy is now:
  - preferred `.NET 8` self-contained single-file publish
  - fallback `net48` build when it exists
- Updated the plugin launcher to prefer the published `.NET 8` executable, then a `net48` executable, and only fail when neither exists and `dotnet` is unavailable.
- Bound the result contract to state-linked reasons and a single safe routing action:
  - `none`
  - `run_gov2gov_migration`
- Added the first repo-local delivery plugin scaffold:
  - `plugins/anarchy-ai/.codex-plugin/plugin.json`
  - `plugins/anarchy-ai/.mcp.json`
  - `plugins/anarchy-ai/scripts/start-anarchy-ai.cmd`
  - `plugins/anarchy-ai/skills/anarchy-ai-harness/SKILL.md`
  - `.agents/plugins/marketplace.json`
- The plugin now acts as the first extension-style delivery lane:
  - MCP declaration
  - launcher script
  - thin skill surface
  - marketplace discovery
- Renamed the visible plugin surface to `Anarchy AI` before the first commit so the forward-facing product name is settled early.
- The launcher script is responsible for:
  - preferring the published `.NET 8` executable
  - falling back to the `net48` executable when it exists
  - publishing `.NET 8` locally only when no executable is present and the SDK is available
- Updated the repo runbook so the harness scaffold is discoverable without mixing runtime concerns into the schema family itself.

Validation:

- Documentation/contract scaffold update only.
- No runtime behavior changed.

### Sibling vocabulary alignment continuation

- Continued the post-`efe9ae0` vocabulary sweep across sibling and human-facing surfaces so the schema family no longer partially teaches `blocked-until` while governance teaches `report-to-human`.
- Updated `AGENTS-schema-gov2gov-migration.json` to rename `blocked-until-values` to `report-to-human-values` and align the `human-confirms` definition with the governance wording.
- Updated `AGENTS-schema-narrative.json` to replace the remaining `blocked-until` gate language in compress mode and cold-start authoring with `report-to-human`.
- Updated `AGENTS-schema-triage.md` and `Getting-Started-For-Humans.txt` so the human-facing routing/onramp docs now teach `report-to-human` and `first-time-governance-authoring` instead of older `blocked-until` / `greenfield` phrasing.
- Removed remaining explicit `required: false` declarations from the schema family where optionality is already implied by absence of `required`.
- Added explicit triage posture that `1project` should give way to governance for generalized solutions and is better treated as a bounded-project lane than a product/default lane.
- Corrected the `incomplete-session` audit rationale so it now centers pseudo-state and exit-grammar risk rather than leaning on governance measurement parity.
- Kept this pass intentionally narrow:
  - no broader negation rewrite
  - no change to the verbatim `CANNOT` context-ordering rule
  - no revisit of `invalid -- do not proceed`

Validation:

- Vocabulary alignment pass only.
- No runtime behavior changed.

### Scratchpad #2 sample extraction

- Extracted the full observed sample corpus out of `docs/SCRATCHPAD_prophecy_precontext_influence.md` into the dated companion file `docs/SAMPLES_prophecy_precontext_influence_2026-04-10.md`.
- Replaced the large in-file sample block in scratchpad `#2` with a short pointer section so the scratchpad now stays focused on:
  - core question
  - conceptual distinctions
  - current notes
  - working claims
- Updated `docs/README_ai_links.md` so the dated sample corpus is directly discoverable from the runbook.

Validation:

- Documentation organization update only.
- No runtime behavior changed.

### Scratchpad #2 continuation notes

- Added a clarification that the startup chain was not optimized for tiny context so much as low-entropy routing:
  - thin dispatcher
  - clear authority-file naming
  - trusted voluntary ingest into the larger context body
- Added a sharper diagnosis for proof-lane recovery language:
  - a recovery and healing system for rare failures can become an exit system if the failures are common enough
- Recorded the likely next audit target:
  - continuation-enforcing language
  - recovery-only language
  - mixed language that accidentally legitimizes early exit
- Added a token-efficiency note clarifying that the schema appears to save cloud / serialized spend by converting repeated reasoning work into stable, cacheable, parallelizable starter structure rather than by shrinking context.
- Added a research bridge note preserving the strongest carry-forward from scratchpad `#1`:
  - Dziri et al. supports planning modules and fear of composition depth
  - Leviathan et al. supports repeated exposure in the parallelizable input domain
  - the project's own collisions include low-entropy startup routing, deliberate specific naming, repeated structural exposure, and skepticism toward context compression
- Preserved the important boundary condition:
  - those research-backed wins appear strongest in startup, routing, naming, and authority recognition
  - later samples still expose a narrower weakness around active proof continuity and anti-exit behavior
- Added a new hypothesis-shaped note that some of the strongest project outcomes may involve a second steering mechanism beyond the underlay itself:
  - one technician engaging the agent
  - another technician later supplying outside perspective, authority signaling, pressure, or reframing
- Recorded the likely candidate mechanisms without overclaiming them:
  - social-authority effect
  - context-refresh / re-anchoring effect
  - progress-pressure effect
  - phrasing diversity effect

Validation:

- Documentation/research framing update only.
- No runtime behavior changed.

## 2026-04-09

### Prophecy scratchpad

- Added `docs/SCRATCHPAD_prophecy_precontext_influence.md` as a second research scratchpad exploring the framework as a pre-context influence system rather than an accuracy system.
- The scratchpad centers the question:
  - can durable pre-read structure positively influence later session outcomes with reliability before it has seen later session-specific context
- The note frames the system in terms of:
  - trajectory shaping
  - pre-context control
  - outcome steering
  - prophecy as an engineering question rather than a mystical one
- Extended the note to clarify:
  - why `underlay` is the correct name for a closed-system steering layer
  - why the design intentionally prefers contract-style influence over overclaimed enforcement
  - why some intentionally unenforceable controls may still matter if they steer context before the session path hardens
- Recorded an additional product signal:
  - agents tend to minify or "slice" meaningful work into semantically unsafe local patches by default
  - schema re-pointing can often restore the obvious broader sequence without adding new task-specific instruction
- Recorded an observed sample where a broad, under-specified request still propagated through multiple repo surfaces, proof lanes, and local guardrail mechanisms without requiring schema edits.
- Recorded a second observed sample where schema reingest plus recent-history review surfaced implementation-discipline failures and then drove broad corrective work with explicit proof-lane qualification.
- Recorded a third observed sample where the agent correctly identified the unfinished governance layer, propagated module scope on demand, then shifted from anti-structure hesitation into module objective-pack refinement after schema-aligned user correction.
- Recorded a fourth observed sample where the agent initially tried to mutate the governance schema to fit an implementation omission, the user stopped that move, and the correct recovery was to implement the schema's existing provision rather than redesign the contract.
- Recorded a fifth observed sample where a stray artifact started pulling the session toward a plausible but non-canonical missing-concept theory, and canonical schema reingest restored the distinction between real provisions, invented artifacts, and hypothetical future extensions.
- Recorded a sixth observed sample where rapid backlog/objective prioritization was later re-grounded against commits and pending worktree state, showing the underlay helping separate planned work from actually shipped or still-risky work.
- Recorded a seventh observed sample where a long chat was converted into schema-backed interaction/program method coverage, stale generated artifacts were treated as a real completeness failure, and a user vocabulary correction (`slice`) was absorbed as a trajectory-steering constraint rather than a cosmetic wording preference.
- Recorded an eighth observed sample where schema reingest plus recent-history review classified commit churn into recurring failure families such as ownership drift, policy discovered in UI, late consolidation, and optimistic closure, showing the underlay functioning as a retrospective diagnostic lens.
- Recorded a ninth observed sample where a broad consolidation review became a sustained multi-turn consistency program across wrappers, docs, verifier harnesses, archive boundaries, and UI shell ownership, with user correction against needless fragmentation absorbed into larger coherent checkpoints.
- Recorded a tenth observed sample where schema reingest, subagent-trust questioning, richer proof/measurement correction, and a governed path-forward plan converged into gate-based closure with explicit operator-in-the-loop proof, showing the underlay resisting premature closure across multiple truth layers.
- Recorded an eleventh observed sample where a full-chat traceability pass was promoted into schema-backed interaction/program methods, linked back to product requests, remeasured through governance, and explicitly prevented from canonizing transient conversational noise as permanent contract.
- Recorded a twelfth observed sample where recent commits in a neighboring repo were used to expose local durability gaps, Git history was bootstrapped for the migrations project, and user correction against partial "highest-value" follow-up pushed the agent into a broader schema-informed hardening pass that still disclosed unproven residuals honestly.
- Recorded a thirteenth observed sample where an authenticated proof lane was dropped after the first meaningful finding, the user forced that continuity failure into schema-backed stop-point rules and guarded repo truth, and the recovery path evolved from a failure list into a real execution-supporting plan with critical path, complementary work, and validation gates.
- Recorded a fourteenth observed sample where a user-supplied workflow identity was acknowledged but not materialized into same-pass executable proof quickly enough, forcing a new schema-backed rule that workflow-test config needed for the active proof lane must become runnable immediately rather than remaining chat-adjacent.
- Recorded a fifteenth observed sample where recurring workflow requests were correctly promoted into schema-backed methods and guardrails, but the actual ignored local secret was still missing until the user forced it, exposing the gap between "represented through governance" and "materialized for execution."
- Recorded a sixteenth observed sample where live use of a concrete test operator rapidly narrowed the auth blocker to a gateway transport/pathing problem and was documented into repo truth, but still left the user asking where the explicit action items were, exposing the gap between narrowed findings and preserved momentum.
- Recorded a seventeenth observed sample where runtime logs exposed host-contract drift and launcher/status inconsistency around `wo.braintek.local` versus `localhost`, the user forced the obvious broader cleanup instead of another staged micro-step, and the resulting normalization aligned most runtime surfaces while still honestly preserving a smaller localhost-biased readiness-message residual.
- Recorded an eighteenth observed sample where authenticated-workflow defaults that should have been settled were still being re-asked in chat, the user forced those decisions into first-copy governance and helper defaults, and the later attempt with a concrete operator identity produced a more disciplined diagnosis that separated identity truth from host, TLS, and local proof-lane noise.
- Added current notes clarifying that the most troubling recurring failures now look like exit-condition failures more than context failures, and that prompt/schema steering may be near ceiling for this class without stronger external state, gating, validation, and restartability around active proof lanes.
- Added current notes refining the earlier "language versus non-language" framing into a more accurate distinction between structured linguistic artifacts and runtime-coupled state, clarifying that the underlay is already doing sophisticated language-shaped control and that the likely complement is less-purely-model-mediated state rather than some impossible external escape from the closed system.
- Added current notes defining `model narration`, clarifying that active proof files are lane-scoped runtime-adjacent state rather than one-file-per-prompt ceremony, and capturing a simple emerging mental schema of `1. prose guidance`, `2. structured linguistic artifacts`, `3. runtime-coupled state`, `4. harder gates`, with the current system read roughly as `2.2 / 4`.
- Added current notes tightening the naming around `AI heuristic underlay` and capturing the line: `Agent behavior is heuristic and must be shaped before its local heuristics take over.`
- Added comparison clarifications from scratchpad `#1`, including:
  - a softer and more accurate reading of `pretokenization`
  - explicit acknowledgment that later harness thinking came from later testing pressure
  - correction that deliberate label engineering was already heavily present
  - refinement that `context compression` is the first-class enemy, not compression in general
  - note that ambiguity/grammar steers likely become operational only with harness support
  - note that independent authorship of `#1` and `#2` is a feature for resisting confirmation bias
- Updated `docs/README_ai_links.md` so both scratchpads are explicitly discoverable from the runbook.

Validation:

- Documentation/research framing update only.
- No runtime behavior changed.

### Assumption-failure penetration review

- Added `docs/ASSUMPTION_FAILURE_PEN_TEST.md` as a penetration-test-style review of the framework's assumption surfaces rather than a runtime app assessment.
- The review focuses on places where control strength depends on undeployed enforcement, underconfigured measurement, human approval quality, or consuming-system discipline.
- Findings now distinguish:
  - holes that should be patched now
  - holes that should be patched before broader downstream adoption
  - holes that can be risk-accepted only in bounded environments
- Updated `docs/README_ai_links.md` so the new review is discoverable from the repo runbook.

Validation:

- Documentation/framework update only.
- No runtime behavior changed.

## 2026-04-07

### AGENTS schema family hosting

- Added the portable schema JSON family at the repo root:
  - `AGENTS-schema-governance.json`
  - `AGENTS-schema-1project.json`
  - `AGENTS-schema-narrative.json`
- Moved the human-facing schema companions and comparison guide into `AI-Links` so the reusable framework repo now hosts the public-safe schema explanation set:
  - `README-AGENTS-schema-governance.md`
  - `README-AGENTS-schema-1project.md`
  - `README-AGENTS-schema-narrative.md`
  - `AGENTS-schema-comparison-matrix.md`
- Updated `README.md` and `docs/README_ai_links.md` so the schema family is part of the normal repo navigation instead of living only in `TheLinks`.

Validation:

- Cross-repo documentation move/copy only.
- No runtime behavior changed.

## 2026-04-02

### Control-plane agent prompting

- Added `docs/CONTROL_PLANE_AGENT_PROMPT_MODEL.md` to codify the pattern where a control-plane repo should generate richer worker-agent prompts for narrower module/repo agents.
- Added `templates/MODULE_AGENT_PROMPT_TEMPLATE.md` so downstream repos can produce structured module-agent prompt packets with:
  - objective
  - source-of-truth
  - startup set
  - impact surface
  - acceptance contract
  - prohibited shortcuts
- Updated `AGENTS.md` and `docs/README_ai_links.md` so:
  - control-plane prompting is now explicit top-level guidance
  - serious control-plane work uses `25k` minimum starter context and `50k` target context
  - bounded maintenance work is still allowed to use a smaller context pack

Validation:

- Documentation/framework update only.
- No runtime behavior changed.

### Progress-over-patching model

- Added `docs/PROGRESS_OVER_PATCHING_MODEL.md` as a reusable guardrail for:
  - impact-first planning instead of narrow symptom patching
  - widening local-first tables when translation friction reveals missing structure
  - keeping source-of-truth, fallback/degraded behavior, and naming/discoverability explicit
- Updated `AGENTS.md`, `docs/CROSS_REPO_CONTRACT_MODEL.md`, the cross-repo templates, and `docs/README_ai_links.md` so the new rule is part of the startup path and reusable framework spine.
- Cross-repo standard now explicitly requires external impact-surface review across owner repos, producer/consumer repos, orchestration/runtime repos, and edge/intake tools.

Validation:

- Documentation/framework update only.
- No runtime behavior changed.

## 2026-03-31

### Startup-context budget model

- Added `docs/STARTUP_CONTEXT_BUDGET_MODEL.md`:
  - reusable scoring model for predicting startup-token budgets
  - explicit complication-density calculation
  - documentation-adequacy checks
  - worked examples for `Workorders`, `TheLinks`, and `AI-Links`
- Added `templates/STARTUP_CONTEXT_BUDGET_WORKSHEET.md` for repeatable repo scoring.
- Updated `docs/README_ai_links.md` so the startup-context budget model and worksheet are part of the main reusable navigation set.

Validation:

- Documentation/framework update only.
- No runtime behavior changed.

## 2026-03-30

### Startup-context refactor guide and prompt-role clarification

- Added `docs/STARTUP_CONTEXT_REFACTOR_GUIDE.md` as a reusable reference for:
  - expanding mandatory startup context before cleanup
  - splitting `AGENTS`, runbook README, project prompt, TODO, and changelog roles cleanly
  - preserving historical docs before any archival/move pass
- Updated `AGENTS.md` and `docs/README_ai_links.md` so startup-doc cleanup and prompt/runbook refactor work has an explicit navigation target instead of being rediscovered through chat.
- Rebuilt `templates/PROMPT_PROJECT_TEMPLATE.md` around the intended prompt role:
  - implemented baseline
  - active direction
  - discretionary biases for new work
  - when TODO and CHANGELOG actually need to be loaded
  - explicit separation from hard rules in `AGENTS.md`

Validation:

- Documentation review only.
- No runtime application behavior exists in this repository.

### Guardrail clarification

- Removed the weaker "read only what's needed" bias from the baseline ingest language:
  - the startup spine is now an entry order, not a license to ignore the rest of the repo
  - `AI-Links` is treated as intentional operating context rather than a subjective pile of optional docs
  - full-repo ingest now stays explicit and should not be narrowed by agent preference
- Tightened the repo-ingest contract so "ingest the entire repo" now means exactly that:
  - agents should not self-select a subset of "important" docs and call that full ingest
  - ingest acknowledgments should distinguish startup-spine ingest, task-relevant subset ingest, and true full-repo ingest
  - agents should state any exclusions explicitly instead of implying full coverage
- Tightened the ingest acknowledgment shape:
  - after repo ingest, the reply should still echo the working directory or repo path
  - the reply should still end with one short confirmation question that this is the right working location
- Tightened `AGENTS.md` and `docs/AI_COLLAB_STARTUP_PROMPT.md` so the default behavior contract now says more explicitly:
  - `gitignored`, `untracked`, and `workspace-safe` are different concepts
  - synced workspaces should not receive non-critical runtime/cache state without an explicit decision
  - machine-local `AppData` / temp lanes should be preferred for disposable output
  - `clean clone + declared bootstrap` is the target, not warm-cache success
  - destructive cleanup should inventory first, quarantine before delete, and revalidate the exact target path in-scope immediately before execution
  - command sequencing is not itself a safety gate for destructive follow-up actions
- Tightened the startup-intake guidance so repo ingestion does not immediately jump to developer-style repo classification:
  - the default intake now clarifies user outcome, audience, and "done" in plain language first
  - repo-shape and implementation-taxonomy questions are now explicitly secondary
- Replaced the weaker "infer it from how the user talks" pattern with an explicit one-question communication/autonomy gauge:
  - agents should ask one short gauge question when technical-depth assumptions are unclear
  - the answer is treated as a communication/autonomy contract, not a competence judgment
  - the working level can be updated later as the conversation reveals stronger context or specific knowledge gaps
- Updated `templates/PROMPT_PROJECT_TEMPLATE.md` so new-project intake captures `User outcome`, `Communication / autonomy gauge`, `Audience / operator`, and `Done when` up front.
- Added an explicit wrong-workspace guard to the startup model:
  - after repo ingest, the agent should echo the current working directory/repo path
  - then ask one short confirmation question that this is the correct working location
  - this is intentionally a single confirmation step, not a long intake chain
- Updated `docs/README_ai_links.md` purpose wording to call out repo-truth vs workspace-sprawl boundaries.

Validation:

- Documentation review only.
- No runtime application behavior exists in this repository.

## 2026-03-28

### Initial scaffold

- Added root repo guardrails in `AGENTS.md`.
- Added runbook and navigation hub in `docs/README_ai_links.md`.
- Added a generic AI startup prompt in `docs/AI_COLLAB_STARTUP_PROMPT.md`.
- Added a reusable repo `2.5` readiness model in `docs/REPO_2_5_READINESS_MODEL.md`.
- Added a generic cross-repo contract model in `docs/CROSS_REPO_CONTRACT_MODEL.md`.
- Added a subagent safety model in `docs/SUBAGENT_SAFETY_MODEL.md`.
- Added a documentation cleanup method in `docs/DOCUMENTATION_CLEANUP_METHOD.md`.
- Added a publication checklist in `docs/PUBLICATION_CHECKLIST.md`.
- Added reusable templates for `AGENTS`, project prompt, project README, and cross-repo handoffs.

Validation:

- Scaffold and documentation review only.
- No runtime application behavior exists in this repository yet.

### Publication stance

- Added a custom `LICENSE` with an all-rights-reserved, permission-required model.
- Added a root `NOTICE` to make the public-reference-only stance explicit.
- Updated `README.md` and `docs/PUBLICATION_CHECKLIST.md` to reflect the custom publication model.
- Added a new Scratchpad #2 note that treats negation as a wrapper over prior context, preserving the deeper claim that negation can fail before token-level mitigation even begins when the rejected context is already compressed, ambiguous, or misidentified.




- Added a Scratchpad #2 note preserving the bounded-incarnation/shared-substrate framing as a mental model for weak persistence across sessions without overclaiming individuality, hive-mind continuity, or literal consciousness.
