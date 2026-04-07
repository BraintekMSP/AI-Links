# AGENTS-schema-governance.json

## The Problem

Code projects accumulate rules, vocabulary, ownership boundaries, and behavioral expectations over time. These live in people's heads, in scattered docs, in commit messages, and in "the way things are done here." When a new agent or team member enters the project, they reconstruct this context from whatever they can find — and they fill gaps with assumptions.

Those assumptions are where drift starts. A term used one way in one scope gets used differently in another. A rule that was never written down gets violated by someone who had no way to know it existed. An architectural boundary that was clear to the original team becomes invisible to the third agent to touch the codebase.

Governance schemas exist because code projects are too complex for any single session, any single agent, or any single person to hold the full picture reliably.

## What This Schema Does

AGENTS-schema-governance.json is the full governance schema for the AGENTS-schema-* family. It defines:

- **Files**: A structured set of markdown files (AGENTS.md, AGENTS-hello.md, AGENTS-Terms.md, AGENTS-Vision.md, AGENTS-Rules.md, AGENTS-Pitfalls.md) that together form a complete operating picture for a scope.
- **Scope hierarchy**: Platform > repo > module, with inheritance. Terms defined at a parent scope flow down. Rules at a child scope cannot contradict the parent.
- **agent-session-open-close-measurement**: Command-based measurement at session open and close, with baseline recording, diff output, and discrepancy blocking. Requires human-configured measurement commands — unconfigured commands render the tier system, auto-triggers, and measurement-discrepancy-count signal inert.
- **Complexity monitoring**: Signals (term-count, rule-count, deliverable-divergence, measurement-discrepancy-count, cross-ref-count) tracked with observed-good-patterns and observed-bad-patterns. Qualitative soft-metrics evaluate signal behavior and shift agent posture.
- **Auto-triggers**: A self-healing mechanism that raises the complexity tier when real failures occur — not when someone guesses that things are getting complex.
- **Edit modes**: Agents must declare intent (add, revise, remove, or none) before closing a governance session. `add`, `revise`, and `remove` are blocked-until human-confirms; `none` is only valid when no governance-surface files changed.
- **Docs-repo migration completeness**: For documentation/control workspaces, migration is not complete when the AGENTS family exists but the wider conflict surface still speaks the old monolithic model. Active docs must become family-aware and historical docs must be labeled and redirected.
- **Architecture governance**: For app-repo and module scopes, an ownership model that declares who owns truth, who constructs from it, who consumes it, and who may mutate it. Has its own drift-signals and observed-patterns.
- **Conflation disambiguation**: A map for terms that carry multiple meanings, making the agent's statistical default explicit so the workspace override lands cleanly.
- **DECISION-Record.md**: A reconciliation artifact — read when a discrepancy is found between docs or between docs and schema. Not an audit trail. Not a mandatory session read.

## When to Use It

- Multiple agents or team members need to coordinate on the same codebase
- Rules exist that need to be enforced across sessions
- Ownership boundaries matter (who owns this data, who can modify it)
- The project has grown beyond a single goal into distinct deliverables
- A 1project schema has hit its exit conditions (pitfall rediscovery, goal drift, rules emerging)

## When NOT to Use It

- The work has a single clear goal and one expected output — use AGENTS-schema-1project.json
- The subject is communication, relationships, or tribal knowledge — use AGENTS-schema-narrative.json
- The project has not yet accumulated enough complexity to warrant governance overhead

## How the Solution Manifests

An agent receives this schema and authors AGENTS files in load order (0 through 5), with human confirmation at each step. The result is a workspace where:

1. Any new agent can read AGENTS.md and immediately know what files to load
2. Terms are defined once and inherited — no re-explanation needed
3. Rules reference only defined terms — no ambiguous constraints
4. Vision is explicit — what "done" looks like is written down, not assumed
5. Failures are recorded with enough structure to prevent rediscovery
6. Complexity is tracked and the agent's behavioral posture adjusts automatically
7. Architecture ownership is declared — "who owns this truth" has one answer

The files are flat labeled key-value markdown — human-readable, human-editable, no tooling required.

For docs-repo migrations, the AGENTS family is only the first half of the job. The second half is the conflict surface:

- classify old-reference docs as active-control, active-bridge, or historical-supporting
- update active docs so they stop treating `AGENTS.md` as the whole authority
- keep historical docs useful, but explicitly label and redirect them
- wire session measurement so drift back to the old model fails loudly
- require edit-mode-aware close measurement so governance changes cannot be called complete under `none`

## Relationship to Siblings

- **1project** exits to governance when rules, ownership, or multiple scopes emerge. A 12-step migration-protocol in the 1project schema maps fields between the two schemas.
- **Narrative** references governance scopes when a narrative thread spawns concrete work (via known-decisions.project-ref). Governance scopes can reference narrative records via the narrative-ref field on AGENTS-hello.md.
- **Sibling isolation**: If another AGENTS-schema-*.json is present in the context window, do not merge, inherit, or cross-reference fields unless an explicit pointer field (narrative-ref, project-ref) declares the relationship.
- Governance does not exit to anything — it is the destination schema.

## Key Design Decisions

- **Declarative language only**: No metaphor, no poetic framing. Agents evaluate language in a single pass. Ambiguous language resolves to the most statistically common interpretation, not the intended one.
- **Human-confirms gates everywhere**: The schema does not trust agents to self-approve scope changes, rule changes, or architecture decisions.
- **agent-session-open-close-measurement is command-based**: Not a narrative summary — a command that produces output that can be diffed. If no commands are configured, the measurement system is inert and the agent must ask the human rather than guessing.
- **Docs-repo migration requires measurement, not just file creation**: If the conflict surface is not measured, a repo can look migrated while still operating on the old startup model.
- **Observed patterns over numeric thresholds**: Bad patterns are tracked as narrative descriptions with optional numeric qualifiers, not hard floor values.
- **Context compression is not trustworthy**: Acceptable compression in the current context window does not guarantee fidelity in future context. Durable state must be written to files, not held in context.
- **Bug reports and feature requests are not governed here**: The schema explicitly declares that human-reported issues belong to whatever external system the project uses.
- **DECISION-Record.md is for reconciliation, not audit**: Read when an agent encounters a discrepancy between docs or between docs and the schema. Written when a structural change creates a gap that would otherwise look like an error to the next agent. Never a mandatory session read.
