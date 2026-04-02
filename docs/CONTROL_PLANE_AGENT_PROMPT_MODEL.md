# Control-Plane Agent Prompt Model

## Purpose

Some repos should not just answer questions or hold strategy notes.

They should also act as control-plane context for downstream agents working in narrower owner repos or module slices.

This model exists for that pattern.

## Core rule

When a repo is functioning as a control plane for a parent platform, the control-plane agent should produce the instruction prompt for the narrower worker agent whenever the user is steering module-specific work.

That reduces the chance that the user has to carry all the missing platform context manually.

## When to use this model

Use this model when:

- a control-plane repo is materially more informed than the user prompt alone
- the target work touches module boundaries, cross-repo contracts, shared business objects, or migration staging
- the user is likely to run narrower follow-on agents against specific repo/module sets
- the wrong missing context would create repeated patching, weak schema choices, or mixed source-of-truth behavior

## Context-budget tiers

### Bounded maintenance exception

Small clerical work may use a smaller context pack when all of the following are true:

- the change is local-only
- no owner-boundary or shared-object meaning is changing
- no significant schema/API/workflow implication exists

Examples:

- broken links
- changelog cleanup
- minor navigation fixes

### Serious control-plane work

For meaningful control-plane work, treat:

- `25k` as the minimum starter context
- `50k` as a healthy target when the work touches platform direction, migration, ownership, workflow/access boundaries, or cross-repo implementation guidance

The point is not "always read more."

The point is that the control-plane repo should engage the whole picture before it generates narrower worker prompts.

## Required prompt packet

A control-plane-generated worker prompt should include:

### 1. Objective

- the real business/module goal
- the current failure mode or drift
- the desired outcome

### 2. Source-of-truth framing

- owner repo or owner lane
- allowed local canonical tables or prefixes
- producer/consumer relationship
- degraded/discovery constraints if applicable

### 3. Required startup set

- the exact must-read docs/code/contracts for that worker
- what is optional
- what should not be treated as authority

### 4. Impact surface

- local surfaces affected
- sibling repos or modules affected
- scripts/bootstrap/validation impact
- external system or connector impact

### 5. Acceptance contract

- what must be true when done
- what should visibly improve for operators
- what must remain stable

### 6. Prohibited shortcuts

- hidden fallback
- UI-only compensation for missing meaning
- payload-blob-only meaning
- ambiguous source-of-truth
- any repo-local optimization that weakens the parent platform contract

## Prompt shape

The control-plane prompt should prefer:

- a short executive summary
- the exact docs/routes/tables/contracts to inspect
- a clear ownership boundary
- a clear main problem statement
- a concrete acceptance section

It should avoid:

- giant history dumps
- vague "look around and figure it out"
- assuming the user description carries enough system context

## Relationship to progress-over-patching

This model is an execution aid for:

- `PROGRESS_OVER_PATCHING_MODEL.md`
- `CROSS_REPO_CONTRACT_MODEL.md`

If those rules say a broader impact sweep is needed, the control-plane prompt should make that broader scope explicit before a worker starts implementing.
