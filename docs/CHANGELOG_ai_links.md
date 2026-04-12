# Changelog - AI-Links

## 2026-04-10

### Scratchpad #2 governance-language note

- Added a new scratchpad `#2` note distinguishing adversarial human governance from interpretation-first governance.
- Preserved the observation that traditional governance often behaves like a reactive punishment surface masquerading as enforcement, and rarely seems concerned with helping the individual succeed.
- Added the counter-framing for the underlay:
  - assume little to no trust and build trust anyway
  - treat misunderstanding as a design condition
  - let faulty information and weak processes become enriched and valuable information
- Added a second scratchpad `#2` note placing taxonomy as a backstage build-time layer that shapes the delivery but should not dominate the canonical operating artifacts.
- Added the frontier/horse/harness metaphor as an explicitly separate paper-language formulation, preserving the stronger line that the execution harness keeps rider and horse from training each other into worse habits and the closing phrase `safe, useful anarchy`.
- Added the first harness scaffold under `harness/`:
  - `harness/README.md`
  - `harness/contracts/schema-reality.contract.json`
- Added the second harness contract:
  - `harness/contracts/gov2gov-migration.contract.json`
- Added the first launchable harness server scaffold:
  - `harness/server/README.md`
  - `harness/server/dotnet/SpindleMcp.Server.csproj`
  - `harness/server/dotnet/Program.cs`
- Defined the first harness function as `is_schema_real_or_shadow_copied`, with a bounded status model:
  - `real`
  - `partial`
  - `copied_only`
  - `fully_missing`
- Defined the second harness function as `run_gov2gov_migration`, with bounded inputs from the first function:
  - `partial`
  - `copied_only`
- Bound the second contract to non-destructive reconciliation and a re-evaluated resulting schema reality state.
- Kept the first server pass intentionally honest:
  - the MCP entrypoint loads the contracts and exposes the tool names
  - the real evaluation and reconciliation logic are not implemented yet
- The active runtime strategy is now:
  - preferred `.NET 8` self-contained single-file publish
  - fallback `net48` build when it exists
- Updated the plugin launcher to prefer the published `.NET 8` executable, then a `net48` executable, and only fail when neither exists and `dotnet` is unavailable.
- Bound the result contract to state-linked reasons and a single safe routing action:
  - `none`
  - `run_gov2gov_migration`
- Added the first repo-local delivery plugin scaffold:
  - `plugins/anarchy-ai/.codex-plugin/plugin.json`
  - `plugins/anarchy-ai/.mcp.json`
  - `plugins/anarchy-ai/scripts/start-anarchy-ai.cmd`
  - `plugins/anarchy-ai/skills/anarchy-ai-harness/SKILL.md`
  - `.agents/plugins/marketplace.json`
- The plugin now acts as the first extension-style delivery lane:
  - MCP declaration
  - launcher script
  - thin skill surface
  - marketplace discovery
- Renamed the visible plugin surface to `Anarchy AI` before the first commit so the forward-facing product name is settled early.
- The launcher script is responsible for:
  - preferring the published `.NET 8` executable
  - falling back to the `net48` executable when it exists
  - publishing `.NET 8` locally only when no executable is present and the SDK is available
- Updated the repo runbook so the harness scaffold is discoverable without mixing runtime concerns into the schema family itself.

Validation:

- Documentation/contract scaffold update only.
- No runtime behavior changed.

### Sibling vocabulary alignment continuation

- Continued the post-`efe9ae0` vocabulary sweep across sibling and human-facing surfaces so the schema family no longer partially teaches `blocked-until` while governance teaches `report-to-human`.
- Updated `AGENTS-schema-gov2gov-migration.json` to rename `blocked-until-values` to `report-to-human-values` and align the `human-confirms` definition with the governance wording.
- Updated `AGENTS-schema-narrative.json` to replace the remaining `blocked-until` gate language in compress mode and cold-start authoring with `report-to-human`.
- Updated `AGENTS-schema-triage.md` and `Getting-Started-For-Humans.txt` so the human-facing routing/onramp docs now teach `report-to-human` and `first-time-governance-authoring` instead of older `blocked-until` / `greenfield` phrasing.
- Removed remaining explicit `required: false` declarations from the schema family where optionality is already implied by absence of `required`.
- Added explicit triage posture that `1project` should give way to governance for generalized solutions and is better treated as a bounded-project lane than a product/default lane.
- Corrected the `incomplete-session` audit rationale so it now centers pseudo-state and exit-grammar risk rather than leaning on governance measurement parity.
- Kept this pass intentionally narrow:
  - no broader negation rewrite
  - no change to the verbatim `CANNOT` context-ordering rule
  - no revisit of `invalid -- do not proceed`

