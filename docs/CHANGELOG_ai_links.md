# Changelog - AI-Links

## 2026-03-30

### Guardrail clarification

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
