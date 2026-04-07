# AGENTS Schema Family Comparison Matrix

This matrix compares the four sibling schemas across structural, behavioral, and boundary dimensions. It is designed to surface gaps, overlaps, and assumption failures before any schema is deployed for data collection.

---

## Identity

| Dimension | Governance | 1project | Narrative | Gov2Gov |
|---|---|---|---|---|
| File | AGENTS-schema-governance.json | AGENTS-schema-1project.json | AGENTS-schema-narrative.json | AGENTS-schema-gov2gov-migration.json |
| Version | 1.0.0 | 0.1.0 | 0.1.0 | 0.1.0 |
| One-line role | Multi-scope governance for code and operations | Single-goal session continuity | Evolving narrative records for entities with multiple parties | Governance-to-governance migration contract |
| Primary subject | Code, operations, agent behavior | A focused project with one deliverable | Communication, relationships, tribal knowledge | Reconciling one governed system into another |
| Output format | Human-editable flat markdown (AGENTS-*.md) | Human-editable flat markdown (AGENTS-*.md) | JSON data artifacts (not human-editable) | Flat markdown (GOV2GOV-*.md) + JSON registry (GOV2GOV-registry.json) |
| Who writes the output | Agent, human reviews and confirms | Agent, human reviews and confirms | Agent exclusively, human provides input | Agent, human confirms at divergence triggers and phase gates |
| Schema modes | Single mode (steady-state) | Single mode (active until exit-to-governance) | Single mode (active) | Two modes: active (GOV2GOV-* files materialized) and reference (schema present, no artifacts) |

---

## Structural Features

| Feature | Governance | 1project | Narrative | Gov2Gov |
|---|---|---|---|---|
| Entry point file | AGENTS.md (required) | AGENTS.md (required) | Register (required) | GOV2GOV-hello.md (required) |
| Session state tracking | agent-session-open-close-measurement -- command-based with baseline, diff, blocked-on-discrepancy | project-state-when-work-starts-and-stops -- string-based open/close | record-state-at-review-open-and-close -- string-based open/close | migration-state-when-work-starts-and-stops + migration-measurement (command-based) |
| Scope hierarchy | platform > repo > module with inheritance | None -- single scope | None -- entity types independent | None -- source/target pairing, not hierarchy |
| File inheritance | Yes -- Terms, Pitfalls, Rules inherit from parent scope | No | No | No |
| Terms | Required (AGENTS-Terms.md) | Optional (AGENTS-Terms.md) | Embedded in entry narrative | Not applicable -- uses source/target governance terms |
| Rules | Required (AGENTS-Rules.md) | None | Edit-modes and compression-rules govern behavior | GOV2GOV-rules.md with phase gates, divergence triggers, hard rules, completion rules |
| Vision | Required (AGENTS-Vision.md) | None -- goal field replaces vision | None -- subject field replaces vision | None -- migration-scope replaces vision |
| Pitfalls | Optional (AGENTS-Pitfalls.md) with tier-history | Optional (AGENTS-Pitfalls.md) | Observed-bad-patterns and entries | Optional (GOV2GOV-pitfalls.md) for migration-specific failures |
| Templates | Yes -- per file, flat markdown encoding | Yes -- per file, flat markdown encoding | Yes -- per record type, JSON encoding | Yes -- per file, mixed encoding declaration |

---

## Governance Mechanisms

