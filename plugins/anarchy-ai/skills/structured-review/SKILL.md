---
name: structured-review
description: Use this skill when the user asks to review recent commits before pushing or publishing — triggers include "structured review", "pre-publish review", "review my changes", "review before push", "review recent commits". Reads commits on the current branch that are not yet on origin (or a user-supplied range) and runs the same verification protocol as structured-commit against each batch, producing a review artifact and optionally follow-up commits. Do NOT use for reviewing someone else's pull request — use ordinary `gh pr view` or the `review` slash command for that. This skill never pushes.
---

# Structured Review

Review commits on the current branch not yet on origin, batch by batch, running the verification checks against each. Produce a review artifact and, when findings are in scope, follow-up commits. Never pushes — publishing is always the human's call.

## Invocation contract

- **Default**: review every commit on the current branch that is ahead of its tracked remote (`git log origin/<branch>..HEAD`).
- **Range override**: if the user gives a range like `review HEAD~5..HEAD` or `review since <sha>`, use that instead.
- **`fix-in-scope` flag** or user confirms when asked: make follow-up commits for in-scope findings using structured-commit's Phase 5 format.
- **No `fix-in-scope`**: review-only. Findings are surfaced in the review artifact; no commits are made.

## The loop

### Phase 1: Inventory

- Determine the range (default or user-supplied).
- `git log --oneline <range>` to see the commits.
- Group commits into **review batches** by purposeful theme. Often one commit is one batch; multi-commit features can be reviewed as a unit when they share one purpose.
- Name each batch in one sentence before reviewing.

### Phase 2: Review one batch

For each commit in the batch, read `git show <sha>` and run the four checks below — same definitions and if-gates as `structured-commit/SKILL.md` Phase 3. Every check ends in **applied-clean**, **applied-with-findings**, or **skipped-with-reason**.

**Check A — Vision alignment** *(always applicable)*
- Skim the vision doc at conventional paths (`AGENTS-Vision.md`, `docs/VISION.md`, `README.md`) and confirm the commit doesn't contradict stated direction.
- Skip with reason `no vision doc found` if none exists.

**Check B — API layer boundary** *(if-gated)*
- **Apply if** the commit touches business-logic code (`.cs`, `.ts`, `.tsx`, `.py`, `.go`, `.java`, `.rb`, `.kt`) under domain-module paths (`/Modules/`, `/domains/`, `/services/`, `/Features/`).
- **Skip with one of**: `docs-only commit`, `schema-only commit`, `install/config-only commit`, `tests-only commit`, `no recognizable domain-layer structure in touched paths`.
- **When applied**: look for direct DB access (raw SQL, ORM/DbContext, database SDK) in controllers, UI handlers, cross-domain services, or other wrong-layer code. Direct DB writes from the wrong layer are findings.

**Check C — Contract layer** *(if-gated)*
- **Apply if** the commit introduces or modifies public entry points (HTTP controllers, public services, CLI handlers, MCP tool handlers, RPC endpoints).
- **Skip with one of**: `no public entry points touched`, `docs-only commit`, `schema-only commit`, `internal utilities only`.
- **When applied**: check explicit input/output types (no bare `object`, `any`, `dict[str, Any]` without justification) and meaningful verbs (flag `process_`, `handle_`, `do_`, `manage_`, vague `update_`).

**Check D — Structural footprint** *(always applicable)*
- Confirm the commit didn't sneak in secrets, generated artifacts, or oversized binaries.

**Anarchy-AI invariant — schema mirror sync** *(if-gated, mechanical)*
- **Apply if** any reviewed commit touches canonical schema files (`AGENTS-schema-*.json`, `AGENTS-schema-triage.md`, `Getting-Started-For-Humans.txt`) or files under `plugins/anarchy-ai/schemas/`.
- **Apply if** `docs/scripts/test-schema-mirror-sync-compliance.ps1` exists. Otherwise skip with reason `no schema-mirror verifier in this repo`.
- **When applied**: run the verifier at the current HEAD. A non-zero exit becomes a review finding — in-scope for a follow-up resync commit if `fix-in-scope` is set, otherwise surfaced in the review artifact as a blocker on publish. This is the only mechanical check in the review; do not skip silently.

### Phase 3: Triage findings

Every finding falls into one of three buckets, and every finding is recorded in its bucket:

- **In-scope for a follow-up commit** — the finding can be fixed without changing the purposeful framing of the batch. Example: the batch added a new endpoint with an untyped parameter; adding the type is in scope.
- **Architectural / out-of-scope** — surface as a recommendation in the review artifact; do not fix.
- **False positive** — document why the check fired but the code is actually correct.

### Phase 4: Apply in-scope fixes

Only if `fix-in-scope` was passed or the user confirms when prompted.

- Make follow-up commits using structured-commit's Phase 5 format.
- Each follow-up commit's trailer adds an `Addresses:` line pointing to the finding-id it resolves, so the review artifact and the git history cross-reference cleanly.
- Never amend an existing commit — always create a new one.

### Phase 5: Produce the review artifact

Write to `.agents/anarchy-ai/structured-review-<ISO-timestamp>.md`:

```
# Structured Review — <branch> — <timestamp>

## Range reviewed
<commit range>
<count> commits

## Batches

### Batch 1: <one-sentence purpose>
Commits: <sha1>, <sha2>

#### Findings
- Check A — Vision: applied, clean
- Check B — API-layer: applied, 1 finding
  - F-001: `PeopleController.UpdateRecord` writes directly to `DbContext`; should go through `IPeopleService`. [in-scope]
- Check C — Contract-layer: skipped — no public entry points touched
- Check D — Structural: applied, clean

#### In-scope fixes applied
- <new commit sha>: addresses F-001

#### Deferred (out-of-scope)
- F-002: Cross-module coupling between CommercialOps and PeopleOps around service agreements — architectural, needs its own design pass.

<repeat per batch>

## Summary
- Batches reviewed: N
- Checks run: X applied-clean, Y with findings
- Findings fixed in-scope: Z
- Findings deferred: W
- Recommendation before publish: <proceed | address deferrals | further review needed>
```

### Phase 6: Loop

Next batch. Halt when the range is covered, or a halt condition fires.

## Halt conditions

- Same as structured-commit: merge conflicts, destructive ops required, user stop.
- **Never pushes**, regardless of recommendation. `git push`, `gh pr create`, and any publishing op are explicitly out of scope.

## Output

- The **review artifact** (always), at the path above.
- Optional **follow-up commits** (only with `fix-in-scope` or user confirmation).
- A short **console summary** at the end: counts per batch, recommendation, path to the artifact file.

## Residue is the product

Same principle as structured-commit: a review that names its deferred findings is a stronger artifact than one that silently signs off. The review file becomes the audit trail for what was seen and what was deliberately set aside.
