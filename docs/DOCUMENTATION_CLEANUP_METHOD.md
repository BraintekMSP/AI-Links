# Documentation Cleanup Method

## Purpose

This document defines a reusable cleanup method for repos whose docs have become too layered, stale, or ambiguous.

## Goal

Reduce the number of documents a contributor must trust first.

## Standard sequence

1. Identify the startup spine:
- `AGENTS`
- project prompt
- runbook README
- active TODO
- changelog

2. Classify docs by role:
- `canonical`
- `supporting`
- `operational`
- `supplemental`
- `historical`

3. Find duplicate or drifting topics:
- startup instructions
- current-state behavior
- branch and version rules
- ownership boundaries
- strategy decisions

4. Preserve code truth:
- docs should link to code, scripts, and schemas instead of trying to out-describe them

5. Fix navigation before deleting detail:
- people should still be able to find the truth path during cleanup

6. Demote or merge duplicates:
- keep one owner doc per topic

## Practical lesson

Large READMEs tend to turn into mini-changelogs.
Once that happens, contributors trust the wrong document.
