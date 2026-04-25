# AI-Links Collaboration Guardrails

## 1) Scope
- This repo is a reusable framework and template set.
- Keep content generic and public-safe by default.
- Do not store client secrets, client data, internal credentials, or environment-specific private details here.

## 2) Startup truth chain
- Start with `AGENTS.md`.
- Then read `docs/README_ai_links.md`.
- Then continue through the rest of the repo in a deliberate order.
- Do not self-select a subset and call that complete when the user asked for a repo ingest.
- For `AI-Links`, treat the repo as intentional operating context, not as a pile of optional reference docs.

## 2.5) AI-Links authoring boundary
- `AI-Links` is the authoring and source-truth repo for the schema family, harness, installer, docs, and product doctrine.
- In this repo, the schemas and harness must not be expected to self-fulfill their own design; the human owner is the operative harness for source-authoring judgment, continuity pressure, and promotion decisions.
- Treat harness output in this repo as evidence and assistance, not as the final authority over source intent.
- Do not migrate this repo-specific boundary into portable schemas, templates, plugin payloads, or consuming repos unless the human explicitly asks for a portable formulation.

## 3) Documentation rules
- `docs/TODO_ai_links.md` is active work only.
- Completed work moves to `docs/CHANGELOG_ai_links.md`.
- `docs/README_ai_links.md` is the repo runbook and navigation hub.
- `docs/STARTUP_CONTEXT_REFACTOR_GUIDE.md` is the reusable reference when a project needs startup-doc cleanup, prompt/runbook role split, or historical-doc triage.
- Templates should stay example-safe and copy-friendly.
- When reporting an ingest result, name what was actually loaded.
- Do not imply a full-repo ingest if only part of the repo was read.

## 4) Safety rules
- Do not describe destructive actions casually or as defaults.
- Treat `gitignored`, `untracked`, and `workspace-safe` as different concepts.
- Do not assume an ignored file is safe to delete just because git does not track it.
- Distinguish repo truth from workspace sprawl: source truth belongs in the repo; disposable cache/runtime scratch does not.
- Do not normalize repo-local caches, browser artifacts, or scratch output as good practice.
- Ask before redirecting non-critical tool/runtime/cache state into a synced workspace or repository tree.
- Prefer machine-local `AppData`, temp, or other non-synced lanes for caches, logs, build output, browser state, and scratch data.
- Do not present runtime patching or generated-output patching as the permanent answer when source/build inputs exist.
- Treat "clean clone + declared bootstrap" as the target; if ignored non-secret, non-DB files are required for success, that is a repo-hygiene gap.
- Do not recommend broad cleanup or deletion without explicit inventory and rollback planning.
- For destructive cleanup, inventory first, quarantine before delete, and revalidate the exact absolute target path in the same execution scope immediately before the move/remove action.
- Do not rely on command sequencing as a safety gate; destructive follow-up actions must be explicitly guarded by success checks and failure handling.

## 5) Public-safe rules
- Keep names, examples, and prompts generic.
- Prefer placeholders like `your-repo`, `your-app`, and `your-org`.
- Avoid company-specific jargon unless a doc is explicitly marked as an example.

## 6) Subagent stance
- Default subagents to off.
- If a repo adapts this framework, read-only explorers and exact file ownership should be the default delegation model.
- Destructive shell autonomy, repo-wide cleanup, and broad environment installs should remain disallowed by default.

## 7) Progress Over Patching
- Treat each change as durable product/module progress for a parent solution, not as an isolated patch.
- Before changing a surface, inspect the impacted schema, translation/hydration path, API/bundle contract, controller/service/view seams, scripts/startup helpers, and tests.
- Tiny slices are waste when a broader impact sweep and scoped plan are clearly required to avoid semantic drift or repeated break/fix churn.
- If local-first tables are missing fields needed to represent business meaning clearly, widen the local tables or add the supporting local tables instead of hiding meaning in payload blobs, mapper-only code, or UI-only logic.
- A change is not considered progress if it leaves source-of-truth ambiguous, silently relies on fallback/degraded behavior, or fixes one surface while bypassing related surfaces.
- Name tables, columns, classes, methods, variables, and properties so domain intent and likely impact are discoverable by reading the code without constant reference to external docs.
- When a change impacts scripts, bootstrap helpers, or validation paths, account for those dependencies in the same slice instead of treating them as optional follow-up.
- When shared business objects or workflow lanes are involved, require an explicit cross-repo impact review covering owner repos, producers, consumers, orchestration/runtime repos, and edge/intake tools.

## 8) Control-Plane Prompting
- When this framework is used from a control-plane repo, prefer generating the worker-agent prompt from the control-plane context instead of relying on the user's short description alone.
- For meaningful control-plane work, treat `25k` as the minimum starter context and `50k` as the healthy target.
- Use a bounded maintenance exception only for clearly local, low-impact hygiene work.
- Prefer a prompt packet that states the objective, source-of-truth lane, startup set, impact surface, acceptance contract, and prohibited shortcuts.
