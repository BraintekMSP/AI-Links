# Anarchy-AI Harness Architecture

## Purpose

This document is the current implementation-level architecture note for the Anarchy-AI harness.

Current architecture:

- schema family = canonical layer
- AGENTS Heuristic Underlay = operative layer
- Anarchy-AI = runtime framework harness

The harness remains contract-first and host-adapted.

That means:

- shared logic lives in shared contracts and runtime implementation
- host translation lives in adapters
- canonical harness truth stays in the shared contracts — SDK, App Server, and skills consume those contracts, the contracts own harness truth

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

The shared core excludes these (they live in adapters or other layers):

- Codex-only lifecycle semantics — live in the App Server adapter
- Claude-only install mechanics — live in the Claude host adapter
- App Server protocol bindings — live in the App Server
- SDK orchestration policy — lives in the SDK
- host-native UI prompts — live in the host integration
- schema authorship — lives in the schema family

## Current Core Contracts

The current first-class contracts are:

- `preflight_session`
- `compile_active_work_state`
- `is_schema_real_or_shadow_copied`
- `assess_harness_gap_state`
- `run_gov2gov_migration`

Current experimental test-lane contract:

- `direction_assist_test`

Test-lane rule:

- this module remains callable but outside the five-core default order
- prime promotion should reuse the same `DirectionAssistRunner` module from this test lane rather than duplicating qualification logic

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

Current environment evidence discipline:

- proven versus inferred platform behavior is tracked in:
  - `ANARCHY_AI_ENVIRONMENT_TRUTH_MATRIX.md`
- keep inferred host behavior separate from proven architecture facts
- maintain explicit separation between:
  - host-neutral marketplace plane (`.agents`)
  - Codex plugin-marketplace install plane (`~/.agents/plugins/marketplace.json` -> `~/.codex/plugins/anarchy-ai-herringms`)
  - optional Codex custom-MCP fallback/debug plane (`~/.codex/config.toml`)
  - runtime/tool plane (bundled `AnarchyAi.Mcp.Server.exe`)

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

The SDK orchestrates against the harness contracts — canonical logic stays in the contracts themselves.

## Delivery Direction

Current v1 delivery direction:

- repo bootstrap first
- Windows-first runtime bundle
- optional direct user use
- Codex + Claude as first-class compatibility targets

Repo-authored publish rule:

- canonical schema family, contracts, docs, disclaimers, and install assertions stay authored in the repo
- published installer payloads carry those repo-authored surfaces forward
- installed docs and disclosure text should describe destination-relative paths and role labels, not source-checkout-relative layout

Preferred next delivery direction:

- `AnarchyAi.Setup.exe` becomes the installer/bootstrap surface
- `AnarchyAi.Mcp.Server.exe` remains the long-running runtime
- argument-free launch opens a simple Windows GUI installer
- switch-driven launch remains machine-readable and agent-friendly
- repo-local install remains the default target
- explicit alternate repo targeting remains available through `/repo`

Future work reserved for later delivery:

- machine-level install
- managed rollout for RMM/Immybot
- Claude packaging/registration adapter
- Cursor adapter
- reflection workflow (`assess the last exchange and do better`) as a secondary workflow
