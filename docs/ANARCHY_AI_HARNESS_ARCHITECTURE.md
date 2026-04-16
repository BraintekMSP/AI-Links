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
- canonical harness truth stays in the shared contracts â€” SDK, App Server, and skills consume those contracts, the contracts own harness truth

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

- Codex-only lifecycle semantics â€” live in the App Server adapter
- Claude-only install mechanics â€” live in the Claude host adapter
- App Server protocol bindings â€” live in the App Server
- SDK orchestration policy â€” lives in the SDK
- host-native UI prompts â€” live in the host integration
- schema authorship â€” lives in the schema family

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

- complex changes start with `preflight_session`

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
  - home-local Codex install plane (`~/.agents/plugins/marketplace.json` -> `~/.codex/plugins/anarchy-ai`) -- proven
  - repo-local Codex install plane (`<repo>/.agents/plugins/marketplace.json` -> `<repo>/plugins/anarchy-ai-local-...`) -- Codex-documented, runtime is per-machine
  - home-local Claude Code install plane (direct read-merge-write into user-scope `~/.claude.json`, no `claude` CLI shellout) -- Pass 2 implemented, pending verification (see truth matrix item D)
  - home-local Claude Desktop install plane (auto-detect MSIX vs classic install, read-merge-write into the active `claude_desktop_config.json`) -- Pass 2 implemented, pending verification (see truth matrix item E)
  - legacy Codex custom-MCP cleanup plane (`~/.codex/config.toml` stale `[mcp_servers.*]` removal) -- retained for migration only
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

The SDK orchestrates against the harness contracts â€” canonical logic stays in the contracts themselves.

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

Pass 2 implemented (pending promotion through the truth matrix):

- host-target selection surface: `[Flags] HostTargets { Codex, ClaudeCode, ClaudeDesktop }` threaded through `SetupOptions`, the CLI (`/codex`, `/claudecode`, `/claudedesktop`, `/allhosts`; default Codex when none supplied), and live GUI checkboxes (replacing the Pass-1 greyed-out placeholders)
- Claude Code lane: `ClaudeCodeUserScopeLane.Register` performs a direct read-merge-write against `~/.claude.json` rather than shelling out to the `claude` CLI; the shellout path was abandoned because the `claude` binary is not reliably on PATH for the installer process (it is commonly the Desktop app's embedded copy) and baseline evidence showed a clean stdio-compatible write surface
- Claude Desktop lane: `ClaudeDesktopInstallDetector` disambiguates MSIX vs classic vs absent by directory existence on `%LOCALAPPDATA%\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\Claude` and `%APPDATA%\Claude`, with MSIX-preferred tie-break when both populated (single MSIX install with file redirection, not two independent installs); `ClaudeDesktopLane.Register` then merges into the resolved active config
- shared write discipline for both Claude lanes: tolerant parse (`JsonCommentHandling.Skip` + `AllowTrailingCommas`), dedup-by-name (noop when the existing `command` already matches), `.bak` on first modification, UTF-8 no-BOM, atomic `File.Replace` swap, and per-action result codes (`..._noop` / `_refreshed` / `_added` / `_skipped_no_install_detected`)
- disclosure + help text call out the restart requirement, the MSIX-ignore-`mcpServers` upstream caveat, and that both Claude lanes remain unverified on this machine

Future work reserved for later delivery:

- machine-level install
- managed rollout for RMM/Immybot
- Claude Code marketplace-plugin parity lane (`~/.claude/plugins/known_marketplaces.json` + `extraKnownMarketplaces`/`enabledPlugins`)
- Claude Desktop `.mcpb` bundle (silent-install path not documented)
- Cursor adapter
- reflection workflow (`assess the last exchange and do better`) as a secondary workflow

