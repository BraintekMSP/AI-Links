---
name: review-commits
description: Use when the user asks to review recent git changes, review commits in structured batches, verify changes against product vision, or continue reviewing until gaps are closed. This skill inventories the recent commit range and current working tree, groups changed files by purposeful batch, reports findings first, verifies contract/API boundaries when applicable, patches in-scope gaps, and repeats until the review is clean or a halt condition fires.
---

# Review Commits

Review recent git changes as durable product history, not as a loose diff scan. The goal is to catch gaps between the committed intent, product vision, changed surfaces, and contract boundaries before the work becomes misleading history.

## Operating Contract

- Review findings first, ordered by severity.
- Review in purposeful batches, not one file at a time by default.
- Verify that each batch communicates a coherent achievement historically.
- Verify product vision and cross-domain structure before accepting the batch as complete.
- If a real gap is found and the fix is in scope, patch it, validate it, and restart the affected review pass.
- If a changed surface is unexpected, investigate before accepting it.
- If contracts exist for a changed module, verify changed functions have explicit input data types, explicit output data types, and meaningful verbs.
- Stop and ask only when a fix requires a product decision, destructive action, or unavailable context.

## Inventory

1. Run `git status --short`.
2. Identify the review range. Prefer the recent local commits since the last known baseline, or the range the user named.
3. Run `git log --oneline` for the range.
4. Run `git diff --stat <base>..HEAD` and `git diff --name-only <base>..HEAD`.
5. Include uncommitted files in the review if the working tree is not clean.

## Batch Review

Group by purpose:

- Implementation behavior
- Tests and validation
- Generated deterministic surfaces
- Documentation and product doctrine
- Narrative or registry continuity
- Repo-level skills or local agent workflows

For each batch, check:

- Intent: Does the diff match the user request and commit message?
- Product vision: Does it preserve current repo claims, scratchpad boundaries, and stated non-goals?
- Cross-domain structure: Does it avoid coupling unrelated domains or treating adapter state as core truth?
- Contract layer: If the module has contracts, do changed functions expose explicit input/output types and meaningful verbs?
- Validation: Are tests, audits, smoke checks, or JSON/schema checks updated and run where appropriate?
- Historical clarity: Would a future reader understand why this batch exists?

## Findings

Use review-severity language:

- `P0`: data loss, security exposure, destructive behavior, or impossible-to-use output
- `P1`: likely functional regression or serious product-truth mismatch
- `P2`: maintainability, validation, contract, or documentation gap
- `P3`: clarity, naming, or future-proofing issue

Each finding should include:

- file path and line when possible
- why it is a bug or product gap
- what should change

If no findings remain, state that explicitly and list residual risks or checks not run.

## Correction Loop

When a finding is in scope:

1. Patch the smallest source surface that actually owns the problem.
2. Update generated mirrors only through the repo's generator when practical.
3. Run the narrowest relevant validation.
4. Re-review the affected batch.
5. Continue until findings are closed or a halt condition fires.

## Halt Conditions

Halt when:

- The required fix needs a product decision.
- The review range is ambiguous and cannot be inferred safely.
- A destructive action would be required.
- A merge conflict exists.
- A required external system is unavailable and the result would be misleading without it.

## Output

Final output should include:

- Findings first, or `No findings` if clean.
- Batches reviewed.
- Fixes applied, if any.
- Validation run and results.
- Residual risks or skipped checks with reasons.
