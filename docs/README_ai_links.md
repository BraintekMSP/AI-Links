# README - AI-Links (AGENTS Heuristic Underlay)

## Purpose

This is the runbook and navigation hub for `AI-Links`.

AI-Links houses the **AGENTS Heuristic Underlay** â€” a portable schema family that engineers what AI agents read before they act â€” alongside a broader framework for AI-assisted software delivery.

The schema family is the core. The framework docs are supporting material for teams adopting it.

Current architecture:

- schema family = canonical layer
- AGENTS Heuristic Underlay = operative layer built from that family
- Anarchy-AI = runtime framework harness that compiles active work, evaluates schema reality, and reconciles local drift without replacing the schema family

## Documentation startup spine

- `../AGENTS.md`
- `./README_ai_links.md`
- `./TODO_ai_links.md`
- `./CHANGELOG_ai_links.md`

Default behavior:

- start with the startup spine
- then continue through the rest of the repo in a deliberate order
- do not reduce `AI-Links` to a self-selected subset of "important" docs when the user asked for a repo ingest

## The Schema Family (AGENTS Heuristic Underlay)

### Portable deployment set (travels to any workspace):

- `../AGENTS-schema-governance.json` â€” multi-scope governance for code and operations
- `../AGENTS-schema-1project.json` â€” single-goal session continuity
- `../AGENTS-schema-narrative.json` â€” evolving narrative records for tribal knowledge capture
- `../AGENTS-schema-gov2gov-migration.json` â€” governance-to-governance migration contracts
- `../AGENTS-schema-triage.md` â€” routing document that picks the right schema
- `../Getting-Started-For-Humans.txt` â€” human onramp

### Evaluation and reference (stays in AI-Links):

- `../README-AGENTS-schema-governance.md`
- `../README-AGENTS-schema-1project.md`
- `../README-AGENTS-schema-narrative.md`
- `../README-AGENTS-schema-gov2gov-migration.md`
- `../AGENTS-schema-comparison-matrix.md`

Treat the `.json` files as canonical and the markdown companions as human-facing explainers.

## Research scratchpads

- `./SCRATCHPAD_prompt_efficacy_patterns.md` â€” prompt efficacy, research foundation (Dziri 2023, Leviathan 2025), product identity, measurable claims
- `./SCRATCHPAD_prophecy_precontext_influence.md` â€” pre-context influence theory, model-narration concept, layered maturity model, continuation boundary analysis
- `./SAMPLES_prophecy_precontext_influence_2026-04-10.md` â€” 10 observed deployment samples with analysis
- `./ASSUMPTION_FAILURE_PEN_TEST.md` â€” penetration-test-style review of the framework's control model

## Framework docs

- `./AI_COLLAB_STARTUP_PROMPT.md`
- `./ANARCHY_AI_HARNESS_ARCHITECTURE.md`
- `./VISION_REGISTER_MODEL.md`
- `./VISION_anarchy_ai_harness_core.md`
- `./VISION_anarchy_ai_delivery_and_access.md`
- `./VISION_negation_context_span_verbatim.md`
- `./STARTUP_CONTEXT_REFACTOR_GUIDE.md`
- `./STARTUP_CONTEXT_BUDGET_MODEL.md`
- `./CONTROL_PLANE_AGENT_PROMPT_MODEL.md`
- `./PROGRESS_OVER_PATCHING_MODEL.md`
- `./REPO_2_5_READINESS_MODEL.md`
- `./CROSS_REPO_CONTRACT_MODEL.md`
- `./SUBAGENT_SAFETY_MODEL.md`
- `./DOCUMENTATION_CLEANUP_METHOD.md`
- `./PUBLICATION_CHECKLIST.md`

## Anarchy-AI runtime harness

