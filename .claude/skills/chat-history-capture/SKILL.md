---
name: chat-history-capture
description: Use this skill when the user asks to go through the current chat or pasted chat history and extract repo-specific decisions, direction, open threads, or findings into Anarchy narrative arc records and vision docs. Triggers include "go through this chat", "capture decisions", "add decisions to arc", "chat history capture", "look into {repo} specific decisions", and requests to include message date/time provenance.
---

# Chat History Capture

Extract durable repo-specific direction from the current chat or a pasted transcript into narrative arc and vision surfaces without inventing provenance.

## Non-negotiables

- Treat this as archival capture unless the user explicitly asks for live decision-making. The capture records when decisions were formed, not when the capture is performed.
- Treat versioned Codex cache skill paths as evidence, not authority. Resolve the active harness/source/cache/runtime lane before schema, arc, or gov2gov work.
- Preserve timestamp provenance. The decision `date` field must reflect when the decision was actually made in the chat, not the capture date.
- Today's date or current capture time is not a valid fallback decision date. Capture time can be recorded separately only as capture provenance.
- Do not turn transcript order into exact times. Approximate only when the transcript itself gives enough evidence, and label it approximate.
- Do not substitute repo logs, file modification times, commit times, build times, or implementation evidence for the message timestamp. Those can be related evidence timestamps, never decision timestamps.
- Show each proposed new entry to the user before writing when the user asks for timestamp audit or archival capture.
- Capture decisions and durable direction, not every message.
- Keep repo-specific decisions in that repo. Put Anarchy tool/runtime findings in AI-Links/Anarchy surfaces, not in the consumer repo's product arc unless they affect that repo's operating rule.
- Prefer append/consolidation edits. Do not overwrite an existing arc or vision section without first reading it and preserving still-live meaning.

## Workflow

### 1. Establish authority and target

1. Identify the target repo from the user's prompt or current workspace.
2. Read the target repo startup instructions and vision surfaces before editing.
3. If Anarchy-AI is available, call `preflight_session` for the target repo.
4. Verify the active harness lane when the task involves Anarchy schemas, arcs, or gov2gov:
   - user-profile source version
   - active Codex cache version
   - callable runtime provenance
   - schema bundle version
   - skill path generation, if a skill path is supplied
5. If those disagree, stop schema/gov2gov mutation and report the split. You may still validate existing JSON or make clearly bounded non-schema notes if the user approves.

### 2. Inventory destination surfaces

Look for, in this order:

- `.agents/anarchy-ai/narratives/register.json`
- `.agents/anarchy-ai/narratives/projects/*.json`
- `AGENTS-Vision.md`
- `docs/vision.md`
- repo-specific backlog or TODO docs referenced by the vision docs

If no narrative register exists, use the current schema/template only after confirming the repo is intended to carry Anarchy underlay or narrative surfaces.

### 3. Extract candidates from chat

For each candidate, capture:

- source timestamp as displayed, including date and hour when available
- recovered decision date and the evidence used to recover it
- capture timestamp separately if source timestamp is absent or ambiguous
- related implementation evidence timestamps separately, only when useful
- repo or subsystem affected
- decision/direction/open question/finding classification
- short evidence summary from the chat
- whether the point is confirmed, inferred, or proposed
- destination surface recommendation

Recover the decision date in this order:

1. Visible timestamps on the messages themselves.
2. Session headers, dated file references, or relative markers in the transcript such as "yesterday", "as of X", or "last week".
3. If only time-of-day is visible, anchor it to the session date.
4. If unrecoverable, use the closest verifiable decision/session date and add `(timestamp inferred from <source>)` or `(original decision date unknown; captured YYYY-MM-DD)` in the summary. Never fabricate a precise date you cannot support.

Reject candidates that are:

- generic preference with no repo consequence
- already captured with equal or better specificity
- stale or reversed by later messages
- about Anarchy tooling rather than the target repo, unless the repo's own rule changes

### 4. Choose the landing shape

Use an existing arc when the subject matches and the new material extends the same thread.

Create a new arc when the chat exposes a distinct durable topic that would overload the existing arc.

Update vision docs only for stable product direction. Arc can carry more transient decisions and open threads; vision docs should carry durable north-star or operating doctrine.

For completed gov2gov migrations, consolidate into narrative/reference state. Do not leave root `GOV2GOV-*` active packet files unless the repo is actively in migration mode.

### 5. Author with timestamp discipline

When exact message time is visible:

```json
"date": "2026-04-26T00:00:00-05:00",
"source-timestamp": "2026-04-26 12:34 AM CDT"
```

When only date/hour is visible, record that precision instead of pretending minute-level precision.

When the original decision timestamp is unrecoverable but a session date is verifiable:

```json
"date": "2026-04-25",
"summary": "Decision summary (original decision date unknown; captured 2026-04-26)",
"provenance-note": "Date inferred from session header; exact message timestamp unavailable."
```

When no decision/session date is verifiable, do not silently write today's date into `date`. Present the candidate to the user and ask what schema-compatible date should be used, or record only a non-decision capture note in a schema-valid text field.

If the existing narrative schema does not define these exact fields, place the provenance note in `beat`, `definition-of-fixed`, `handoff-note`, or another schema-valid text field rather than adding invalid structure.

Do not fill `date` from nearby repo session logs when the chat message timestamp is missing. Use only message/session provenance for decision dates. If related implementation logs are relevant, label them as related evidence, not decision provenance.

### 5.5. Present before write

Before editing arc or vision docs, show the user a compact proposed-entry list:

- destination file
- entry type
- proposed decision date
- timestamp evidence
- summary text
- uncertainty note, if any

Do not write until the user approves or corrects the proposed timestamps and content.

### 6. Validate before reporting

- Parse every edited JSON file.
- Check register entries point to existing record files and record IDs match.
- Run `validate_narrative_arc_state` before declaring Arc or narrative edits complete; treat findings as measurement terrain and repair or report them honestly.
- Run `git diff --check` on touched files.
- Review vision diffs for overclaiming; vision should not claim more certainty than the chat supports.
- If Anarchy tools are in use, call `compile_active_work_state` after edits to preserve the stop point.

## Output

Report:

- arcs created or updated
- vision docs updated
- decisions captured, grouped by repo topic
- timestamp precision used: exact, date-hour, session-date-inferred, closest-verifiable-date, or unknown-requires-human-date
- rejected or deferred candidates and why
- validation performed

## Default user prompt

Use this wording when asking another agent to perform the capture:

```text
This is an archival capture of past decisions, not live decision-making.
Provenance accuracy matters: a decision recorded with the wrong date
misattributes when the rationale was formed.

Go through this chat and extract durable {repo}-specific decisions and
direction for capture in the Arc and Vision docs.

For each entry, the date field must reflect when the decision was
actually made in the chat, not when this capture is happening. Today's
date (the capture date) is not a valid fallback for the decision date.

To recover the decision date:
1. Look for visible timestamps on the messages themselves
2. Check session headers, dated file references, or relative markers
   ("yesterday", "as of X", "last week")
3. If only a time-of-day is visible, anchor it to the session date
4. If unrecoverable: use the closest verifiable date (e.g., session
   date) and add "(timestamp inferred from <source>)" or "(original
   decision date unknown; captured YYYY-MM-DD)" in the summary.
   Never fabricate a precise date you cannot support.

Use the Anarchy-AI harness for preflight, JSON validation, and final
state capture. Show me each new entry before writing it so I can
audit timestamps and content.
```
