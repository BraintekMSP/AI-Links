# Anarchy-AI Harness Architecture

## Purpose

This document is the current implementation-level architecture note for the Anarchy-AI harness.

Current architecture:

- schema family = canonical layer
- AGENTS Heuristic Underlay = operative layer
- Anarchy-AI = runtime framework harness

The harness should remain contract-first and host-adapted.

That means:

- shared logic belongs in shared contracts and runtime implementation
- host translation belongs in adapters
- SDK/App Server/skills must not become the source of harness truth

## Shared Core

The shared core owns:

- contract definitions
- result vocabularies
- workspace inspection
- canonical bundle and integrity checks
- preflight rules
- active-work compilation
- schema-reality classification
- non-destructive gov2gov reconciliation
- gap assessment

The shared core does not own:

- Codex-only lifecycle semantics
- Claude-only install mechanics
- App Server protocol bindings
- SDK orchestration policy
- host-native UI prompts
- schema authorship

## Current Core Contracts

The current first-class contracts are:

- `preflight_session`
- `compile_active_work_state`
- `is_schema_real_or_shadow_copied`
- `assess_harness_gap_state`
- `run_gov2gov_migration`

## Actor Surfaces

### User

The user needs:

- one obvious repo-bootstrap lane
- one obvious preflight lane
- one obvious gap-assessment lane
- optional direct harness use without requiring manual tool sequencing

### Agent

The default agent rule is:

- meaningful governed work starts with `preflight_session`

Direct tool use remains valid when the lane is already clear.

### Environment

The environment must be able to answer:

- is the runtime present
- is it callable
- is the repo bootstrapped
- is the schema package materially real
- is the host adapter missing or degraded

That is why `assess_harness_gap_state` exists as a first-class contract.

## Adapter Allocation

### MCP

MCP is the common callable layer.

Use MCP for:

- Codex baseline access
- Claude compatibility
- future Cursor readiness
- stable callable contracts

### App Server

App Server is the Codex-native lifecycle adapter.

Use it for:

- session-start preflight insertion
- richer approvals and status
- Codex-native lifecycle behavior

### SDK

SDK is the programmatic orchestration adapter.

Use it for:

- wrapping Codex runs in harness policy
- orchestrating preflight and reflection workflows
- helper apps and bootstrap flows that need programmatic agent control

The SDK is highly applicable to the harness, but it is not the canonical logic layer.

## Delivery Direction

Current v1 delivery direction:

- repo bootstrap first
- Windows-first runtime bundle
- optional direct user use
- Codex + Claude as first-class compatibility targets

Preferred next delivery direction:

- `AnarchyAi.Setup.exe` becomes the installer/bootstrap surface
- `AnarchyAi.Mcp.Server.exe` remains the long-running runtime
- no-argument launch opens a simple Windows GUI installer
- switch-driven launch remains machine-readable and agent-friendly
- repo-local install remains the default target
- explicit alternate repo targeting remains available through `/repo`

Still-open gaps:

- machine-level install
- managed rollout for RMM/Immybot
- Claude packaging/registration adapter
- Cursor adapter
- reflection workflow (`assess the last exchange and do better`) as a secondary workflow
