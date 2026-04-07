# AGENTS Schema Family Comparison Matrix

This matrix compares the three sibling schemas across structural, behavioral, and boundary dimensions. It is designed to surface gaps, overlaps, and assumption failures before any schema is deployed for data collection.

---

## Identity

| Dimension | Governance | 1project | Narrative |
|---|---|---|---|
| File | AGENTS-schema-governance.json | AGENTS-schema-1project.json | AGENTS-schema-narrative.json |
| Version | 1.0.0 | 0.1.0 | 0.1.0 |
| One-line role | Multi-scope governance for code and operations | Single-goal session continuity | Evolving narrative records for entities with multiple parties |
| Primary subject | Code, operations, agent behavior | A focused project with one deliverable | Communication, relationships, tribal knowledge |
| Output format | Human-editable flat markdown (AGENTS-*.md) | Human-editable flat markdown (AGENTS-*.md) | JSON data artifacts (not human-editable) |
| Who writes the output | Agent, human reviews and confirms | Agent, human reviews and confirms | Agent exclusively, human provides input |

---

## Structural Features

| Feature | Governance | 1project | Narrative |
|---|---|---|---|
| Entry point file (AGENTS.md) | Yes, required | Yes, required | No -- uses register instead |
| Session state tracking | agent-session-open-close-measurement -- command-based with baseline, diff, blocked-on-discrepancy | project-state-when-work-starts-and-stops -- string-based open/close | record-state-at-review-open-and-close -- string-based open/close |
| Scope hierarchy | platform > repo > module with inheritance | None -- single scope, no parent | None -- entity types are independent, not hierarchical |
| File inheritance | Yes -- Terms, Pitfalls, Rules inherit from parent scope | No | No |
| Terms | Required (AGENTS-Terms.md) | Optional (AGENTS-Terms.md) | Embedded in entry narrative (no separate file) |
| Rules | Required (AGENTS-Rules.md) | None | None -- edit-modes and compression-rules govern behavior |
| Vision | Required (AGENTS-Vision.md) | None -- goal field replaces vision | None -- subject field replaces vision |
| Pitfalls | Optional (AGENTS-Pitfalls.md) with tier-history | Optional (AGENTS-Pitfalls.md) | None as separate concept -- observed-bad-patterns and entries serve this function |
| Conflations | Optional disambiguation map | None | None |
| Architecture block | Optional for app-repo and module scopes | None | None |
| Templates | Yes -- per file, flat markdown encoding | Yes -- per file, flat markdown encoding | Yes -- per record type, JSON encoding |

---

## Governance Mechanisms

| Mechanism | Governance | 1project | Narrative |
|---|---|---|---|
| blocked-until gates | human-confirms, verification-passes, parent-valid | human-confirms only | human-confirms (compress mode only) |
| Complexity monitoring | signals + observed-patterns + soft-metrics + auto-triggers | None | criticality ranking + compression rules |
| Tier system | low/medium/high with operational-modes per tier | None | None |
| Auto-triggers (self-healing) | Yes -- intra-attempt, intra-session, cross-session | None | None |
| Module extraction | MODULE-Boundary.md when soft-metrics evaluate high | None | None |
| Decision records | DECISION-Record.md as reconciliation artifact -- read when discrepancy found between docs or between docs and schema -- not an audit trail | None | known-decisions block with attribution and reversal tracking |
| Edit mode governance | Explicit: add, revise, remove -- all blocked-until human-confirms | Not explicitly declared | Explicit: append, update, compress, correct |
| Compression governance | None | None | Yes -- never-drop list, lossy-artifact-warning, quarterly-only, human-confirms |

---

## Signal Detection

| Signal capability | Governance | 1project | Narrative |
|---|---|---|---|
| Complexity signals | term-count, rule-count, deliverable-divergence, measurement-discrepancy-count, cross-ref-count | None | None |
| Architecture drift signals | duplicate-helper-count, oversized-file-count, route-drift-count, consumer-rederivation-count, unclassed-object-count | None | None |
| Signal cue classes | None | None | 8 classes with false-positive-tracking: context-truncation, cross-domain-reference, attribution-gap, implicit-history, ownership-ambiguity, stale-open-thread, definition-divergence, tribal-flag |
| Capture workflows | None | None | 6 workflows: technician-mailbox, client-email-thread, ticket-update, vendor-exchange, internal-handoff, review-session |
| Pattern tracking | observed-good-patterns, observed-bad-patterns (with signal mapping and count) | freeform-patterns: working/not-working string lists (lightweight, no signal mapping -- 3-session repeat is exit-to-governance signal) | observed-patterns good/bad (with tribal flag) |

