# Anarchy-AI MCP Server

## Purpose

This directory is the launchable MCP lane for the harness.

The contract files remain in:

- `../contracts/`

The server remains separate so the harness keeps a clean boundary between:

- contract shape
- runtime implementation
- future test fixtures

## Current Entry Points

- `./dotnet/Program.cs`
- `./dotnet/AnarchyAi.Mcp.Server.csproj`

## Packaged Codex MCP Fields

For the packaged local delivery surface, the expected MCP shape is:

- Name:
  - `anarchy-ai-herringms`
- Command to launch:
  - `.\runtime\win-x64\AnarchyAi.Mcp.Server.exe`
- Arguments:
  - none
- Working directory:
  - the installed plugin root (`.` inside the bundled `.mcp.json` contract)

`start-anarchy-ai.cmd` is now a repo-local development helper and fallback path. It should not be taught as the primary packaged launch path.

## Current State

This server is active and intentionally bounded.

It currently exists to:

- establish the folder boundary Codex expects
- load the harness contract files
- expose the current five harness tools
- prove a Windows-first runtime path:
  - preferred `.NET 8` self-contained publish
  - legacy `net48` target kept in source, but not part of packaged validation

It now performs:

- session preflight for meaningful governed work
- real schema-reality classification
- canonical schema bundle integrity and possession detection
- bounded current-work compilation
- environment gap assessment
- bounded gov2gov planning and missing-canonical-file refresh

It still does not perform:

- repo-authored governed-file invention
- full automatic workspace materialization
- broad runtime/session persistence

## Tool Surface

The intended first tools are:

- `preflight_session`
- `is_schema_real_or_shadow_copied`
- `compile_active_work_state`
- `assess_harness_gap_state`
- `run_gov2gov_migration`

Experimental test-lane tool:

- `direction_assist_test`

The five core tools remain the default runtime contract surface.
`direction_assist_test` is explicitly test-lane and should only be promoted into prime orchestration by reusing the same runner module.
