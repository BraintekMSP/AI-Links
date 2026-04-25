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
- Acceptance:
  - Result payload and docs include explicit “canonical surfaces compared” section.

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
  - Plan-only migration repeatedly lists canonical mismatch with no “approved local divergence” lane.
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
  - Add environment proof script/tests for “change in session A, visible in session B.”

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
  - “Ready” can be reported before lane conflicts are fully surfaced.
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
  - `trace_execution_path` (renamed from `explain_failure_path` — the word “failure” activates the wrong concept first per the negation-mitigation research; “trace” and “execution” are affirmative action words)
  - read-only
  - deterministic structured response
- Required output fields (these are required, not optional — a trace without a next action becomes a satisfying stopping point instead of a repair action):
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
  - `recommended_next_action` (REQUIRED — the tool always produces an action, even when that action is report-to-human)
  - `recommended_next_call` (REQUIRED — the tool always names the next callable method)
- Relationship to AA-BUG-013:
  - `diagnose_harness_mount_state` is a narrow diagnostic for mount-layer issues.
  - `trace_execution_path` is a broader tracer for any harness lane, including mount flow.
- Acceptance:
  - Method consistently identifies the first contradiction in known broken scenarios.
  - Output always includes `recommended_next_action` and `recommended_next_call` — a trace that ends at diagnosis without an action is incomplete.
  - Output is concise enough for agent decisioning and detailed enough for human validation.
  - Skill/setup docs reference it as the default “stop guessing” lane before destructive retries.

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
