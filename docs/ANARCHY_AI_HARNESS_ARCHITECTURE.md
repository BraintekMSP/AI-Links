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

Current operating boundary:

- the installed Codex plugin is an adapter surface, not the source of harness truth
- current plugin incompatibility should be repaired in the plugin lane, not worked around by weakening core harness claims
- schemas do not self-fulfill; they describe route shape, authority, vocabulary, and desired state, but the harness must materialize, verify, reconcile, and record operative state
- careful language is product behavior here: the harness should make the correct path easy to traverse rather than adding rules that the host cannot actually enforce
- when the harness needs consequence, it should create observable state, a callable check, or a next action item instead of only adding stronger prose
- if Anarchy-AI is external ceremony, it fails; if it is policy beside the work, it fails; if it is a tool the agent only uses when reminded, it fails; if it slows the path without improving the actor's immediate odds of success, it gets bypassed
- adoption is an implementation concern: a harness surface is only alive in an environment when people and agents can discover it, own it, and use it under pressure without leaving the work path
- this is continuity with the existing underlay thesis, not a new thesis: make the better-context path easier, clearer, and more recoverable than the shortcut
- zero trust is not the harness goal; tokens, devices, vaults, humans, and AI agents are still trust-bearing surfaces, so Anarchy-AI should complement normal security tools by making risky shortcuts visibly more expensive, not by claiming to remove trust

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
- Codex user-profile install is the current proven packaged lane
- repo-local install remains supported, but its Codex-native plugin surfacing stays unproven until the truth matrix promotion test is captured
- explicit alternate repo targeting remains available through `/repo`

Install lifecycle direction:

- the setup executable is the delivery and operator surface, not the sole source of install truth
- install truth should move toward declared manifests, target adapters, install-state records, doctor/status diagnostics, repair actions, and catalog validation
- host adapters should translate the same declared harness intent into Codex, Claude Code, Claude Desktop, and later Cursor surfaces without changing shared harness logic
- claims about host behavior should be graded through the truth matrix; file presence alone is not proof of plugin materialization or session reachability
- skills and startup instructions are influence surfaces, not enforcement surfaces; they improve consistency and discoverability but do not guarantee behavior
- schema adoption claims should not close on file copy, schema presence, or startup text alone; they close only when a harness or verification lane observes the intended materialized state

Lifecycle state implemented in setup source:

- install/update writes a versioned `.anarchy-ai/install-state.json` record inside the owned plugin bundle
- setup JSON now includes `setup_operation` and `install_state`
- `/status`, `/doctor`, `/selfcheck`, and `/self-check` perform read-only lifecycle inspection
- status mode compares recorded install intent against the currently resolved destination paths and returns bounded `install_state_*` findings
- repair is still a separate future lane; current status output tells the actor which bounded repair is needed without performing it

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