Validation:

- Vocabulary alignment pass only.
- No runtime behavior changed.

### Scratchpad #2 sample extraction

- Extracted the full observed sample corpus out of `docs/SCRATCHPAD_prophecy_precontext_influence.md` into the dated companion file `docs/SAMPLES_prophecy_precontext_influence_2026-04-10.md`.
- Replaced the large in-file sample block in scratchpad `#2` with a short pointer section so the scratchpad now stays focused on:
  - core question
  - conceptual distinctions
  - current notes
  - working claims
- Updated `docs/README_ai_links.md` so the dated sample corpus is directly discoverable from the runbook.

Validation:

- Documentation organization update only.
- No runtime behavior changed.

### Scratchpad #2 continuation notes

- Added a clarification that the startup chain was not optimized for tiny context so much as low-entropy routing:
  - thin dispatcher
  - clear authority-file naming
  - trusted voluntary ingest into the larger context body
- Added a sharper diagnosis for proof-lane recovery language:
  - a recovery and healing system for rare failures can become an exit system if the failures are common enough
- Recorded the likely next audit target:
  - continuation-enforcing language
  - recovery-only language
  - mixed language that accidentally legitimizes early exit
- Added a token-efficiency note clarifying that the schema appears to save cloud / serialized spend by converting repeated reasoning work into stable, cacheable, parallelizable starter structure rather than by shrinking context.
- Added a research bridge note preserving the strongest carry-forward from scratchpad `#1`:
  - Dziri et al. supports planning modules and fear of composition depth
  - Leviathan et al. supports repeated exposure in the parallelizable input domain
  - the project's own collisions include low-entropy startup routing, deliberate specific naming, repeated structural exposure, and skepticism toward context compression
- Preserved the important boundary condition:
  - those research-backed wins appear strongest in startup, routing, naming, and authority recognition
  - later samples still expose a narrower weakness around active proof continuity and anti-exit behavior
- Added a new hypothesis-shaped note that some of the strongest project outcomes may involve a second steering mechanism beyond the underlay itself:
  - one technician engaging the agent
  - another technician later supplying outside perspective, authority signaling, pressure, or reframing
- Recorded the likely candidate mechanisms without overclaiming them:
  - social-authority effect
  - context-refresh / re-anchoring effect
  - progress-pressure effect
  - phrasing diversity effect

Validation:

- Documentation/research framing update only.
- No runtime behavior changed.

## 2026-04-09

### Prophecy scratchpad

- Added `docs/SCRATCHPAD_prophecy_precontext_influence.md` as a second research scratchpad exploring the framework as a pre-context influence system rather than an accuracy system.
- The scratchpad centers the question:
  - can durable pre-read structure positively influence later session outcomes with reliability before it has seen later session-specific context
- The note frames the system in terms of:
  - trajectory shaping
  - pre-context control
  - outcome steering
  - prophecy as an engineering question rather than a mystical one
- Extended the note to clarify:
  - why `underlay` is the correct name for a closed-system steering layer
  - why the design intentionally prefers contract-style influence over overclaimed enforcement
  - why some intentionally unenforceable controls may still matter if they steer context before the session path hardens
- Recorded an additional product signal:
  - agents tend to minify or "slice" meaningful work into semantically unsafe local patches by default
  - schema re-pointing can often restore the obvious broader sequence without adding new task-specific instruction
