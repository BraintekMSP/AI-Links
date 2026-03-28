# Subagent Safety Model

## Purpose

This document defines the default safety posture for subagents in AI-assisted software work.

## Default stance

- subagents are off by default
- use them only after the repo is ingested and the task is bounded

## Allowed default roles

### Read-only explorer
- scans files
- answers scoped codebase questions
- returns file paths, lines, IDs, and short findings

### Scoped worker
- edits only an exact owned file set
- does not infer broad repo authority

## Disallowed by default

- destructive shell operations
- wildcard cleanup
- repo-wide file moves
- broad tool installs
- runtime patching as a permanent fix
- repo-wide document rewrites
- hidden environment mutation

## Parent agent responsibilities

- define exact scope
- define exact write ownership
- pass the relevant rules
- review the output
- own final merge judgment and validation
