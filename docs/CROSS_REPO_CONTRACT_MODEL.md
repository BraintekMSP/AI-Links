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
