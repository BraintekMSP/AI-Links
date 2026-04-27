# Anarchy-AI Plugin

> This README is generated from `docs/ANARCHY_AI_PLUGIN_README_SOURCE.md` by `harness/setup/scripts/build-self-contained-exe.ps1`.
> Keep install-story prose authored here so the published plugin bundle stays destination-relative and honest.

## Purpose

This plugin is the user-facing delivery surface for Anarchy-AI, the runtime harness for the AGENTS Heuristic Underlay.

Anarchy-AI helps teams stop paying the same context tax every session. It turns incomplete repo context into valuable working context for complex changes, often with lower token consumption than repeatedly rebuilding the same setup in chat. It also provides a non-destructive migration lane for current AGENTS files, moving them toward a directionally stronger structure aligned with the Google Research findings captured in this repo.

The current architecture is:

- schema family = canonical layer
- AGENTS Heuristic Underlay = operative layer built from that family
- Anarchy-AI = runtime framework harness

## Repo-Authored Published Truth

All carried schema-family artifacts, contracts, docs, disclaimers, and install assertions remain authored in this repo.

The standalone installer is a published carrier:

- it publishes the repo-authored plugin bundle into the setup payload
- it resolves install paths relative to the destination at install time
- it does not become an alternate source of harness truth

The published plugin bundle therefore keeps destination-relative paths such as:

- repo-local installed plugin root: `{{REPO_LOCAL_PLUGIN_ROOT}}`
- user-profile installed plugin root: `{{USER_PROFILE_PLUGIN_ROOT}}`
- personal marketplace path: `{{USER_PROFILE_MARKETPLACE_PATH}}`
- personal marketplace `source.path`: `{{USER_PROFILE_MARKETPLACE_SOURCE_PATH}}`

This README should never teach source-repo-relative install paths or up-level source checkout hops.

## What It Connects

- plugin layer:
  - this directory
- bundled contracts:
  - `./contracts/`
- bundled runtime:
  - `./runtime/win-x64/`
- bundled canonical schemas:
  - `./schemas/`
- bundled narrative templates:
  - `./templates/narratives/`
- bundled underlay awareness template:
  - `./templates/AGENTS.md.awareness-note.template`
- bundled skills:
  - `./skills/anarchy-ai-harness/`
  - `./skills/chat-history-capture/`
  - `./skills/structured-commit/`
  - `./skills/structured-review/`

## Current Delivery Scope

The packaged plugin delivery is Windows-first today.

That means:

- the bundled runtime is `./runtime/win-x64/AnarchyAi.Mcp.Server.exe`
- `.mcp.json` launches that bundled executable directly
- `start-anarchy-ai.cmd` is a development helper and fallback path, not the primary install story

The broader harness contract may later travel farther than the current packaged runtime, but the present bundle should be described honestly as Windows-first.

## Current Delivery Story

The plugin provides:

- a preferred single-file installer at `{{SETUP_EXE_PATH}}` after that installer is generated locally by the build helper
- a local MCP declaration through `.mcp.json`
- direct launch of the bundled `.NET 8` self-contained single-file runtime inside the plugin
- a bundled canonical schema family plus hash manifest under `./schemas/`
- narrative register and record templates under `./templates/narratives/` because `AGENTS-schema-narrative.json` carries the arc/register lane
- a skill that teaches when to use the six bounded core runtime tools and how to discover the experimental `direction_assist_test` module
- a runtime-free `/underlay` setup lane that seeds portable schema and narrative discipline into a repo without installing the runtime plugin or touching host config
- a plan-first `/refresh` setup lane that aligns only portable schema files and requires `/apply` before overwriting
- a repo-bootstrap script at `./scripts/bootstrap-anarchy-ai.ps1` as a compatibility/fallback lane for repo-local install, assess, and bundle refresh after the bundle already exists
- a runtime lock script at `./scripts/stop-anarchy-ai.ps1` for assessing, safely releasing, or forcibly releasing the bundled Anarchy-AI runtime lock
- a safe retirement script at `./scripts/remove-anarchy-ai.ps1` for inventorying, quarantining, or fully removing repo-local, user-profile, and documented plugin-cache surfaces without treating repo-authored source truth as disposable

The repo-local launcher script is retained only as a development helper during source work. It is not the intended packaged delivery path.

The plugin does not yet add a custom UI panel or settings page.

The setup executable help and disclosure surfaces are intentionally generated from current installer facts so an agent or user can see:

