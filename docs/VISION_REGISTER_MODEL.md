# Vision Register Model

## Purpose

This document defines the intended shape of a durable vision register for `AI-Links`.

The goal is not to turn every long human message into a vision artifact.
The goal is to preserve durable user intent in a structured way when that intent is:

- forward-shaping
- cross-surface
- implementation-relevant
- likely to matter after the current exchange ends

This is distinct from:

- scratchpad theory
- transient implementation notes
- changelog history
- one-off prompts

## Why A Vision Register Exists

The repo already preserves:

- vision artifacts
- implementation gaps
- architecture direction

What is still missing is a bounded register shape that lets the harness answer:

- what the human actually asked for
- what wording mattered
- what context qualified that ask
- what implementation surfaces were implicated
- how much of the ask is actually implemented
- what still detracts from delivery quality

Without that register, vision capture remains too ad hoc.

## Minimum Register Properties

Each vision-register entry should support at least:

- `vision_request`
- `human_quote`
- `qualifying_context`
- `surfaces_affected`
- `implementation_assessed_at_percent`
- `implementation_grade_detractor_count`

## Field Intent

### `vision_request`

The normalized request or durable intent.

This is not meant to overwrite the human's exact language.
It is the bounded operational statement of what the vision entry is about.

### `human_quote`

The exact source wording from the human that materially shaped the vision.

This is important because normalized summaries compress meaning.
The quote preserves the original request where nuance matters.

### `qualifying_context`

Why the entry counted as vision and not just local chat.

This should preserve things like:

- whether the user was defining product behavior
- whether the user was constraining delivery semantics
- whether the user was naming architectural direction
- whether the user was reacting against an existing bad implementation

### `surfaces_affected`

The implementation or delivery surfaces likely touched by the vision.

Examples:

- setup executable
- bootstrap lane
- MCP runtime
- plugin manifest
- skills
- docs
- schemas
- installer GUI

### `implementation_assessed_at_percent`

A bounded estimate of how much of the vision is actually realized.

This should remain clearly heuristic rather than pretending to be precision proof.

### `implementation_grade_detractor_count`

A count of known issues materially detracting from the quality of the delivered vision.

This is not the same as raw todo count.
It is meant to preserve how many concrete delivery flaws still undermine the vision.

## Future Harness Controls

The harness should eventually support explicit vision-register controls.

Candidate controls:

- `qualify_as_vision`
- `capture_vision`
- `detract_from_vision_id_with_note`

These should be treated as bounded helper surfaces, not as freeform narration.

## Qualification Rules

Vision qualification should not depend on long messages alone.

Long human prompts may be a useful detection cue, but they are not sufficient.
Length is only a heuristic.

More reliable qualification signals are:

- explicit product-direction language
- explicit delivery constraints
- cross-turn or cross-surface relevance
- language defining what should remain true later
- language rejecting a bad implementation frame
- wording that should survive a later rebuild or refactor

## Capture Rules

`capture_vision` should preserve both:

- normalized register structure
- exact quoted human source

It should not silently replace one with the other.

The capture lane should remain conservative.
It is better to miss a weak candidate than to pollute the register with transient chat.

## Detractor Rules

`detract_from_vision_id_with_note` should exist because implementation quality is rarely binary.

A vision item may be:

- partly implemented
- locally implemented but poorly delivered
- structurally correct but behaviorally weak
- present in docs but absent in runtime

The detractor lane should preserve that without rewriting the original vision request.

## Relationship To Existing Docs

The intended split is:

- vision artifacts preserve durable high-level human language
- a future vision register preserves structured traceability across those artifacts
- implementation gaps preserve what still prevents delivery quality
- changelog preserves what was actually changed

## Current Status

This is a model and implementation-gap direction, not a delivered harness lane.

What is not yet delivered:

- a real register file
- stable `vision_id` issuance
- qualification and capture controls
- detractor controls
- automatic or semi-automatic vision qualification from user prompts

## Practical Constraint

If the harness later watches for candidate vision prompts, it should do so boundedly.

That means:

- no indiscriminate capture of long prompts
- no assuming every complaint is durable vision
- no replacing direct human review with aggressive auto-registration

The correct direction is:

- qualify carefully
- capture explicitly
- preserve the quote
- track implementation and detractors separately