- Recorded an observed sample where a broad, under-specified request still propagated through multiple repo surfaces, proof lanes, and local guardrail mechanisms without requiring schema edits.
- Recorded a second observed sample where schema reingest plus recent-history review surfaced implementation-discipline failures and then drove broad corrective work with explicit proof-lane qualification.
- Recorded a third observed sample where the agent correctly identified the unfinished governance layer, propagated module scope on demand, then shifted from anti-structure hesitation into module objective-pack refinement after schema-aligned user correction.
- Recorded a fourth observed sample where the agent initially tried to mutate the governance schema to fit an implementation omission, the user stopped that move, and the correct recovery was to implement the schema's existing provision rather than redesign the contract.
- Recorded a fifth observed sample where a stray artifact started pulling the session toward a plausible but non-canonical missing-concept theory, and canonical schema reingest restored the distinction between real provisions, invented artifacts, and hypothetical future extensions.
- Recorded a sixth observed sample where rapid backlog/objective prioritization was later re-grounded against commits and pending worktree state, showing the underlay helping separate planned work from actually shipped or still-risky work.
- Recorded a seventh observed sample where a long chat was converted into schema-backed interaction/program method coverage, stale generated artifacts were treated as a real completeness failure, and a user vocabulary correction (`slice`) was absorbed as a trajectory-steering constraint rather than a cosmetic wording preference.
- Recorded an eighth observed sample where schema reingest plus recent-history review classified commit churn into recurring failure families such as ownership drift, policy discovered in UI, late consolidation, and optimistic closure, showing the underlay functioning as a retrospective diagnostic lens.
- Recorded a ninth observed sample where a broad consolidation review became a sustained multi-turn consistency program across wrappers, docs, verifier harnesses, archive boundaries, and UI shell ownership, with user correction against needless fragmentation absorbed into larger coherent checkpoints.
- Recorded a tenth observed sample where schema reingest, subagent-trust questioning, richer proof/measurement correction, and a governed path-forward plan converged into gate-based closure with explicit operator-in-the-loop proof, showing the underlay resisting premature closure across multiple truth layers.
- Recorded an eleventh observed sample where a full-chat traceability pass was promoted into schema-backed interaction/program methods, linked back to product requests, remeasured through governance, and explicitly prevented from canonizing transient conversational noise as permanent contract.
- Recorded a twelfth observed sample where recent commits in a neighboring repo were used to expose local durability gaps, Git history was bootstrapped for the migrations project, and user correction against partial "highest-value" follow-up pushed the agent into a broader schema-informed hardening pass that still disclosed unproven residuals honestly.
- Recorded a thirteenth observed sample where an authenticated proof lane was dropped after the first meaningful finding, the user forced that continuity failure into schema-backed stop-point rules and guarded repo truth, and the recovery path evolved from a failure list into a real execution-supporting plan with critical path, complementary work, and validation gates.
- Recorded a fourteenth observed sample where a user-supplied workflow identity was acknowledged but not materialized into same-pass executable proof quickly enough, forcing a new schema-backed rule that workflow-test config needed for the active proof lane must become runnable immediately rather than remaining chat-adjacent.
- Recorded a fifteenth observed sample where recurring workflow requests were correctly promoted into schema-backed methods and guardrails, but the actual ignored local secret was still missing until the user forced it, exposing the gap between "represented through governance" and "materialized for execution."
- Recorded a sixteenth observed sample where live use of a concrete test operator rapidly narrowed the auth blocker to a gateway transport/pathing problem and was documented into repo truth, but still left the user asking where the explicit action items were, exposing the gap between narrowed findings and preserved momentum.
- Recorded a seventeenth observed sample where runtime logs exposed host-contract drift and launcher/status inconsistency around `wo.braintek.local` versus `localhost`, the user forced the obvious broader cleanup instead of another staged micro-step, and the resulting normalization aligned most runtime surfaces while still honestly preserving a smaller localhost-biased readiness-message residual.
- Recorded an eighteenth observed sample where authenticated-workflow defaults that should have been settled were still being re-asked in chat, the user forced those decisions into first-copy governance and helper defaults, and the later attempt with a concrete operator identity produced a more disciplined diagnosis that separated identity truth from host, TLS, and local proof-lane noise.
- Added current notes clarifying that the most troubling recurring failures now look like exit-condition failures more than context failures, and that prompt/schema steering may be near ceiling for this class without stronger external state, gating, validation, and restartability around active proof lanes.
- Added current notes refining the earlier "language versus non-language" framing into a more accurate distinction between structured linguistic artifacts and runtime-coupled state, clarifying that the underlay is already doing sophisticated language-shaped control and that the likely complement is less-purely-model-mediated state rather than some impossible external escape from the closed system.
- Added current notes defining `model narration`, clarifying that active proof files are lane-scoped runtime-adjacent state rather than one-file-per-prompt ceremony, and capturing a simple emerging mental schema of `1. prose guidance`, `2. structured linguistic artifacts`, `3. runtime-coupled state`, `4. harder gates`, with the current system read roughly as `2.2 / 4`.
- Added current notes tightening the naming around `AI heuristic underlay` and capturing the line: `Agent behavior is heuristic and must be shaped before its local heuristics take over.`
- Added comparison clarifications from scratchpad `#1`, including:
  - a softer and more accurate reading of `pretokenization`
  - explicit acknowledgment that later harness thinking came from later testing pressure
  - correction that deliberate label engineering was already heavily present
  - refinement that `context compression` is the first-class enemy, not compression in general
  - note that ambiguity/grammar steers likely become operational only with harness support
  - note that independent authorship of `#1` and `#2` is a feature for resisting confirmation bias
