---
name: structured-commit
description: Use this skill when the user asks to commit pending changes with verification — triggers include "structured commit", "commit with verification", "commit pending changes", "loop commit", "commit everything", or any request for unattended/overnight commit work. Do NOT use when the user asks for a "quick commit" or "just commit" with a specific message they've already described; fall back to ordinary git commit flow in that case. The skill loops until the working tree is clean, batching related changes by purposeful theme, running verification checks with explicit if-gates, and recording residue as structured deferrals in each commit's trailer so nothing is buried.
---

# Structured Commit

Commit all pending changes in purposeful batches, with verification at each batch, until the working tree is clean. Residue — things noted but out of scope — is recorded in each commit's trailer rather than scattered as TODOs, silent omissions, or "I'll come back to it" debt.

## Invocation contract

- **Default**: loop until the working tree is committed or a halt condition fires.
- **`quick-commit` / "quick commit" phrasing**: skip verification, make one commit with a conventional message, record `Quick-commit: true` in the trailer so the skip is itself evidence, and exit.
- **`overnight` / late-night invocation**: same as default, plus append a running log to `.agents/anarchy-ai/structured-commit.log` after every pass so the morning reader has a timeline.

## The loop

Repeat until `git status` is clean or a halt condition fires.

### Phase 1: Inventory

- Run `git status` and `git diff` (staged and unstaged) to see everything pending.
- Group changes into **purposeful batches**. A batch is one coherent purpose — a feature slice, a doc pass, a refactor, a bugfix. Never "all files in a folder" by default.
- State the batch plan in one line per batch before starting. If the first batch can't be named in one sentence, the grouping is wrong.

### Phase 2: Stage one batch

- Stage only the files for the current batch with explicit `git add <path>` — never `git add -A` or `git add .`.
- Confirm with `git status` that only the intended files are staged.

### Phase 3: Verify — with explicit if-gates

Run each check. Every check ends in one of three outcomes, all of which are reported:

- **applied, clean**
- **applied, findings** (findings go to Phase 4 triage)
- **skipped, reason: `<reason>`**

Silent skipping is the bug this skill exists to prevent.

**Check A — Vision alignment** *(always applicable)*
- If a vision doc is discoverable at a conventional path (`AGENTS-Vision.md`, `docs/VISION.md`, `README.md`), read it and confirm the batch doesn't contradict stated direction.
- Skip with reason `no vision doc found` if none exists.

**Check B — API layer boundary** *(if-gated)*
- **Apply if** the batch modifies files under domain-module paths (heuristics: path contains `/Modules/`, `/domains/`, `/services/`, `/Features/`) **and** the files are business-logic code (`.cs`, `.ts`, `.tsx`, `.py`, `.go`, `.java`, `.rb`, `.kt`).
- **Skip with one of these reasons** when not applicable:
  - `docs-only batch`
  - `schema-only batch`
  - `install/config-only batch`
  - `tests-only batch`
  - `no recognizable domain-layer structure in touched paths`
- **When applied**: scan diffs for direct data-access patterns (raw SQL, ORM/DbContext use, direct database SDK calls) appearing inside controllers, UI handlers, other-domain services, or anywhere that should be going through an API/service layer. Direct DB writes from the wrong layer are a finding. Correct if the fix is in scope for the batch; defer otherwise.

**Check C — Contract layer** *(if-gated)*
- **Apply if** the batch introduces or modifies public entry points (HTTP controllers, public service methods, CLI handlers, MCP tool handlers, RPC endpoints).
- **Skip with one of these reasons** when not applicable:
  - `no public entry points touched`
  - `docs-only batch`
  - `schema-only batch`
  - `internal utilities only`
- **When applied**: verify each new or changed entry point has (1) explicit input and output types — no bare `object`, `any`, `dict[str, Any]`, or untyped `params` without justification — and (2) a meaningful verb in its name. Vague verbs to flag: `process_`, `handle_`, `do_`, `manage_`, generic `update_` when something more specific (`reconcile_`, `apply_`, `compute_`, `extract_`, `register_`) would fit.