| Mechanism | Governance | 1project | Narrative | Gov2Gov |
|---|---|---|---|---|
| blocked-until gates | human-confirms, verification-passes, parent-valid | human-confirms only | human-confirms (compress mode only) | human-confirms, measurement-passes |
| Complexity monitoring | signals + observed-patterns + soft-metrics + auto-triggers | None | criticality ranking + compression rules | Not applicable -- migration phases replace complexity tiers |
| Edit mode governance | Explicit: add, revise, remove -- all blocked-until human-confirms | Not explicitly declared | Explicit: append, update, compress, correct | Implicit via phase gates -- each phase has entry/exit criteria |
| Compression governance | None | None | Yes -- never-drop list, lossy-artifact-warning, quarterly-only, human-confirms | Not applicable |
| Decision records | DECISION-Record.md as reconciliation artifact | None | known-decisions block with attribution and reversal tracking | GOV2GOV-registry.json tracks every surface with disposition and progress |
| Stop-state contract | incomplete-session field (optional) | incomplete-session field (optional) | None | required-stop-state-if-incomplete (mandatory -- migration-objective-at-pause, paused-at-migration-phase, remaining-work, blocker, first-unknown, next-automatic-action) |

---

## Signal and Detection

| Signal capability | Governance | 1project | Narrative | Gov2Gov |
|---|---|---|---|---|
| Complexity signals | term-count, rule-count, deliverable-divergence, measurement-discrepancy-count, cross-ref-count | None | None | None -- parallel-authority-surface-class replaces complexity signals |
| Architecture drift signals | 5 drift signals with observed-patterns and soft-metrics | None | None | None |
| Signal cue classes | None | None | 8 classes with false-positive-tracking | None |
| Capture workflows | None | None | 6 named workflows | None |
| Pattern tracking | observed-good-patterns, observed-bad-patterns (with signal mapping) | freeform-patterns: working/not-working (3-session repeat is exit signal) | observed-patterns good/bad (with tribal flag) | core-lessons encode patterns at schema level; GOV2GOV-pitfalls.md captures instance failures |
| Deduplication tracking | None | None | None | Extraction is incomplete while original source wording exists -- built into hard rules and authoring sequence |

---

## Human-Facing Features

| Feature | Governance | 1project | Narrative | Gov2Gov |
|---|---|---|---|---|
| Cast / attribution | parties, end-user, deliverable-recipients on scope | None | Per-entry: owner, context-holder, requested-by, finalized-by | source-governance-system, target-governance-system, confirmed-by on stop-state |
| Definition-of-fixed | None | None | Yes -- fixed-per-client, fixed-per-tech, definitions-match | Not applicable |
| Sentiment tracking | None | None | sentiment-and-tension per entry | Not applicable |
| Tribal knowledge flag | None | None | tribal: true on entries and patterns | Not applicable -- load-bearing-truth-summary on registry entries serves similar purpose |
| Handoff mechanism | incomplete-session pointing to HANDOFF.md | incomplete-session pointing to HANDOFF.md | handoff-note required on thread closure | required-stop-state-if-incomplete (mandatory, structured, with next-automatic-action) |
| Cadence | None declared | None declared | monthly, quarterly, transient | Phase-driven -- not time-cadenced |

---

## Cross-Schema Connections

| Connection | From | To | Mechanism |
|---|---|---|---|
| Exit to governance | 1project | Governance | exit-to-governance conditions + 12-step migration-protocol |
| Narrative tracks project | Narrative | 1project or Governance | known-decisions.project-ref |
| Sibling acknowledges narrative | Governance or 1project | Narrative | narrative-ref field on AGENTS-hello.md |
| Gov2gov targets governance | Gov2Gov | Governance | source/target pairing in GOV2GOV-hello.md; governance assumes authority after migration |
| Gov2gov handoff | Gov2Gov | Governance | post-migration-handoff-to-steady-state block; ancillary ring becomes permanent |
| Sibling isolation | All | All | sibling-isolation declaration prevents cross-contamination |
| Shared boundary | All | External | human-reported-issue-governance |
| Shared invariant | All | All | context-ordering-rule (identical verbatim) |
| Shared warning | All | All | context-compression-warning (durable state to files, not context) |
| Distribution vs. realtime | Gov2Gov | All schemas | distribution-and-realtime-copies: distribution is authoritative for structure, realtime inherits and adds workspace-specific content, undeclared divergence is a parallel authority surface |

