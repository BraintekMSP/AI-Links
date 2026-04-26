# Anarchy-AI Skills

Operational skills that ship alongside the Anarchy-AI harness. Each skill is a named workflow method, not a guarantee of enforcement. The goal is to make durable behavior easy for agents to follow and easy for humans to audit.

## Skills

### `structured-commit`

Commit pending changes in purposeful batches with verification. Loops until the working tree is clean. Residue (things noted but out of scope) is recorded in each commit's trailer, so nothing is buried.

Primary use: **overnight / unattended commit work**. Also works interactively; use `quick-commit` phrasing to bypass verification and do a one-shot conventional commit.

### `structured-review`

Review commits on the current branch not yet on origin. Runs the same verification protocol as `structured-commit`. Produces a review artifact. Optionally makes follow-up commits for in-scope findings. Never pushes.

Primary use: **post-commit, pre-publish quality gate**.

### `chat-history-capture`

Extract repo-specific decisions, direction, findings, and open threads from the current chat or pasted chat history into Anarchy narrative arc records and vision docs with timestamp provenance.

Primary use: **governance memory capture** after long planning or debugging chats. It distinguishes message timestamps from capture timestamps and treats stale cache skill paths as evidence, not authority.

## Shared Philosophy

These skills share one operating posture: **name a purposeful unit of change, verify the relevant boundary, correct what is in scope, and name what is not**. The goal is not perfect output. The goal is named, auditable output with residue surfaced rather than buried.

The structured-code skills use the four verification checks below. `chat-history-capture` uses a different verification shape focused on provenance, register/record validity, timestamp precision, and show-before-write auditability.

## Four Structured-Code Verification Checks

| # | Check | Applies | Skips with reason |
|---|-------|---------|-------------------|
| A | Vision alignment | Always, if a vision doc exists | `no vision doc found` |
| B | API layer boundary | Business-logic code under domain-module paths | `docs-only`, `schema-only`, `install/config-only`, `tests-only`, or `no recognizable domain-layer structure` |
| C | Contract layer | Public entry points introduced or modified | `no public entry points touched`, `docs-only`, `schema-only`, `internal utilities only` |
| D | Structural footprint | Always | none |

Each check ends in one of three outcomes: **applied-clean**, **applied-with-findings**, or **skipped-with-reason**. Silent skipping is treated as a process bug.

The if-gates on B and C are there because direct-DB-write and contract-verb checks are only meaningful when the commit actually touches layered business code. A doc-only or schema-only commit has no API layer to verify, and forcing the check to fire in those cases would produce noise.

## Deployment

### Codex

These skills are registered via `plugin.json`'s `skills: "./skills/"` field. The Anarchy-AI plugin surfaces them to Codex when installed.

AI-Links also keeps repo-local mirrors at `.codex/skills/chat-history-capture/SKILL.md` and `.claude/skills/chat-history-capture/SKILL.md` so this source repo can use the capture workflow before the rebuilt plugin is installed and cache-materialized.

### Claude Code

Mirror copies for Claude Code live under `.claude/skills/`. The structured skills and chat-history capture skill should stay byte-for-byte aligned with their plugin-owned sources when mirrored.

### Generic / Other Hosts

Each `SKILL.md` is a self-contained prompt. An agent on any host can be directed to read and follow one of them when the matching objective comes up. The YAML `description` field describes when the skill should fire; the body describes what to do.

## Portable Schema Boundary

The executable skills travel with the Anarchy-AI plugin, not with the 6-file portable schema family. A repo that adopts only the schema family does not pick up the skill files unless it also installs the plugin or copies host-local mirrors.

The portable schema can still carry concise heuristics that point agents in the right direction. For example, `chat-history-capture` now exists both as:

- a portable narrative heuristic for archival decision capture and timestamp provenance
- a plugin skill for preflight, JSON validation, cache/source provenance checks, and final state capture

## Invocation Cheatsheet

| Situation | Invocation |
|-----------|------------|
| End of coding session, commit everything in structured batches | "structured commit" |
| Before bed, let it run unattended | "structured commit overnight" |
| Single obvious fix, do not want the loop | "quick commit: <message>" |
| Before pushing a feature branch | "structured review" |
| Review and fix trivial findings inline | "structured review fix-in-scope" |
| Review a specific range | "structured review HEAD~5..HEAD" |
| Capture decisions from a long chat into arc/vision docs | "chat history capture for <repo>" |
| Use the default prompt shape | "This is an archival capture of past decisions... Go through this chat and extract durable <repo>-specific decisions and direction for capture in the Arc and Vision docs..." |

## Morning Report

When `structured-commit` runs in overnight mode, it appends to `.agents/anarchy-ai/structured-commit.log` after every batch. The log is ordered oldest-first and is intended to be read quickly. It records timestamp, batch purpose, file count, verification outcomes, commit SHA, and halt reason if the loop stopped before clean-tree.

The `Deferred:` trailer in each commit message, plus the final summary, together form the complete residue list the next session picks up from.
