# AGENTS-schema-gov2gov-migration.json

## The Problem

Migrating from one governance system to another is not the same as setting up governance for the first time. When a workspace already has a governed or semi-governed operating model, the migration risk is not missing file creation — it is semantic drift, shadow governance, and parallel authority.

The failure modes are specific and recurring:

- An agent declares the migration complete when the new file family exists, but the load-bearing governance meaning still lives in the old docs.
- Adjacent support docs get classified and budgeted, but their prose is never trimmed after extraction — creating duplicate authority that looks clean in a registry but confuses every future agent.
- The ancillary governance ring passes measurement on day one, then grows back into shadow governance because no one monitors it post-migration.
- Versioned lane directories carry first-copy governance meaning that was never promoted to the governed core, because the extraction phase only checked the root docs.
- Work pauses mid-migration with no stop-state, and the next agent starts from scratch because nothing records where the work left off or what remains.

These failures happened in practice. This schema was written from the lessons learned, not from theory.

## What This Schema Does

AGENTS-schema-gov2gov-migration.json is a migration contract for moving between governance systems. It produces its own artifact family (GOV2GOV-hello.md, GOV2GOV-source-target-map.md, GOV2GOV-registry.json, GOV2GOV-rules.md, GOV2GOV-pitfalls.md) that lives alongside the target governance files during migration.

It defines:

- **Schema modes**: Active mode (GOV2GOV-* files materialized, migration in progress) and reference mode (schema present for future use, no artifact family, expected state after migration completes).
- **Distribution and realtime copies**: Schemas may exist in two places — a distribution copy (portable, publication-ready, authoritative for schema structure) and a realtime copy (deployed in a workspace, accumulates project-specific lessons). Distribution → realtime for schema updates. Realtime → distribution only when a lesson is validated, generalized, and human-confirmed as universally applicable. Undeclared divergence between copies is a parallel authority surface.
- **Parallel authority surface classification**: Every file in the workspace gets classified as source-core, source-ancillary, target-core, target-ancillary, historical-preserved, or bridge-support. Nothing remains unclassified.
- **Six migration phases**: source-ingest, contract-diff, semantic-extraction, ancillary-reconciliation, measurement-rewrite, completion-audit. Each has entry and exit criteria.
- **Semantic extraction with mandatory deduplication**: An extraction is incomplete while the original source wording still exists. Leaving the source intact after extracting into the target core creates parallel authority.
- **Completion audit with dual check**: Classification completeness (every file registered and dispositioned) AND semantic compression completeness (every file trimmed to minimum after extraction). Passing one without the other is not passing the audit.
- **Required stop-state**: If work pauses, migration-objective-at-pause, paused-at-migration-phase, remaining-work, blocker, first-unknown-encountered, next-automatic-action, and divergence-question-for-human must all be populated. No silent pauses.
- **Divergence triggers**: Conditions where the human must be involved — source and target law conflict, mandatory reads cannot be shrunk without losing meaning, measurement must change unexpectedly, ancillary ring exceeds budget, or a surface has no clear target-home.
- **Post-migration handoff to steady-state**: The ancillary ring becomes permanent and measured on every governance session. Versioned lane directories that survived the migration must be declared in steady-state governance or they become unclassified drift surfaces. The steady-state measurement script must test for semantic compression — stale current-state language, dated snapshots presented as live truth, and untrimmed duplicate prose are measurement failures, not just migration leftovers. If the measurement doesn't test for this, the completion-audit passed but the handoff is incomplete.

## When to Use It

- The source workspace already has a governed or semi-governed operating model
- The target workspace also has a governance model (not a greenfield AGENTS family)
- The migration risk is semantic drift, shadow governance, or parallel authority

## When NOT to Use It

- The source workspace is monolithic and the target is the first governance model — use AGENTS-schema-governance.json migration mode instead
- The work is a bounded single objective — use AGENTS-schema-1project.json
- The task is steady-state governance maintenance with no competing governance model — use AGENTS-schema-governance.json

## How the Solution Manifests

An agent receives this schema and the target governance schema. It:

1. Reads the source governance system completely before planning changes
2. Runs the review-before-authoring-prompt and presents findings to the human
3. Authors GOV2GOV-hello.md with source/target pairing, measurement, and stop-state
4. Authors GOV2GOV-source-target-map.md mapping every authority surface
5. Builds GOV2GOV-registry.json classifying every file with disposition, progress, and target-home
6. Writes GOV2GOV-rules.md with phase gates, divergence triggers, and completion rules
7. Executes semantic extraction — moving meaning to target core and trimming source in the same operation
8. Reconciles the ancillary ring — sets budgets and authority anchors
9. Rewrites measurement so the target system can prove compliance
10. Runs completion-audit — dual check: classification AND semantic compression

After completion, the GOV2GOV-* files are archived or removed, and the schema transitions to reference mode.

## Relationship to Siblings

- **Governance** is the target system this schema migrates into. After migration, governance assumes full authority.
- **1project** may trigger a need for gov2gov if a project graduates to governance and the existing workspace already has a governance model that must be reconciled.
- **Narrative** is independent — narrative records may reference the migration through known-decisions.project-ref but the migration itself is not a narrative.
- **Sibling isolation**: The GOV2GOV-* artifact family coexists with the target AGENTS-* family during migration. Sibling-isolation rules prevent cross-contamination.

## Core Lessons Encoded

These came from real migration failures, not from design theory:

1. Structural migration is not semantic migration
2. Measured doc placement is not proof that load-bearing truth has been extracted
3. Adjacent support docs need explicit budgets or they regrow into shadow governance
4. A migration is not complete while parallel authority surfaces remain unclassified
5. Stop-state output is part of the contract, not a courtesy
6. Versioned lane docs are high-risk hiding places for first-copy governance meaning
7. The ancillary ring needs ongoing monitoring after migration
8. An extraction is incomplete while the original source wording still exists
9. Classification completeness is not semantic compression completeness
10. The steady-state measurement script must test for semantic compression after migration or stale docs will survive the handoff

## Key Design Decisions

- **Self-documenting field names**: Registry entries use `parallel-authority-surface-class`, `migration-progress-for-this-surface`, and `source-file-path` rather than bare `class`, `state`, and `path`. An agent reading the field name knows what goes there without reading the role description.
- **Schema-level rules vs. instance rules**: Universal migration rules live at the schema level. Instance-specific rules live in GOV2GOV-rules.md. Schema wins on conflict.
- **Ancillary, not auxiliary**: Ancillary means "on the outskirts, not core, but still critical to the system." Auxiliary means purely supplemental. The schema uses ancillary because these docs are load-bearing support, not optional extras.
- **Active vs. reference mode**: The schema explicitly declares both modes so agents don't try to instantiate migration artifacts in a workspace that already completed its migration.
- **Distribution copy is authoritative**: The distribution copy (e.g. AI-Links) owns schema definitions and naming conventions. The realtime copy (e.g. TheLinks) inherits structure and adds workspace-specific content. This prevents project-specific semantics from invading the distribution plane through an overeager agent.
- **Context compression is not trustworthy**: Durable state must be written to files, not held in context. Acceptable compression now does not guarantee fidelity later.
- **Bug reports and feature requests are not governed here**: Same boundary as siblings.
