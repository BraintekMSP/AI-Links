# Startup Context Budget Model

## Purpose

This document defines a reusable algorithm for:

- predicting a healthy startup-token budget for a repo
- estimating complication density
- deciding whether documentation is underbuilt, overgrown, or simply carrying the wrong material

This model is meant to help with repo triage and startup-doc refactors.

It is not a precision science. It is a structured decision aid.

## Core idea

Startup tokens are useful for more than cost or compactness.

The real value is:

- understanding how much context a repo genuinely needs every run
- spotting when a repo is under-documented for its consequence level
- spotting when a repo is overgrown but still not clear
- deciding whether cleanup should focus on expansion, reconciliation, or demotion

In other words:

- complexity tells us how much context the repo probably needs
- complication tells us how much extra friction the repo has accumulated
- documentation coverage tells us whether the startup spine is carrying its weight

## Three scores

### 1. Complexity score

Rate each from `0` to `5`.

1. Domain breadth
- `0`: one narrow feature lane
- `5`: many distinct business or platform domains

2. External integration depth
- `0`: no external systems
- `5`: many external systems or one very deep integration surface

3. Runtime/deployment topology
- `0`: one runtime mode
- `5`: multiple local/shared/cloud or host-mode paths with meaningful behavior differences

4. State and data-lane count
- `0`: one obvious store/lane
- `5`: several state lanes, DB lanes, caches, or ownership boundaries

5. Ownership-boundary count
- `0`: one owner, one system
- `5`: multiple owner systems, module boundaries, or cross-repo contracts

6. Consequence of misunderstanding
- `0`: low-risk mistakes, easy recovery
- `5`: misunderstanding can damage operations, trust, data safety, or release posture

7. Legacy and coexistence burden
- `0`: greenfield or very clean
- `5`: significant coexistence, migration, or old/new model overlap

Formula:

`ComplexityScore = sum(all 7 dimensions)`

Range:

- minimum: `0`
- maximum: `35`

### 2. Complication score

Rate each from `0` to `5`.

1. Duplicate guidance drift
- repeated rules across startup docs that are no longer fully aligned

2. Temporary artifact residue
- handoff notes, backup docs, stale working notes, or leftover fallbacks still in live lanes

3. Fallback/helper sprawl
- too many scripts, one-off helpers, or alternate paths for the same job

4. Generated-artifact competition
- generated mirrors, manifests, or summaries competing with source docs for authority

5. History pollution in active docs
- changelog/history content bleeding into README/TODO/prompt surfaces

6. Terminology or role-split drift
- docs disagree about what a file is for, what the repo is for, or which doc owns a topic

Formula:

`ComplicationScore = sum(all 6 dimensions)`

Range:

- minimum: `0`
- maximum: `30`

### 3. Documentation coverage score

Rate each from `0` to `4`.

1. `AGENTS` strength
- always-read rules are clear, forceful, and current

2. Runbook clarity
- README/runbook explains current operating posture and navigation

3. Prompt quality
- project prompt carries direction, not duplicated rules

4. Backlog discipline
- TODO stays active/open and does not try to explain the whole repo

5. History discipline
- changelog/archive/history lanes are distinct and believable

Formula:

`CoverageScore = sum(all 5 dimensions)`

Range:

- minimum: `0`
- maximum: `20`

## Complication density

Complication density tells us how much friction the repo has accumulated relative to its genuine complexity.

Formula:

`ComplicationDensity = ComplicationScore / max(ComplexityScore, 1)`

Interpretation:

- `< 0.35`: low complication relative to complexity
- `0.35 - 0.55`: normal complication band
- `0.56 - 0.75`: docs/process are under strain
- `> 0.75`: strong signal that curation or startup refactor is overdue

Important:

High complexity is not bad.
High complication density usually is.

## Role bonuses

Not every repo with the same complexity needs the same startup budget.

Add one repo-role bonus:

- `0` = single-lane app or tool
- `500` = shared library or framework
- `1000` = operational shell or mixed host
- `2500` = cross-repo control plane / interpretation workspace

## Risk premium

Add one risk premium based on the cost of under-context:

