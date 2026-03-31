# Project Prompt Template

## Purpose
- what this prompt is for in this repo
- what it should carry that is not already in `AGENTS.md`

## Resolved Identity
- project name:
- project slug:
- working folder:
- primary branch:
- version target or current release label:

## Prompt role
- directional brief, current-state brief, or other
- one sentence on what this prompt should not try to replace

## Current implemented baseline
- the important capabilities, boundaries, or foundations that already exist
- prefer facts that can be supported by recent changelog or current runbook truth

## Active direction
- the major active lift now
- what the next meaningful improvement areas are

## Discretionary biases for new work
- when a task leaves room for judgment, what choices should be favored

## Canonical current-state sources
- `AGENTS.md`
- runbook README
- exact change-gate or contract docs if relevant
- other current-state docs actually used

## When to load TODO
- what kinds of work require the backlog
- what should not depend on loading the full backlog

## When to load CHANGELOG
- what kinds of work require shipped history
- what should not depend on loading the full changelog

## Strategy sources
- list the deeper docs actually ingested and used
- if the user explicitly required full-repo ingest, record that the entire repo was ingested instead of naming only a subset

## Constraints not already owned by `AGENTS.md`
- deployment constraints
- environment constraints
- operator or audience constraints
- data-handling constraints that are current-state rather than permanent repo policy

## Original prompt note
- if replacing an older prompt, preserve one short note explaining what changed in role or emphasis

## Working agreements
1. keep hard rules in `AGENTS.md`
2. keep the runbook in README
3. keep active backlog state in TODO
4. keep shipped history in CHANGELOG
5. use this prompt for implemented baseline plus directional judgment
6. keep changes scoped and reversible