- what destination-relative surfaces install will create
- which lane is being chosen:
  - `repo-underlay`
  - `repo-local`
  - `user-profile`
- what install will and will not change

Repo-local runtime install is a proving/debug carrier. Commit the portable underlay surfaces when intended; do not commit installed runtime bundles, marketplace pointers, PDB/EXE/runtime files, or local test JSONL residue into consumer repos.

For Codex, the primary user-profile lane is the plugin marketplace lane:

- plugin bundle under `{{USER_PROFILE_PLUGIN_ROOT}}`
- personal marketplace at `{{USER_PROFILE_MARKETPLACE_PATH}}`
- personal marketplace `source.path` of `{{USER_PROFILE_MARKETPLACE_SOURCE_PATH}}`

The current optional custom `mcp_servers.anarchy-ai` block is no longer the primary Codex home-install truth.
Older legacy `mcp_servers.anarchy-ai-herringms` entries are cleanup evidence only.

## Current Tool State

- `preflight_session` is implemented and returns:
  - bounded readiness for complex changes
  - recommended next path
  - adoption state and active gaps
- `compile_active_work_state` is implemented and returns a bounded operational packet for:
  - current objective
  - active lane
  - blockers
  - stop point
  - evidence and degradation signals
- `is_schema_real_or_shadow_copied` is implemented and returns:
  - bounded schema reality classification
  - canonical schema bundle integrity
  - derived `possession_state` for canonically diverged but operative workspaces
  - resolved Anarchy workspace posture so repo-underlay and repo-local-runtime startup expectations are not conflated
- `assess_harness_gap_state` is implemented and returns:
  - installation state
  - runtime state
  - schema state
  - underlay readiness so schema/template availability is not mistaken for repo utilization
  - narrative utilization facts such as register presence, projects directory presence, and record count
  - narrative Arc conformance status from read-only validation
  - artifact hygiene state for repo-local generated build/cache/runtime/scratch directories
  - adoption state
  - missing components and safe repairs
  - nested `paths.origin|source|destination` evidence instead of flat path fields
- `validate_narrative_arc_state` is implemented and returns:
  - register, project-record, decision, entry, and observed-pattern conformance findings
  - file path and JSON path evidence for each finding
  - structural grounding and advisory next actions
  - no file writes and no blocking behavior
- `run_gov2gov_migration` is implemented for:
  - planning non-destructive gov2gov reconciliation
  - copying missing canonical schema bundle files into the workspace in `non_destructive_apply`
  - auditing canonical divergence instead of silently overwriting it
  - preserving completed GOV2GOV migrations in reference mode while materializing missing `GOV2GOV-*` companion files only when active artifact mode is requested or observed

Together, these tools give Anarchy-AI its current runtime promise:

- preflight complex changes before the agent proceeds
- compile incomplete or drifting work context into bounded operational state
- evaluate whether the schema package is materially real here
- assess install, runtime, schema, and adoption gaps explicitly
- validate narrative Arc/register conformance before declaring Arc edits complete
- non-destructively migrate current AGENTS files toward a directionally stronger structure without replacing local authorship

Experimental test-lane addition:

- `direction_assist_test` qualifies long direction text using bounded linguistic findings, returns cleaned direction plus fixed two-choice output, and appends local test telemetry.
- it is explicitly test-lane and does not change default core tool order.

The plugin bundle currently carries:

- contracts
- runtime
- canonical schemas
- schema bundle manifest
- skills, including `chat-history-capture` as the richer execution/checking lane for archival decision capture

The plugin bundle can refresh canonical schema-family files from its own carried schema bundle.
The bootstrap script can refresh the plugin bundle from a public zip source or from a local source path when machine trust or TLS is unreliable.

## Runtime Lock Repair

The bootstrap refresh path does not replace a live bundled runtime in place.
Stop the running `AnarchyAi.Mcp.Server.exe` process before retrying an update that touches `runtime/win-x64/AnarchyAi.Mcp.Server.exe`.

The dedicated runtime-lock commands are:

- assess runtime lock:
  - `powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\stop-anarchy-ai.ps1 -Mode AssessRuntimeLock`
- safe release runtime lock:
  - `powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\stop-anarchy-ai.ps1 -Mode SafeReleaseRuntimeLock`
- force release runtime lock:
  - `powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\stop-anarchy-ai.ps1 -Mode ForceReleaseRuntimeLock`

`<installed-plugin-root>` is either:

