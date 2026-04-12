# Anarchy AI Plugin

## Purpose

This plugin is the local delivery surface for the Anarchy AI harness.

It exists so users do not need to manually wire:

- harness paths
- MCP launch commands
- schema-reality tool discovery

## What It Connects

- plugin layer:
  - this directory
- harness contracts:
  - `../../../harness/contracts/`
- harness MCP server:
  - `../../../harness/server/`

## Current Delivery Story

The plugin provides:

- a local MCP declaration through `.mcp.json`
- a launcher script that prefers:
  - `.NET 8` self-contained single-file output
  - `net48` build output when it exists
- a skill that tells the agent when to use the first two harness tools

The plugin does not yet add a custom UI panel or settings page.