- Updated `docs/README_ai_links.md` so both scratchpads are explicitly discoverable from the runbook.

Validation:

- Documentation/research framing update only.
- No runtime behavior changed.

### Assumption-failure penetration review

- Added `docs/ASSUMPTION_FAILURE_PEN_TEST.md` as a penetration-test-style review of the framework's assumption surfaces rather than a runtime app assessment.
- The review focuses on places where control strength depends on undeployed enforcement, underconfigured measurement, human approval quality, or consuming-system discipline.
- Findings now distinguish:
  - holes that should be patched now
  - holes that should be patched before broader downstream adoption
  - holes that can be risk-accepted only in bounded environments
- Updated `docs/README_ai_links.md` so the new review is discoverable from the repo runbook.

Validation:

- Documentation/framework update only.
- No runtime behavior changed.

## 2026-04-07

### AGENTS schema family hosting

- Added the portable schema JSON family at the repo root:
  - `AGENTS-schema-governance.json`
  - `AGENTS-schema-1project.json`
  - `AGENTS-schema-narrative.json`
- Moved the human-facing schema companions and comparison guide into `AI-Links` so the reusable framework repo now hosts the public-safe schema explanation set:
  - `README-AGENTS-schema-governance.md`
  - `README-AGENTS-schema-1project.md`
  - `README-AGENTS-schema-narrative.md`
  - `AGENTS-schema-comparison-matrix.md`
- Updated `README.md` and `docs/README_ai_links.md` so the schema family is part of the normal repo navigation instead of living only in `TheLinks`.

Validation:

- Cross-repo documentation move/copy only.
- No runtime behavior changed.

## 2026-04-02

### Control-plane agent prompting

- Added `docs/CONTROL_PLANE_AGENT_PROMPT_MODEL.md` to codify the pattern where a control-plane repo should generate richer worker-agent prompts for narrower module/repo agents.
- Added `templates/MODULE_AGENT_PROMPT_TEMPLATE.md` so downstream repos can produce structured module-agent prompt packets with:
  - objective
  - source-of-truth
  - startup set
  - impact surface
  - acceptance contract
  - prohibited shortcuts
- Updated `AGENTS.md` and `docs/README_ai_links.md` so:
  - control-plane prompting is now explicit top-level guidance
  - serious control-plane work uses `25k` minimum starter context and `50k` target context
  - bounded maintenance work is still allowed to use a smaller context pack

Validation:

- Documentation/framework update only.
- No runtime behavior changed.

### Progress-over-patching model

- Added `docs/PROGRESS_OVER_PATCHING_MODEL.md` as a reusable guardrail for:
  - impact-first planning instead of narrow symptom patching
  - widening local-first tables when translation friction reveals missing structure
  - keeping source-of-truth, fallback/degraded behavior, and naming/discoverability explicit
- Updated `AGENTS.md`, `docs/CROSS_REPO_CONTRACT_MODEL.md`, the cross-repo templates, and `docs/README_ai_links.md` so the new rule is part of the startup path and reusable framework spine.
- Cross-repo standard now explicitly requires external impact-surface review across owner repos, producer/consumer repos, orchestration/runtime repos, and edge/intake tools.

Validation:

- Documentation/framework update only.
- No runtime behavior changed.

## 2026-03-31

### Startup-context budget model

- Added `docs/STARTUP_CONTEXT_BUDGET_MODEL.md`:
  - reusable scoring model for predicting startup-token budgets
  - explicit complication-density calculation
  - documentation-adequacy checks
  - worked examples for `Workorders`, `TheLinks`, and `AI-Links`
- Added `templates/STARTUP_CONTEXT_BUDGET_WORKSHEET.md` for repeatable repo scoring.
- Updated `docs/README_ai_links.md` so the startup-context budget model and worksheet are part of the main reusable navigation set.

Validation:

- Documentation/framework update only.
- No runtime behavior changed.

## 2026-03-30

### Startup-context refactor guide and prompt-role clarification

- Added `docs/STARTUP_CONTEXT_REFACTOR_GUIDE.md` as a reusable reference for:
  - expanding mandatory startup context before cleanup
  - splitting `AGENTS`, runbook README, project prompt, TODO, and changelog roles cleanly
  - preserving historical docs before any archival/move pass