---

## Human-Facing Features

| Feature | Governance | 1project | Narrative |
|---|---|---|---|
| Cast / attribution | parties, end-user, deliverable-recipients on scope | None | Per-entry: owner, context-holder, requested-by, finalized-by |
| Definition-of-fixed | None | None | Yes -- fixed-per-client, fixed-per-tech, definitions-match, matches-original-request, divergence-narrative |
| Sentiment tracking | None | None | sentiment-and-tension per entry |
| Narrative beats | None | None | Yes -- direction shifts tracked as entry type |
| Tribal knowledge flag | None | None | Yes -- tribal: true on entries and patterns, highest criticality |
| Handoff mechanism | incomplete-session field pointing to HANDOFF.md | incomplete-session field pointing to HANDOFF.md | handoff-note field required on thread closure or ownership transfer |
| Cadence | None declared | None declared | monthly (iterative), quarterly (full review + compress), transient (one-off) |

---

## Cross-Schema Connections

| Connection | From | To | Mechanism |
|---|---|---|---|
| Exit to governance | 1project | Governance | exit-to-governance conditions + 12-step migration-protocol |
| Narrative tracks project | Narrative | 1project or Governance | known-decisions.project-ref points to sibling record |
| Sibling acknowledges narrative | Governance or 1project | Narrative | narrative-ref field on AGENTS-hello.md |
| Sibling isolation | All | All | sibling-isolation declaration prevents cross-contamination when multiple schemas in context |
| Shared boundary | All | External | human-reported-issue-governance -- FR/BR owned by external system |
| Shared invariant | All | All | context-ordering-rule -- identical verbatim in all three |

---

## Safeguards Added This Session

| Safeguard | Schema | What it addresses |
|---|---|---|
| sibling-isolation | All three | Prevents cross-contamination when multiple schemas are in the same context window |
| agent-session-open-close-measurement setup-note | Governance | Makes explicit that unconfigured measurement commands render the tier system, auto-triggers, and measurement-discrepancy-count inert |
| false-positive-tracking | Narrative | Acknowledges signal cues will misfire; routes false positives to review patterns; reduces trust rather than disabling cues |
| migration-protocol | 1project | 12-step protocol for migrating 1project files to governance structure when exit conditions fire |
| lossy-artifact-warning | Narrative | Compression is explicitly lossy; detail not promoted to durable sections during authoring is accepted as lost |
| DECISION-Record.md rewrite | Governance | Reconciliation artifact read when confusion arises, not an audit trail or mandatory session read |

---

## What Each Schema Misses

### Governance misses:
- No mechanism for tracking who asked for what and whether the outcome matched their expectation (definition-of-fixed)
- No sentiment or relationship health tracking
- No passive signal detection for incomplete context (signal cues)
- No narrative preservation -- strips communication to declarative rules
- No cadence or update rhythm -- operates on session-by-session basis
- No compression protection -- no never-drop list for critical content

### 1project misses:
- No complexity monitoring -- no signals, no soft-metrics (freeform-patterns exist but have no signal mapping)
- No rules or vision -- relies on goal drift as the only governance signal
- No architecture or ownership tracking
- No attribution beyond the human who confirms
- No mechanism for tracking tribal knowledge
- No cadence or review rhythm
- No edit mode governance
- project-state-when-work-starts-and-stops is simplified (no baseline, diff, or blocked-on-discrepancy)

### Narrative misses:
- No code-level governance (rules, architecture, object classes, drift signals)
- No scope hierarchy or inheritance
- No complexity-density monitoring for operational complexity
- No tier system or operational-mode posture changes
- No auto-trigger self-healing mechanism
- No module extraction pathway
- No conflation disambiguation
- No flat markdown output -- JSON-only, which limits human readability for some contexts
- No workspace root declaration -- storage is consuming-system's decision
- JSON isolation requirement (record sets must not cross-contaminate) is declarable but not enforceable by the schema alone

---

## Known Assumptions and Their Status