- `./ANARCHY_AI_HARNESS_ARCHITECTURE.md` - implementation-level harness architecture, actor split, and adapter allocation
- `./ANARCHY_AI_SETUP_EXE_SPEC.md` - preferred Windows-first installer contract for separating `Setup.exe` delivery/bootstrap from the MCP runtime
- `./ANARCHY_AI_REPO_INSTALL_PROCESS.md` - exact repo-bootstrap install process for delivering Anarchy-AI into another repo
- `./ANARCHY_AI_ENVIRONMENT_TRUTH_MATRIX.md` - proven vs inferred environment behavior for Codex config, MCP mounting, marketplace lanes, and tool-surface verification
- `./ANARCHY_AI_BUG_REPORTS.md` - active bug tickets for setup, mounting, divergence semantics, and proof-lane reliability
- `./ANARCHY_AI_DOC_DISCOVERY_BUG_REGISTER.md` - bugs and hidden assumptions exposed specifically by the code documentation pass
- `./ANARCHY_AI_PLUGIN_README_SOURCE.md` - repo-authored source for the installed plugin README; the build helper publishes this into `plugins/anarchy-ai/README.md` with destination-relative install paths
- `./scripts/test-documentation-truth-compliance.ps1` - documentation-truth audit that fails when active specs and READMEs drift from the current Anarchy-AI identity and install model
- `../harness/README.md` - local runtime harness boundary and capability notes
- `../harness/setup/dotnet/AnarchyAi.Setup.csproj` - Windows-first setup/installer project that embeds the current plugin bundle and exposes GUI/CLI bootstrap behavior
- `../harness/setup/scripts/build-self-contained-exe.ps1` - one-command helper that syncs schema-bundle hashes, runs the path and documentation-truth audits, builds the self-contained setup executable with temp build lanes, and refreshes the local generated `plugins/AnarchyAi.Setup.exe`
- `../harness/contracts/preflight-session.contract.json` - session preflight contract for meaningful governed work
- `../harness/contracts/schema-reality.contract.json` - first harness contract for `is_schema_real_or_shadow_copied`
- `../harness/contracts/harness-gap-state.contract.json` - environment gap-assessment contract for install/runtime/schema/adoption state
- `../harness/contracts/gov2gov-migration.contract.json` - second harness contract for `run_gov2gov_migration`
- `../harness/server/README.md` - local MCP launch lane and Codex hookup notes
- `../harness/server/dotnet/AnarchyAi.Mcp.Server.csproj` - Windows-first C# MCP server project (`net8.0` packaged path; `net48` target remains provisional)
- `../harness/server/dotnet/Program.cs` - current C# MCP entrypoint for the five core harness tools plus the experimental `direction_assist_test` module

## Anarchy-AI plugin delivery

- `../plugins/AnarchyAi.Setup.exe` - preferred single-file installer/bootstrap executable for Anarchy-AI after it is generated locally by the build helper; supports repo-local and user-profile lanes and is not tracked in git
- `../plugins/anarchy-ai/.codex-plugin/plugin.json` - repo-local plugin manifest for Anarchy-AI delivery
- `../plugins/anarchy-ai/.mcp.json` - plugin MCP declaration that launches the bundled runtime directly
- `../plugins/anarchy-ai/runtime/win-x64/AnarchyAi.Mcp.Server.exe` - bundled Windows-first self-contained MCP runtime used by the delivery plugin
- `../plugins/anarchy-ai/scripts/bootstrap-anarchy-ai.ps1` - compatibility/fallback repo-bootstrap lane for registration, quick install assessment, and bundle refresh after the bundle exists
- `../plugins/anarchy-ai/scripts/stop-anarchy-ai.ps1` - bounded runtime-lock assess, safe-release, and force-release command for the repo-local Anarchy-AI process
- `../plugins/anarchy-ai/scripts/start-anarchy-ai.cmd` - development helper only, not the intended delivery path
- `../plugins/anarchy-ai/skills/anarchy-ai-harness/SKILL.md` - usage layer for the five core harness tools, with explicit discovery for the experimental `direction_assist_test` module
- `../.agents/plugins/marketplace.json` - repo-local plugin marketplace entry

## Branding

- `../branding/branding-canon.json` - repo-authored branding source for display names, legal URLs, brand metadata, default publish URL, and bundle visual paths
- `../branding/assets/` - source-owned landing zone for future brand assets such as logos, icons, and marketplace imagery
- `../branding/published-materials/` - source-owned landing zone for publish-ready brand copy and future branded instruction additions
- `../harness/branding/Branding.Shared.cs` - shared C# binding over the generated branding constants
- `../harness/branding/scripts/generate-branding-artifacts.ps1` - generator for C#, PowerShell, and MSBuild branding artifacts

## Templates

- `../templates/AGENTS_TEMPLATE.md`
- `../templates/ANARCHY_AI_STARTUP_INSTRUCTION_TEMPLATE.md`
- `../templates/MODULE_AGENT_PROMPT_TEMPLATE.md`
- `../templates/PROMPT_PROJECT_TEMPLATE.md`
- `../templates/README_PROJECT_TEMPLATE.md`
- `../templates/CROSS_REPO_HANDOFF_TEMPLATE.md`
- `../templates/STARTUP_CONTEXT_BUDGET_WORKSHEET.md`

## Document roles

- **schema family (canonical):**
  - 4 schema JSON files + triage + Getting-Started
  - this is the AGENTS Heuristic Underlay
- **framework (canonical):**
  - startup prompt, refactor guide, budget model, control-plane prompt model
  - progress-over-patching, readiness model, cross-repo contracts, subagent safety
- **research (supporting):**
  - scratchpads, samples, assumption-failure pen test, comparison matrix
- **reference (supporting):**
  - schema READMEs, cleanup method, publication checklist, templates
- **operational:**
  - TODO, changelog, bug registers

## Rule

The framework should stay smaller than the systems it is trying to help.
If it becomes a second opaque system, it has failed.
