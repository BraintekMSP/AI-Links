# Module Agent Prompt Template

## Objective

- module or repo:
- business outcome:
- current failure or drift:
- desired result:

## Source Of Truth

- owner repo or owner lane:
- local canonical lane/prefix:
- external or upstream source lanes:
- degraded/discovery rules:

## Required Startup Set

Always read:

- `...`
- `...`

Read if directly relevant:

- `...`
- `...`

Do not treat as authority:

- `...`

## Impact Surface

- local routes/surfaces:
- local tables/schema families:
- APIs/bundles/contracts:
- scripts/bootstrap/helpers:
- tests/acceptance paths:
- sibling repos/modules affected:
- external systems/connectors affected:

## Acceptance Contract

- operators should see:
- source-of-truth should be:
- degraded/discovery state should be:
- must remain stable:

## Prohibited Shortcuts

- no hidden fallback as the real operator story
- no payload-blob-only meaning when local schema should be widened
- no UI-only compensation for missing canonical semantics
- no repo-local fix that weakens sibling producer/consumer contracts

## Delivery Notes

- prefer broad, durable solution slices over tiny symptom patches
- widen local schema when business meaning is missing
- write code so likely impact is readable from the code itself
- call out assumptions, risks, and unverified surfaces explicitly
