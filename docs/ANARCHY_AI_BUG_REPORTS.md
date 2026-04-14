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

### AA-BUG-005: Missing setup `self-check` command for active mount diagnostics

- Severity: Medium
- Status: Open
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

## Notes

- These bug reports are about deployment and harness ergonomics, not about constraining repo expression in `AGENTS.md` or companion docs.
- Rich repo-level guidance is expected; integrity checks should remain bounded to canonical schema surfaces.
