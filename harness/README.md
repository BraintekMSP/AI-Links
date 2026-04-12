# Anarchy AI Harness Scaffold

## Purpose

This directory is the starting scaffold for the local execution harness that complements the AGENTS Heuristic Underlay.

The underlay remains the portable truth layer:

- schemas
- triage
- getting-started
- human-facing explainers

The harness remains the local runtime layer:

- reality checks
- materialization checks
- non-destructive reconciliation
- future runtime state and validation

The harness should not become the place where schema truth is authored.

## First Function

The first harness function is:

- `is_schema_real_or_shadow_copied`

This exists to answer a recurring failure mode:

- a schema package can be present
- copied into a workspace
- and still fail to be real enough to govern anything

The first question is therefore not:

- can the agent see the schema

It is:

- is the schema real here, or only shadow-copied into place

## Current Contract

The first scaffolded contracts live at:

- `./contracts/schema-reality.contract.json`
- `./contracts/gov2gov-migration.contract.json`
- `./server/README.md`
- `./server/dotnet/SpindleMcp.Server.csproj`
- `./server/dotnet/Program.cs`

Together they define:

- the first diagnostic function:
  - `is_schema_real_or_shadow_copied`
- the second reconciliation function:
  - `run_gov2gov_migration`
- the status model
- state-linked reasons
- safe next actions
- a minimal migration handoff and result shape
- the launchable MCP server lane Codex can point at

## Intended Follow-On

If the schema is not `real`, the next harness lane is:

- gov2gov-backed reconciliation and migration handling

The first two harness functions now exist as contract scaffolds.

The next likely additions are:

- runtime fixture coverage for these contracts
- a real local MCP implementation behind the Windows-first .NET server lane
- a later bootstrap/install lane for `fully_missing` packages

## Boundary

The harness is meant to be:

- local
- host-agnostic
- usable through adapters such as MCP or editor integrations

It is not meant to be:

- a schema replacement
- a second source of governance truth
- a reason to mix runtime state into the canonical schema artifacts