- Updated `AGENTS.md` and `docs/README_ai_links.md` so startup-doc cleanup and prompt/runbook refactor work has an explicit navigation target instead of being rediscovered through chat.
- Rebuilt `templates/PROMPT_PROJECT_TEMPLATE.md` around the intended prompt role:
  - implemented baseline
  - active direction
  - discretionary biases for new work
  - when TODO and CHANGELOG actually need to be loaded
  - explicit separation from hard rules in `AGENTS.md`

Validation:

- Documentation review only.
- No runtime application behavior exists in this repository.

### Guardrail clarification

- Removed the weaker "read only what's needed" bias from the baseline ingest language:
  - the startup spine is now an entry order, not a license to ignore the rest of the repo
  - `AI-Links` is treated as intentional operating context rather than a subjective pile of optional docs
  - full-repo ingest now stays explicit and should not be narrowed by agent preference
- Tightened the repo-ingest contract so "ingest the entire repo" now means exactly that:
  - agents should not self-select a subset of "important" docs and call that full ingest
  - ingest acknowledgments should distinguish startup-spine ingest, task-relevant subset ingest, and true full-repo ingest
  - agents should state any exclusions explicitly instead of implying full coverage
- Tightened the ingest acknowledgment shape:
  - after repo ingest, the reply should still echo the working directory or repo path
  - the reply should still end with one short confirmation question that this is the right working location
- Tightened `AGENTS.md` and `docs/AI_COLLAB_STARTUP_PROMPT.md` so the default behavior contract now says more explicitly:
  - `gitignored`, `untracked`, and `workspace-safe` are different concepts
  - synced workspaces should not receive non-critical runtime/cache state without an explicit decision
  - machine-local `AppData` / temp lanes should be preferred for disposable output
  - `clean clone + declared bootstrap` is the target, not warm-cache success
  - destructive cleanup should inventory first, quarantine before delete, and revalidate the exact target path in-scope immediately before execution
  - command sequencing is not itself a safety gate for destructive follow-up actions
- Tightened the startup-intake guidance so repo ingestion does not immediately jump to developer-style repo classification:
  - the default intake now clarifies user outcome, audience, and "done" in plain language first
  - repo-shape and implementation-taxonomy questions are now explicitly secondary
- Replaced the weaker "infer it from how the user talks" pattern with an explicit one-question communication/autonomy gauge:
  - agents should ask one short gauge question when technical-depth assumptions are unclear
  - the answer is treated as a communication/autonomy contract, not a competence judgment
  - the working level can be updated later as the conversation reveals stronger context or specific knowledge gaps
- Updated `templates/PROMPT_PROJECT_TEMPLATE.md` so new-project intake captures `User outcome`, `Communication / autonomy gauge`, `Audience / operator`, and `Done when` up front.
- Added an explicit wrong-workspace guard to the startup model:
  - after repo ingest, the agent should echo the current working directory/repo path
  - then ask one short confirmation question that this is the correct working location
  - this is intentionally a single confirmation step, not a long intake chain
- Updated `docs/README_ai_links.md` purpose wording to call out repo-truth vs workspace-sprawl boundaries.

Validation:

- Documentation review only.
- No runtime application behavior exists in this repository.

## 2026-03-28

### Initial scaffold

- Added root repo guardrails in `AGENTS.md`.
- Added runbook and navigation hub in `docs/README_ai_links.md`.
- Added a generic AI startup prompt in `docs/AI_COLLAB_STARTUP_PROMPT.md`.
- Added a reusable repo `2.5` readiness model in `docs/REPO_2_5_READINESS_MODEL.md`.
- Added a generic cross-repo contract model in `docs/CROSS_REPO_CONTRACT_MODEL.md`.
- Added a subagent safety model in `docs/SUBAGENT_SAFETY_MODEL.md`.
- Added a documentation cleanup method in `docs/DOCUMENTATION_CLEANUP_METHOD.md`.
- Added a publication checklist in `docs/PUBLICATION_CHECKLIST.md`.
- Added reusable templates for `AGENTS`, project prompt, project README, and cross-repo handoffs.

Validation:

- Scaffold and documentation review only.
- No runtime application behavior exists in this repository yet.

### Publication stance

- Added a custom `LICENSE` with an all-rights-reserved, permission-required model.
- Added a root `NOTICE` to make the public-reference-only stance explicit.
- Updated `README.md` and `docs/PUBLICATION_CHECKLIST.md` to reflect the custom publication model.
