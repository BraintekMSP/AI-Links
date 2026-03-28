# AI-Links Collaboration Guardrails

## 1) Scope
- This repo is a reusable framework and template set.
- Keep content generic and public-safe by default.
- Do not store client secrets, client data, internal credentials, or environment-specific private details here.

## 2) Startup truth chain
- Start with `AGENTS.md`.
- Then read `docs/README_ai_links.md`.
- Then load only the exact docs or templates needed for the task.

## 3) Documentation rules
- `docs/TODO_ai_links.md` is active work only.
- Completed work moves to `docs/CHANGELOG_ai_links.md`.
- `docs/README_ai_links.md` is the repo runbook and navigation hub.
- Templates should stay example-safe and copy-friendly.

## 4) Safety rules
- Do not describe destructive actions casually or as defaults.
- Do not normalize repo-local caches, browser artifacts, or scratch output as good practice.
- Do not present runtime patching or generated-output patching as the permanent answer when source/build inputs exist.
- Do not recommend broad cleanup or deletion without explicit inventory and rollback planning.

## 5) Public-safe rules
- Keep names, examples, and prompts generic.
- Prefer placeholders like `your-repo`, `your-app`, and `your-org`.
- Avoid company-specific jargon unless a doc is explicitly marked as an example.

## 6) Subagent stance
- Default subagents to off.
- If a repo adapts this framework, read-only explorers and exact file ownership should be the default delegation model.
- Destructive shell autonomy, repo-wide cleanup, and broad environment installs should remain disallowed by default.