---

## Safeguards

| Safeguard | Schema | What it addresses |
|---|---|---|
| sibling-isolation | All four | Prevents cross-contamination when multiple schemas share a context window |
| context-compression-warning | All four | Durable state must be written to files; compression fidelity is not guaranteed |
| agent-session-open-close-measurement setup-note | Governance | Unconfigured measurement commands render tier system inert |
| false-positive-tracking | Narrative | Signal cues will misfire; route false positives to review patterns; reduce trust rather than disable |
| lossy-artifact-warning | Narrative | Compression is explicitly lossy; unflagged detail is accepted loss |
| migration-protocol | 1project | 12-step field mapping for 1project-to-governance exit |
| DECISION-Record.md rewrite | Governance | Reconciliation artifact, not audit trail; read when confused, not every session |
| schema-modes | Gov2Gov | Active vs. reference mode prevents artifact creation in post-migration workspaces |
| extraction-deduplication rule | Gov2Gov | Extraction is incomplete while original source wording exists |
| dual completion-audit | Gov2Gov | Classification completeness AND semantic compression completeness checked independently |
| required-stop-state | Gov2Gov | No silent mid-migration pauses; structured handoff with next-automatic-action |
| post-migration-handoff | Gov2Gov | Ancillary ring monitored permanently; versioned lanes declared or they drift |
| semantic-compression-handoff-to-measurement | Gov2Gov | Steady-state measurement must test for stale language, dated snapshots as live truth, and untrimmed duplicate prose — not just classification and budget |

---

## What Each Schema Misses

### Governance misses:
- No definition-of-fixed tracking (who asked for what and whether the outcome matched)
- No sentiment or relationship health tracking
- No passive signal detection for incomplete context (signal cues)
- No narrative preservation -- strips communication to declarative rules
- No cadence or update rhythm -- operates session-by-session
- No compression protection with never-drop list
- No migration contract for governance-to-governance transitions (defers to gov2gov)

### 1project misses:
- No complexity monitoring (freeform-patterns exist but have no signal mapping)
- No rules or vision -- relies on goal drift as the only governance signal
- No architecture or ownership tracking
- No attribution beyond the human who confirms
- No tribal knowledge tracking
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
- JSON-only output limits human readability for some contexts
- JSON isolation requirement is declarable but not enforceable by the schema alone
- No workspace root declaration -- storage is consuming system's decision

### Gov2Gov misses:
- No steady-state governance capability (defers to governance schema after migration)
- No narrative or tribal knowledge tracking (defers to narrative schema)
- No complexity signals or tier system (uses migration phases instead)
- No cadence -- phase-driven, not time-driven
- No edit-mode governance on non-migration files (defers to target schema)
- Cannot enforce ancillary ring monitoring post-migration (hands off to steady-state governance, but now explicitly requires measurement to test for semantic compression)
- Ancillary terminology distinction (vs. auxiliary) may confuse agents trained on more common "auxiliary" usage

---

## Known Assumptions and Their Status

| # | Assumption | Status | Mitigation |
|---|---|---|---|
| 1 | self-contained prevents reaching outside | Partially mitigated | sibling-isolation added to all four; tested once; not tested with adjacent siblings |
| 2 | Governance tier system populated from real data | Mitigated | setup-note states unconfigured commands make tier system inert |
| 3 | Agents can reliably detect signal cues | Mitigated | false-positive-tracking reduces trust rather than disabling cues |
| 4 | 1project exit threshold (session-count 3) is correct | Accepted risk | Arbitrary; testable via H5 |
| 5 | Agents correctly classify entries for compression | Mitigated | lossy-artifact-warning; durable sections survive regardless |
| 6 | Someone will trigger narrative review cadence | Accepted risk | Recommended not mandatory; stale-open-thread catches threads but not whole records |
| 7 | 1project-to-governance preserves context | Mitigated | 12-step migration-protocol |
| 8 | Narrative JSON isolation enforceable | Accepted risk | Declarable but depends on consuming system |
| 9 | Gov2gov extraction includes deduplication | Mitigated | Hard rule: extraction incomplete while source wording exists |
| 10 | Classification = semantic compression | Mitigated | Dual completion-audit checks both independently |
| 11 | Ancillary ring stays bounded post-migration | Mitigated | post-migration-handoff declares ongoing monitoring; semantic-compression-handoff-to-measurement requires steady-state script to test for stale language, not just budget compliance |
| 12 | Ancillary vs. auxiliary distinction respected | Accepted risk | Defined in terms; agents trained on "auxiliary" may conflate |
| 13 | Distribution and realtime copies stay in sync | Partially mitigated | Sync direction declared (distribution → realtime for structure, realtime → distribution for validated lessons); no automated enforcement; undeclared divergence declared as parallel authority surface |

