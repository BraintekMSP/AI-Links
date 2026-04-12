# Anarchy AI MCP Server Scaffold

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
- `./dotnet/SpindleMcp.Server.csproj`

## Codex Custom MCP Fields

For a local STDIO MCP registration in Codex, the expected shape is:

- Name:
  - `anarchy-ai`
- Command to launch:
  - `cmd.exe`
- Arguments:
  - `/c`
  - `..\..\plugins\anarchy-ai\scripts\start-anarchy-ai.cmd`
- Working directory:
  - `.../AI-Links/harness/server`

## Current State

This server is scaffolded, not production-ready.

It currently exists to:

- establish the folder boundary Codex expects
- load the harness contract files
- expose the first two harness tool names
- prove a Windows-first runtime path:
  - preferred `.NET 8` self-contained publish
  - fallback `net48` build if it exists

It does not yet perform the real schema-reality or gov2gov reconciliation logic.

## Tool Surface

The intended first tools are:

- `is_schema_real_or_shadow_copied`
- `run_gov2gov_migration`