- `0` = under-reading is annoying but cheap
- `500` = under-reading creates meaningful cleanup or review cost
- `1000` = under-reading can cause operationally dangerous or high-remediation mistakes

## Startup token budget formula

Formula:

`PredictedStartupBudget = 1500 + (ComplexityScore * 110) + (ComplicationScore * 70) + RoleBonus + RiskPremium`

Then:

1. round to the nearest `500`
2. treat the result as a target band, not an absolute law

Interpretation:

- if current startup budget is within about `+-1000`, it is probably in a healthy band
- if current budget is much lower, the repo may be under-contexted
- if current budget is much higher, the repo may be overgrown or loading the wrong material

## Documentation adequacy checks

Use these decision rules:

### Likely under-documented

Signal this when any of the following are true:

- `CoverageScore <= 12` and `ComplexityScore >= 20`
- `ComplicationDensity >= 0.60`
- current startup budget is at least `1000` below predicted budget and the repo frequently needs chat repastes or follow-up corrections

### Likely overgrown but not necessarily safe

Signal this when:

- current startup budget is at least `1500` above predicted budget
- and `ComplicationDensity >= 0.50`

This usually means:

- too many docs are loaded
- but they still are not cleanly separated by role

### Likely healthy

Signal this when:

- `CoverageScore >= 15`
- `ComplicationDensity < 0.55`
- and current startup budget is reasonably close to predicted budget

## What the outputs mean

### If complexity is high but complication is low

Do not rush to shrink startup context.

The repo may simply need a richer always-read packet.

### If complexity is moderate but complication density is high

Cleanup and role-split work likely matter more than adding more startup tokens.

### If both complexity and complication are high

Expand startup context first.
Cleanup comes after safety and meaning are protected.

## Worked examples

### Workorders

Illustrative scoring:

- Complexity score: `24`
- Complication score: `18`
- Role bonus: `1000` (`operational shell`)
- Risk premium: `1000`

Budget:

`1500 + (24 * 110) + (18 * 70) + 1000 + 1000 = 7400`

Rounded target:

- about `7500`

Interpretation:

- that supports the current intuition that `Workorders` is safer around `7000+`
- later reduction toward `5000` only makes sense after more lift moves out and complication density drops

Complication density:

`18 / 24 = 0.75`

Meaning:

- this is a high-strain repo
- startup context expansion is justified
- cleanup should focus on curation and demotion of redundant/ephemeral material, not aggressive shrink-first behavior

### TheLinks

Illustrative scoring:

- Complexity score: `30`
- Complication score: `15`
- Role bonus: `2500` (`cross-repo control plane`)
- Risk premium: `1000`

Budget:

`1500 + (30 * 110) + (15 * 70) + 2500 + 1000 = 9350`

Rounded target:

- about `9500`
- operationally this supports a `~10000` target band

Complication density:

`15 / 30 = 0.50`

Meaning:

- TheLinks is complex because it is interpretive and cross-repo
- it still needs curation, but the stronger signal is legitimate context demand rather than sheer clutter

### AI-Links

Illustrative scoring:

- Complexity score: `10`
- Complication score: `4`
- Role bonus: `500` (`framework`)
- Risk premium: `500`

Budget:

`1500 + (10 * 110) + (4 * 70) + 500 + 500 = 3880`

Rounded target:

- about `4000`

Meaning:

- AI-Links should stay smaller than the repos it helps
- but it still needs enough startup context to carry the method cleanly

## Practical use

Use this model when:

- deciding whether to raise startup-token budgets
- deciding whether a repo needs documentation refactor before cleanup
- comparing repos by consequence and curation pressure
- deciding whether a repo’s problem is true complexity or avoidable complication

Do not use it:

- as an excuse to compress dangerous rules
- as a rigid gate that overrides operator judgment
- without considering whether the repo is a product host, shared library, or control-plane workspace

## Recommendation

Treat startup-token budgeting as a repo-health signal, not just a token-efficiency problem.

The best startup budget is the one that gives an agent enough context to avoid expensive misunderstanding, while still making it obvious which docs are truly authoritative.