| # | Assumption | Status | Mitigation |
|---|---|---|---|
| 1 | self-contained declaration prevents agents from reaching outside | Partially mitigated | sibling-isolation added to all three schemas; tested once (MODES_OF_ACTION); not tested with adjacent sibling schemas |
| 2 | Governance tier system will be populated from real data | Mitigated | setup-note on agent-session-open-close-measurement explicitly states unconfigured commands make tier system inert |
| 3 | Agents can reliably detect signal cues | Mitigated | false-positive-tracking added; misfires route to review patterns; trust reduced rather than cues disabled |
| 4 | 1project exit threshold (session-count 3) is correct | Accepted risk | Arbitrary; testable via H5; will be validated or revised with real data |
| 5 | Agents can correctly classify entries for compression | Mitigated | lossy-artifact-warning acknowledges compression is lossy; durable sections (known-decisions, open-threads, observed-patterns, handoff-note) survive regardless; unflagged detail is accepted loss |
| 6 | Someone will trigger narrative review cadence | Accepted risk | Schema recommends cadence but cannot enforce; stale-open-thread catches individual thread staleness but not whole-record dormancy |
| 7 | 1project-to-governance transition preserves context | Mitigated | 12-step migration-protocol maps fields, identifies what carries over, what needs human input, what cannot be derived |
| 8 | Narrative JSON isolation is enforceable | Accepted risk | Schema declares isolation requirement; enforcement depends on consuming system implementation |

---

## Research Paper Scaffold

### Problem Statement
AI agents and human teams operating on shared projects, client accounts, and organizational workflows lose critical context through tribal knowledge decay, undocumented decisions, inconsistent terminology, and unreliable session handoffs. Existing tools (documentation, SOPs, CRMs, bug trackers) capture fragments but none answer: what actually happened, who decided what, and what do I need to know that nobody wrote down?

### Intervention
A family of three portable JSON schemas that govern how agents author and maintain structured records:
- **Governance**: enforces rules, ownership, and behavioral postures across code operations
- **1project**: maintains session continuity and failure memory for single-goal work
- **Narrative**: captures structured narrative records preserving tribal knowledge, attribution, resolution alignment, and sentiment across any entity with an evolving story

### Measurable gains (dependent variables):
- Reduction in tribal knowledge loss (narrative: tribal flag count vs. unrecoverable information incidents post-handoff)
- Reduction in pitfall rediscovery rate (governance + 1project: session-count trend over time)
- Reduction in definition-of-fixed divergence (narrative: definitions-match rate across reviews)
- Reduction in context reconstruction time (all: time from session open to productive work, with vs. without schema)
- Reduction in scope drift (governance: soft-metric tier change frequency and direction)

### Measurable costs (dependent variables):
- Overhead per session (all: time spent on state tracking open/close vs. productive work time)
- Compression information loss (narrative: detail unrecoverable after quarterly compress that was later needed)
- False signal rate (narrative: signal cues that fired on noise rather than real tribal knowledge)
- Exit-condition accuracy (1project: false positive and false negative rate of session-count 3 threshold)
- Stale record rate (narrative: records that go dormant despite declared cadence)

### Testable hypotheses:
- H1: Structured narrative records reduce context reconstruction time by >50% for entity handoffs
- H2: Pitfall tracking with session-count reduces failure rediscovery by >70% within 6 months
- H3: Definition-of-fixed tracking reduces resolution mismatch reports by >40%
- H4: Signal cue detection catches >60% of tribal knowledge that would otherwise be lost at handoff
- H5: The 1project exit condition (session-count >= 3) correctly predicts governance need in >80% of cases
- H6: Compression under never-drop rules with lossy-artifact-warning preserves >95% of load-bearing information across quarterly cycles
- H7: Sibling-isolation declaration prevents cross-schema contamination in >90% of multi-schema sessions

### Data collection requirements (not yet defined):
- Baseline measurement protocol: what does "current state" look like before schema deployment?
- Sample size: how many projects/accounts/narratives constitute a meaningful test?
- Timeline: how long must the system run before results are meaningful?
- Instrumentation gap: governance has built-in measurement (agent-session-open-close-measurement); 1project and narrative require external measurement methods
- Control condition: before-schema vs. after-schema per deployment, or matched pairs with/without schema
