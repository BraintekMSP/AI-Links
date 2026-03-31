# Startup Context Refactor Guide

## Purpose

Use this guide when refactoring a repo's startup docs, prompt flow, intake order, or cleanup/archive plan.

Many projects fail here by optimizing for brevity before protecting meaning.

The safer goal is:

- make sure every agent reliably reads the dangerous rules first
- then remove duplication
- then classify or archive historical material

## What Startup Context Is

Startup context is the information a repo wants every agent to know before it changes anything.

That usually includes:

- destructive-action safety
- data/security handling
- environment and artifact-boundary rules
- branch/release posture
- the repo's current operating model

It should not be treated as "as little text as possible."

## Core Rule

Expand mandatory startup context before cleanup, archival, or brevity-driven demotion.

If weighted safety language is important, protect it first.

## Recommended Role Split

### `AGENTS.md`

Always-read hard rules.

Good content here:

- destructive-action rules
- data/security handling
- artifact and workspace safety
- environment-path rules
- delegation boundaries
- branch/release constraints

### Runbook README

Current operating posture and repo navigation.

Good content here:

- startup truth chain
- current workflow/runbook steps
- environment/bootstrap summary
- doc-role explanation
- major navigation points

### Project prompt

Directional brief, not a second rulebook.

Good content here:

- what is already implemented
- what the active lift is now
- what discretionary new work should bias toward
- when the backlog or changelog actually needs to be loaded

### TODO

Active backlog only.

Good content here:

- tracked IDs
- status
- blockers
- dependencies
- remaining scope

### CHANGELOG

Shipped history and validation trace.

Good content here:

- what landed
- when it landed
- what was validated

## What Must Stay Always-Read

These are usually too risky to demote out of startup:

- destructive cleanup and deletion rules
- secrets/data-handling rules
- synced-workspace vs machine-local artifact boundaries
- environment-path-aware DB/runtime policy
- branch/release posture
- handoff and validation expectations

## What Usually Belongs In Load-When-Relevant Docs

- controller maps
- model registries
- large strategy docs
- deep connector/provider inventories
- detailed operational runbooks for one subsystem

## Historical Docs

Do not purge historical docs as a first cleanup step.

Instead:

1. classify them as historical/reference-only
2. keep them discoverable
3. move them later in a dedicated archival pass if needed

## Prompt Rebuild Rule

If a repo has a `PROMPT_PROJECT`-style file, build it from:

- recent implemented baseline from the changelog
- active direction from the backlog
- a small set of current strategy docs

Do not make it a duplicate of `AGENTS.md`.

## Practical Refactor Sequence

1. Inventory which startup docs carry true hard rules.
2. Protect weighted safety language first.
3. Make the role split between `AGENTS`, `README`, `PROMPT`, `TODO`, and `CHANGELOG` explicit.
4. Rebuild the project prompt so it carries direction, not duplicated policy.
5. Remove duplicate summaries from secondary docs.
6. Classify historical docs before moving them.
7. Only then consider shrinking the startup packet.

## Failure Modes To Avoid

- shortening dangerous rules until they lose their force
- moving safety into optional docs just to save tokens
- making the project prompt a second hard-rules document
- requiring the backlog to understand repo identity
- deleting historical markers before they are classified
- repeating the same rule in every startup doc
