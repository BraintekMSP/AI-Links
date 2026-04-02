# Cross-Repo Contract Model

## Purpose

This document defines the minimum contract types that should exist when one repo works with another repo without becoming part of it.

## Core rule

Two repos can collaborate without sharing owner-lane truth.

That requires explicit contracts, not inferred coupling.

## Minimum contract types

### 1. Ownership contract
- what this repo owns
- what the neighbor repo owns
- what neither side should write directly

### 2. Identity and mapping contract
- stable IDs
- source-system IDs
- mapping rules
- fail-closed behavior when mapping is unresolved

### 3. Communication contract
- API
- webhook
- workflow trigger
- event
- exported read model

### 4. Error and health contract
- stable error envelope
- stable health surface
- clear degraded/blocking states

### 5. Validation contract
- smoke checks
- contract tests
- rollback or replay expectations

### 6. Impact-surface contract
- upstream producers that create or mutate the object
- downstream consumers that read or depend on the object
- workflow or orchestration repos that route the work
- edge or intake tools that can still create records outside the main app flow
- what must be revalidated when the local schema, mapping, or surface changes

## Required cross-repo interpretation

- Do not scope a change only to the repo being edited when the business object clearly crosses repo boundaries.
- Explicitly identify the neighboring repos and surfaces that will feel the change.
- If a repo is not being edited, still record it as an affected producer, consumer, owner, or orchestrator.
- Cross-repo work is not complete when the local repo compiles but sibling repos would now receive ambiguous IDs, weaker payloads, or semantically incomplete bundles.

## Preferred communication order

1. explicit owner API
2. explicit webhook or event contract
3. explicit workflow trigger
4. explicit exported read model

Avoid:

- direct hidden database writes
- machine-specific path coupling
- sibling binary references as the only communication story
- manual scripts as the only durable contract
