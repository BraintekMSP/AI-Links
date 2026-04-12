# README - AI-Links (AGENTS Heuristic Underlay)

## Purpose

This is the runbook and navigation hub for `AI-Links`.

AI-Links houses the **AGENTS Heuristic Underlay** — a portable schema family that engineers what AI agents read before they act — alongside a broader framework for AI-assisted software delivery.

The schema family is the core. The framework docs are supporting material for teams adopting it.

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

- `../AGENTS-schema-governance.json` — multi-scope governance for code and operations
- `../AGENTS-schema-1project.json` — single-goal session continuity
- `../AGENTS-schema-narrative.json` — evolving narrative records for tribal knowledge capture
- `../AGENTS-schema-gov2gov-migration.json` — governance-to-governance migration contracts
- `../AGENTS-schema-triage.md` — routing document that picks the right schema
- `../Getting-Started-For-Humans.txt` — human onramp

### Evaluation and reference (stays in AI-Links):

- `../README-AGENTS-schema-governance.md`
- `../README-AGENTS-schema-1project.md`
- `../README-AGENTS-schema-narrative.md`
- `../README-AGENTS-schema-gov2gov-migration.md`
- `../AGENTS-schema-comparison-matrix.md`

Treat the `.json` files as canonical and the markdown companions as human-facing explainers.

## Research scratchpads

- `./SCRATCHPAD_prompt_efficacy_patterns.md` — prompt efficacy, research foundation (Dziri 2023, Leviathan 2025), product identity, measurable claims
- `./SCRATCHPAD_prophecy_precontext_influence.md` — pre-context influence theory, model-narration concept, layered maturity model, continuation boundary analysis
- `./SAMPLES_prophecy_precontext_influence_2026-04-10.md` — 10 observed deployment samples with analysis
- `./ASSUMPTION_FAILURE_PEN_TEST.md` — penetration-test-style review of the framework's control model

## Framework docs

- `./AI_COLLAB_STARTUP_PROMPT.md`
- `./STARTUP_CONTEXT_REFACTOR_GUIDE.md`
- `./STARTUP_CONTEXT_BUDGET_MODEL.md`
- `./CONTROL_PLANE_AGENT_PROMPT_MODEL.md`
- `./PROGRESS_OVER_PATCHING_MODEL.md`
- `./REPO_2_5_READINESS_MODEL.md`
- `./CROSS_REPO_CONTRACT_MODEL.md`
- `./SUBAGENT_SAFETY_MODEL.md`
- `./DOCUMENTATION_CLEANUP_METHOD.md`
- `./PUBLICATION_CHECKLIST.md`

## Harness scaffold

- `../harness/README.md` - local execution-harness scaffold and boundary
- `../harness/contracts/schema-reality.contract.json` - first harness contract for `is_schema_real_or_shadow_copied`
- `../harness/contracts/gov2gov-migration.contract.json` - second harness contract for `run_gov2gov_migration`
- `../harness/server/README.md` - local MCP launch lane and Codex hookup notes
- `../harness/server/dotnet/SpindleMcp.Server.csproj` - Windows-first dual-target C# MCP server scaffold (`net8.0` primary, `net48` fallback)
- `../harness/server/dotnet/Program.cs` - scaffolded C# MCP entrypoint for the first two harness tools

## Plugin delivery scaffold

- `../plugins/anarchy-ai/.codex-plugin/plugin.json` - repo-local plugin manifest for harness delivery
- `../plugins/anarchy-ai/.mcp.json` - plugin MCP declaration
- `../plugins/anarchy-ai/scripts/start-anarchy-ai.cmd` - launcher that locates and starts the local harness server
- `../plugins/anarchy-ai/skills/anarchy-ai-harness/SKILL.md` - thin usage layer for the first two harness tools
- `../.agents/plugins/marketplace.json` - repo-local plugin marketplace entry

## Templates

- `../templates/AGENTS_TEMPLATE.md`
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
  - TODO, changelog

## Rule

The framework should stay smaller than the systems it is trying to help.
If it becomes a second opaque system, it has failed.