**Check D — Structural footprint** *(always applicable)*
- Confirm no secrets, `.env` files, credentials, generated binaries, or oversized artifacts are staged.
- Confirm no `-A` or `.` style wildcard staging slipped in.

**Anarchy-AI invariant — schema mirror sync** *(if-gated, mechanical)*
- **Apply if** the batch touches any canonical schema file (`AGENTS-schema-*.json`, `AGENTS-schema-triage.md`, `Getting-Started-For-Humans.txt`) **or** any file under `plugins/anarchy-ai/schemas/`.
- **Apply if** the repo has `docs/scripts/test-schema-mirror-sync-compliance.ps1`. Otherwise skip with reason `no schema-mirror verifier in this repo`.
- **When applied**: run the verifier. If exit code is non-zero, the batch is not commit-eligible until root/mirror/manifest are resynced. This is the only mechanical verifier in the skill; unlike the prose checks, it does not depend on agent reading discipline and cannot be silently skipped. `quick-commit` phrasing does NOT bypass this check — typed-schema invariants survive the quick escape by design.

### Phase 4: Correct in-scope findings

- If a finding from Check B or C belongs to this batch's purpose, fix it now and re-stage.
- If a finding is architectural, cross-cutting, or belongs to a different batch, add it to the batch's deferral list without fixing.

### Phase 5: Commit

Use a structured commit message. The trailer is the product — it is how residue stops being buried.

```
<imperative subject line, <72 chars>

<optional 1–3 sentence body explaining the purposeful change>

Verified:
- Vision: applied, clean
- API-layer: applied, clean
- Contract-layer: applied, 1 finding corrected
- Structural: applied, clean

Deferred:
- <finding>: <one-line reason it is out of scope for this batch>

Skipped:
- <check name>: <one-line reason, e.g. "docs-only batch">
```

Rules:
- Never use `--amend` unless the user explicitly requested amending an existing commit.
- Never use `--no-verify`, `--no-gpg-sign`, or any hook-skipping flag unless explicitly requested.
- Never use `--force` or rewrite history unless explicitly requested.
- If a pre-commit hook fails, fix the underlying issue and create a new commit — never amend.

### Phase 6: Loop

Return to Phase 1. Halt when `git status` is clean, or a halt condition fires.

## Halt conditions

Halt and hand back to the human when any of the following applies. Each halt writes a self-contained resumption prompt so the next session picks up cleanly.

- **Merge conflict** in the working tree — surface it, don't resolve blindly.
- **Publish / push operation needed** — never push unattended; stop and report.
- **Irreversible destructive op would be required** (`reset --hard`, `branch -D`, `clean -fdx`, rewriting shared history) — never perform; stop and report.
- **User says stop.**
- **Pre-commit hook fails in a way that can't be resolved in scope** — fix attempt gets documented, halt.

## Output

Every pass produces:

- **The commit itself**, with the structured trailer.

In `overnight` mode, additionally append to `.agents/anarchy-ai/structured-commit.log`:

```
== <ISO-8601 timestamp> ==
Batch: <one-line purpose>
Files: <count>
Verified: <count checks applied clean>
Corrected: <count findings fixed in-scope>
Deferred: <count findings deferred>
Skipped: <count checks skipped with reason>
Commit: <sha> — <subject>
```

On halt or completion, write a **final summary** containing:

- Batches committed (SHAs + one-line subjects)
- Total deferrals across all batches (collated)
- Halt reason, if any
- Resumption prompt, if halted

## Residue is the product

The goal is not a perfect working tree. The goal is **named, auditable units of change with their residue surfaced**. A commit that lands with three deferrals in its trailer is a stronger artifact than one that silently contains the same three loose ends. The `Deferred:` list is what next-session work picks up from.
