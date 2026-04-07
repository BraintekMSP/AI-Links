# Changelog - AI-Links

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
