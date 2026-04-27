# Anarchy-AI Harness

## Purpose

This directory is the current local execution harness that complements the AGENTS Heuristic Underlay.

The current architecture is:

- schema family = canonical layer
- AGENTS Heuristic Underlay = operative layer built from that family
- Anarchy-AI = runtime framework harness

The underlay remains the portable operative environment built from:

- schemas
- triage
- getting-started
- human-facing explainers

The harness remains the local runtime layer for:

- reality checks
- materialization checks
- non-destructive reconciliation
- future runtime state and validation

The harness should not become the place where schema authorship is invented or replaced.

## Current Tool Surface

The current implemented tools are:

- `preflight_session`
- `compile_active_work_state`
- `is_schema_real_or_shadow_copied`
- `assess_harness_gap_state`
- `run_gov2gov_migration`

The first recurring harness problem was:

- a schema package can be present
- copied into a workspace
- and still fail to be real enough to govern anything

The first question is therefore not:

- can the agent see the schema

It is:

- is the schema real here, or only shadow-copied into place

## Current Contract

The current contracts live at:

- `./contracts/schema-reality.contract.json`
- `./contracts/gov2gov-migration.contract.json`
- `./contracts/narrative-arc-validation.contract.json`
- `./server/README.md`
- `./server/dotnet/AnarchyAi.Mcp.Server.csproj`
- `./server/dotnet/Program.cs`

Together they define:

- the preflight function:
  - `preflight_session`
- the first diagnostic function:
  - `is_schema_real_or_shadow_copied`
- the current-work compiler:
  - `compile_active_work_state`
- the environment gap assessor:
  - `assess_harness_gap_state`
- the narrative Arc conformance validator:
  - `validate_narrative_arc_state`
- the second reconciliation function:
  - `run_gov2gov_migration`
- the status model
- state-linked reasons
- safe next actions
- a minimal migration handoff and result shape
- the launchable MCP server lane Codex can point at
- the separate setup/installer lane now implemented in:
  - `./setup/dotnet/AnarchyAi.Setup.csproj`
  - `../plugins/AnarchyAi.Setup.exe` after local generation through `./setup/scripts/build-self-contained-exe.ps1`

## Intended Follow-On

If the schema is not `real`, the next harness lane is:

- gov2gov-backed reconciliation and migration handling

Six bounded harness tools now exist as contracts plus runtime implementation.

Test-lane addition:

- `direction_assist_test` is now available as an explicit experimental module.
- It is intentionally outside the six core tool model and is not part of default core sequencing.
- Prime-lane promotion should call the same `DirectionAssistRunner` rather than duplicating direction qualification logic in preflight or active-work paths.

The next likely additions are:

- runtime fixture coverage for these contracts
- machine-level install after repo bootstrap is stable
- richer current-work continuity signals when a repo wants durable runtime residue
- a reflection workflow for:
  - assessing the last exchange against schema rules
  - generating a safer correction path without treating reflection as canonical truth

## Boundary

The harness is meant to be:

- local
- contract-host-agnostic in design
- usable through adapters such as MCP or editor integrations

Current packaged delivery remains Windows-first.

Repo bootstrap is the current first installation lane.

It is not meant to be:

- a schema replacement
- a second source of governance truth
- a reason to mix runtime state into the canonical schema artifacts
