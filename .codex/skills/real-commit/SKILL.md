---
name: real-commit
description: Use when the user asks to commit pending changes with proof-oriented structured batches, including "real commit", "commit all pending changes", "commit in structured batches", "verify intent before commit", or "continue until committed". This skill inventories every pending change, groups changes by purpose, verifies each batch against product vision and applicable API/contract boundaries, corrects in-scope gaps, and commits until the tree is clean or a halt condition fires.
---

# Real Commit

Make commits that are historically useful. A real commit is not just a clean working tree. It is a named unit of progress whose purpose, verification, corrected gaps, and remaining residue are visible in the commit itself.

## Operating Contract

- Loop until all pending changes are committed or a halt condition fires.
- Commit in purposeful batches. One batch should communicate one durable achievement.
- Stage explicit paths. Do not use `git add -A` or `git add .` as the default path.
- Before each commit, verify the staged batch still matches the user's stated intent and the product vision.
- If a gap is found and it belongs to the batch, correct it before committing.
- If an impacted surface is unexpected, investigate before committing. Correct, split, or halt if it changes the meaning of the batch.
- Never amend, force-push, rewrite history, or run destructive cleanup unless the user explicitly requested it.

## Loop

1. Inventory

- Run `git status --short`.
- Read unstaged and staged diffs.
- Identify generated, binary, ignored, or local-only artifacts before staging.
- Group pending changes by product purpose, not by folder convenience.

2. Plan Batches

- Name each batch in one sentence.
- Prefer smaller commits when one change is implementation and another is doctrine, docs, generated metadata, or tests.
- Keep generated files with the source change that requires them when they are deterministic outputs.
- Do not hide unrelated cleanup inside a feature commit.

3. Verify One Batch

Always check:

- Intent: the staged diff matches the user's request.
- Product vision: the staged diff does not contradict current repo vision, runbooks, or scratchpad claims.
- Surface scope: every staged file belongs to the batch purpose.
- Structural safety: no secrets, `.env` files, credentials, oversized generated artifacts, or accidental local telemetry are staged.
- Tests or audits: run the narrowest meaningful checks available for the staged batch, then broader checks when the batch changes shared contracts, setup, runtime, or docs.

Apply when relevant:

- API layer: if business logic crosses domains or modules, verify calls go through API/service layers instead of direct database writes.
- Contract layer: if public entry points changed, verify explicit input types, explicit output types, and meaningful verbs.
- Workorders repo: verify every changed function uses API layers across domains/modules, correct direct database writes, and ensure each function has a contract layer with explicit input/output data types and meaningful verbs.
- Schema mirrors: if schema or mirrored bundle surfaces changed, run the repo's schema/mirror verifier when one exists.
- Setup/runtime: if setup, installer, plugin payload, or runtime surfaces changed, build or smoke-test the deployable path when practical.

4. Correct

- Correct in-scope findings before committing.
- If correction changes the batch purpose, re-inventory and split the batch.
- If a finding is real but out of scope, record it in the commit trailer as a deferral.

5. Commit

Use a message that records purpose and verification:

```text
<imperative subject, under 72 characters>

<brief body explaining why this batch exists>

Verified:
- Intent: applied, clean
- Product-vision: applied, clean
- Surface-scope: applied, clean
- Structural-safety: applied, clean
- Tests: applied, clean

Deferred:
- None

Skipped:
- Workorders API-layer: not a workorders repo
- Contract-layer: no public entry points touched
```

If a check is skipped, state why. Silent skips are not acceptable.

6. Repeat

- Return to inventory after every commit.
- Continue until `git status --short` is clean or a halt condition fires.

## Halt Conditions

Halt and report when:

- A merge conflict exists.
- A destructive action would be required.
- A required validation cannot run and committing would misrepresent proof.
- The staged batch contains unrelated user changes that cannot be separated safely.
- A correction would require a new product decision.
- The user says to stop.

## Completion Report

When finished, report:

- Commit SHAs and subjects.
- Checks run and any checks skipped with reasons.
- Deferrals, if any.
- Remaining working-tree state.