---

## Research Paper Scaffold

### Problem Statement
AI agents and human teams operating on shared projects, client accounts, and organizational workflows lose critical context through tribal knowledge decay, undocumented decisions, inconsistent terminology, and unreliable session handoffs. Existing tools (documentation, SOPs, CRMs, bug trackers) capture fragments but none answer: what actually happened, who decided what, and what do I need to know that nobody wrote down? Migrations between governance systems compound this loss by declaring structural completion while semantic meaning remains trapped in source documents.

### Intervention
A family of four portable JSON schemas that govern how agents author and maintain structured records:
- **Governance**: enforces rules, ownership, and behavioral postures across code operations
- **1project**: maintains session continuity and failure memory for single-goal work
- **Narrative**: captures structured narrative records preserving tribal knowledge, attribution, resolution alignment, and sentiment
- **Gov2Gov**: provides a migration contract ensuring semantic extraction, not just structural parity, when moving between governance systems

### Measurable gains (dependent variables):
- Reduction in tribal knowledge loss (narrative: tribal flag count vs. unrecoverable information incidents)
- Reduction in pitfall rediscovery rate (governance + 1project: session-count trend over time)
- Reduction in definition-of-fixed divergence (narrative: definitions-match rate)
- Reduction in context reconstruction time (all: time from session open to productive work)
- Reduction in scope drift (governance: soft-metric tier change frequency)
- Reduction in post-migration governance spill (gov2gov: duplicate authority surfaces at completion-audit)

### Measurable costs (dependent variables):
- Overhead per session (all: time on state tracking vs. productive work)
- Compression information loss (narrative: detail unrecoverable after compress that was later needed)
- False signal rate (narrative: signal cues firing on noise)
- Exit-condition accuracy (1project: false positive/negative rate of session-count 3)
- Stale record rate (narrative: records dormant despite declared cadence)
- Migration overhead (gov2gov: time to complete full migration vs. ad-hoc migration)

### Testable hypotheses:
- H1: Structured narrative records reduce context reconstruction time by >50% for entity handoffs
- H2: Pitfall tracking with session-count reduces failure rediscovery by >70% within 6 months
- H3: Definition-of-fixed tracking reduces resolution mismatch reports by >40%
- H4: Signal cue detection catches >60% of tribal knowledge that would otherwise be lost at handoff
- H5: The 1project exit condition (session-count >= 3) correctly predicts governance need in >80% of cases
- H6: Compression under never-drop rules with lossy-artifact-warning preserves >95% of load-bearing information
- H7: Sibling-isolation prevents cross-schema contamination in >90% of multi-schema sessions
- H8: Gov2gov dual completion-audit (classification + semantic compression) catches >90% of duplicate authority that single-check audits miss

### Data collection requirements (not yet defined):
- Baseline measurement protocol before schema deployment
- Sample size guidance for meaningful results
- Timeline for meaningful results
- Instrumentation gap: governance has built-in measurement; other schemas require external methods
- Control condition: before/after per deployment, or matched pairs
