# Repo 2.5 Readiness Model

## Purpose

This document defines a reusable maturity model for repos that want disciplined AI-assisted delivery.

`2.5` here means:

- local-first by default
- explicit safety and artifact boundaries
- measurable run contracts
- clear ownership and dependency boundaries
- reversible cloud validation instead of casual always-on hosting

## Phases

### `P0` - Observed

The repo exists and has been identified.

Minimum evidence:
- repo purpose is roughly known
- branch or stack is roughly known

### `P1` - Documented

The repo explains itself clearly.

Minimum evidence:
- root `README` or equivalent
- startup truth chain
- owner intent or role
- repo-local guardrails if AI-assisted work is expected

### `P2` - Runnable

The repo can be built and checked honestly.

Minimum evidence:
- deterministic `build`
- deterministic `start`
- deterministic `test`
- deterministic `health`
- explicit dependency and artifact-root guidance

### `P3` - Bounded

The repo has explicit role and boundary rules.

Minimum evidence:
- workload class
- write-owner rules
- dependency visibility
- rollback, quarantine, or recovery direction

### `P4` - 2.5 Ready

The repo is ready for disciplined AI-assisted local-first delivery.

Minimum evidence:
- passed `P0` through `P3`
- local-first operating profile
- measurable validation pack
- explicit promotion-to-cloud-validation rules where relevant

## Workload classes

- `static`
- `function`
- `job`
- `bursty_api`
- `warm_shell`
- `desktop_edge`
- `contract_library`
- `template_asset`

## Practical rule

If a repo cannot answer these five questions, it is not `2.5-ready`:

1. What is this repo for?
2. How do I build, start, test, and check it?
3. Where do volatile artifacts go?
4. What does it own?
5. What is the rollback or quarantine path?