- `{{REPO_LOCAL_PLUGIN_ROOT}}`
- `{{USER_PROFILE_PLUGIN_ROOT}}`

`SafeReleaseRuntimeLock` does not request UAC elevation.

`ForceReleaseRuntimeLock` tries once normally, then retries once through a UAC elevation prompt if the release fails with access denied.

The safe/force split is intentional for both humans and agents:

- it gives the actor a bounded repair option before resorting to force behavior
- it makes the cause legible when a live runtime lock is blocking update
- it gives the agent stronger direction about the problem before it reaches for broader file or path manipulation

## Safe Retirement

The bundled retirement script is the preferred bounded lane when Anarchy-AI needs to be removed or reset without guessing at paths.

Human-friendly Windows quick cleanup:

- double-click `scripts/Remove Anarchy-AI.cmd`
- it performs safe quarantine-first cleanup across every reachable repo-local, user-profile, and documented plugin-cache surface for the current user
- it keeps the console open and explains what was preserved
- it rewrites shared `~/.codex/config.toml` only to remove Anarchy-owned Codex plugin enable-state
- it leaves legacy custom-MCP config untouched unless the advanced opt-in flag is used

The dedicated retirement commands are:

- assess removable surfaces:
  - `powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\remove-anarchy-ai.ps1 -Mode Assess`
- quarantine repo-local, user-profile, and documented plugin-cache surfaces:
  - `powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\remove-anarchy-ai.ps1 -Mode Quarantine -Targets repo_local,user_profile,device_app`
- quarantine and then permanently delete the quarantined copies:
  - `powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\remove-anarchy-ai.ps1 -Mode Remove -Targets repo_local,user_profile,device_app`
- advanced legacy custom-MCP cleanup:
  - `powershell -ExecutionPolicy Bypass -File <installed-plugin-root>\scripts\remove-anarchy-ai.ps1 -Mode Quarantine -Targets user_profile -IncludeLegacyCustomMcpConfig`

Recommended defaults:

- omit `-RepoRoot` to auto-detect the current repo context when the helper is being run from a repo-local source or installed bundle
- omit `-UserProfileRoot` to auto-detect the current shell/user home directory
- omit `-Targets` to use the recommended current-context scope set:
  - `repo_local` when a repo context is detected
  - `user_profile,device_app` when only a home-local context is detected
- omit `-QuarantineRoot` to use a temp-directory quarantine lane outside the workspace

Use the `.ps1` lane when an agent or power user needs exact control over mode, targets, or automation. Use `Remove Anarchy-AI.cmd` when a human simply wants the plugin gone responsibly.

The retirement script:

- inventories first and reports every target before destructive work
- preserves repo-authored source truth in the source repo instead of treating `plugins/anarchy-ai` as disposable
- backs up mutable files before editing or retiring them
- removes Anarchy-only marketplace files after backup instead of leaving empty branded marketplace shells behind
- detects both current and legacy installed plugin roots before retirement work
- removes Anarchy-owned Codex plugin enable-state from `~/.codex/config.toml` while preserving unrelated Codex plugin, window, and project trust sections
- leaves legacy custom-MCP blocks in `~/.codex/config.toml` untouched unless `-IncludeLegacyCustomMcpConfig` is explicitly requested
- quarantines before delete so rollback remains possible unless `-Mode Remove` is explicitly chosen
- clears owned optional custom-MCP fallback blocks such as `mcp_servers.anarchy-ai` and older legacy `mcp_servers.anarchy-ai-herringms` entries when present
- retires documented plugin-cache roots when they exist, but does not guess at broader Codex app databases or private host state

Marketplace files are treated as shared registries:

- the live `marketplace.json` stays in place
- the script removes only Anarchy-AI entries from the live `plugins` array
- non-Anarchy plugin entries are preserved unchanged
- if Anarchy-AI was the only plugin, the file is backed up and retired instead of leaving an empty branded marketplace shell

## Current Boundaries

- The plugin bundle still does not invent repo-authored governed files such as `AGENTS-hello.md`, `AGENTS-Terms.md`, `AGENTS-Vision.md`, or `AGENTS-Rules.md`.
- The preferred current first install lane is `{{SETUP_EXE_PATH}}`.
- In the `AI-Links` source repo, that setup executable is a generated artifact, not a tracked file.
- The current overall install posture supports:
  - repo-local install
  - user-profile install
- It is still not machine-wide or device-local.

