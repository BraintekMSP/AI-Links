# Anarchy-AI Skills

Operational skills that ship alongside the Anarchy-AI harness. Each skill is a named, enforceable method — not a recommendation — and is designed to prevent the "half-finished with buried action items" failure mode that single-pass work produces.

## Skills

### `structured-commit`
Commit pending changes in purposeful batches with verification. Loops until the working tree is clean. Residue (things noted but out of scope) is recorded in each commit's trailer, so nothing is buried.

Primary use: **overnight / unattended commit work**. Also works interactively; use `quick-commit` phrasing to bypass verification and do a one-shot conventional commit.

### `structured-review`
Review commits on the current branch not yet on origin. Runs the same verification protocol as `structured-commit`. Produces a review artifact. Optionally makes follow-up commits for in-scope findings. Never pushes.

Primary use: **post-commit, pre-publish quality gate**.

## Shared philosophy

Both skills apply the same discipline: **name a purposeful unit of change, verify against shared rules, correct what's in scope, name what isn't**. The goal is not perfect output — it is **named, auditable output with residue surfaced rather than buried**. This mirrors the gov2gov migration pattern applied to code-change work: the transition never claims to fix everything; it claims to produce a complete, audited, clearly-scoped unit of progress.

## The four verification checks (shared across both skills)

| # | Check | Applies | Skips with reason |
|---|-------|---------|-------------------|
| A | Vision alignment | Always, if a vision doc exists | `no vision doc found` |
| B | API layer boundary | Business-logic code under domain-module paths | `docs-only`, `schema-only`, `install/config-only`, `tests-only`, or `no recognizable domain-layer structure` |
| C | Contract layer | Public entry points introduced or modified | `no public entry points touched`, `docs-only`, `schema-only`, `internal utilities only` |
| D | Structural footprint | Always | — |

Each check ends in one of three outcomes — **applied-clean**, **applied-with-findings**, or **skipped-with-reason** — and all three are reported. Silent skipping is treated as the bug it is.

The if-gates on B and C are there because direct-DB-write and contract-verb checks are only meaningful when the commit actually touches layered business code. A doc-only or schema-only commit has no API layer to verify, and forcing the check to fire in those cases would produce noise and train people to ignore it.

## Deployment

### Codex
These skills are registered via `plugin.json`'s `skills: "./skills/"` field. The Anarchy-AI plugin surfaces them to Codex when installed.

### Claude Code
Mirror copies live at `.claude/skills/structured-commit/SKILL.md` and `.claude/skills/structured-review/SKILL.md` in the repo root so Claude Code auto-discovers them. The content is intentionally identical to the versions here — keep them in sync when editing.

### Generic / other hosts
Each `SKILL.md` is a self-contained prompt. An agent on any host can be directed to read and follow one of them when the matching objective comes up. The YAML `description` field describes when the skill should fire; the body describes what to do.

## Not part of the portable schema deployment set

These skills travel with the Anarchy-AI plugin, not with the 6-file portable schema family. A repo that adopts only the schema family does not pick up the skills unless it also installs the plugin (or copies the mirrors into its own `.claude/skills/`).

## Invocation cheatsheet

| Situation | Invocation |
|-----------|------------|
| End of coding session, commit everything in structured batches | "structured commit" |
| Before bed, let it run unattended | "structured commit overnight" |
| Single obvious fix, don't want the loop | "quick commit: <message>" |
| Before pushing a feature branch | "structured review" |
| Review and fix trivial findings inline | "structured review fix-in-scope" |
| Review a specific range | "structured review HEAD~5..HEAD" |

## Morning report (overnight mode)

When `structured-commit` runs in overnight mode, it appends to `.agents/anarchy-ai/structured-commit.log` after every batch. The log is ordered oldest-first and is intended to be read at breakfast in under 90 seconds. It records: timestamp, batch purpose, file count, verification outcomes, commit SHA, and halt reason if the loop stopped before clean-tree.

The `Deferred:` trailer in each commit message, plus the final summary, together form the complete residue list the next session picks up from.
