# Assumption-Failure Penetration Review

## Purpose

This is a penetration-test-style review of `AI-Links` as a control-plane framework.

It is not a network scan, code exploit exercise, or runtime application assessment.

The target here is the framework's control model:

- startup truth
- schema routing
- measurement
- migration controls
- human approval gates
- context isolation
- distribution vs realtime copy discipline

A "hole" in this document means one of these conditions:

- the framework depends on an assumption that is stronger than its enforcement
- the framework declares a safety property that the consuming system must enforce externally
- the framework can appear governed while a required control loop is actually absent
- the framework relies on human or agent discipline where a verifier should exist

## Scope

In scope:

- `AGENTS.md`
- `docs/*`
- `AGENTS-schema-*.json`
- schema companion READMEs
- templates

Out of scope:

- application runtime security
- host/network hardening
- connector/API credentials
- operating system exploitability

## Method

This review stress-tests explicit and implicit assumptions against these abuse classes:

- overeager agent
- misaligned or shortcut-seeking agent
- fatigued human approver
- underconfigured deployment
- mixed-schema context contamination
- shadow governance regrowth
- stale or partial migration state
- drift between distribution and realtime schema copies

## Severity Model

- `Critical`: can create a false sense of governed safety or silent high-consequence drift
- `High`: likely to break correctness, boundary control, or migration integrity in normal use
- `Medium`: significant fidelity, portability, or adoption risk
- `Low`: mostly research, framing, or edge-case risk

## Findings

### PF-01: Measurementless governance is fail-open

Severity: `Critical`

Assumption under test:

- Governance can safely exist before measurement commands are configured.

Why this is a hole:

- Governance explicitly says unconfigured measurement commands render the tier system, auto-triggers, and discrepancy signal inert.
- Gov2Gov then assumes steady-state measurement can detect semantic compression failures after handoff.

Exploit or abuse path:

1. An adapter authors the `AGENTS` file family.
2. The measurement commands are left blank, stubbed, or weak.
3. The workspace still appears governed because the file family exists.
4. Humans and agents trust tiering, discrepancy blocking, and migration checks that are not actually running.

Impact:

- false governed state
- silent drift
- no real open/close discrepancy detection
- migrations that look structurally complete but are operationally unguarded

Patch:

- Treat missing or placeholder measurement commands as an invalid governance state for anything above exploratory use.
- Ship reference measurement packs for `docs-repo`, `app-repo`, and `module`.
- Add a bootstrap validator that fails authoring completion when measurement is unconfigured.

Risk acceptance:

- Accept only for single-operator exploratory setups where no one is claiming steady-state governance or migration completeness.

Evidence:

- `AGENTS-schema-governance.json`
- `README-AGENTS-schema-governance.md`
- `AGENTS-schema-comparison-matrix.md`
- `README-AGENTS-schema-gov2gov-migration.md`

### PF-02: Declarative isolation is not actual isolation

Severity: `High`

Assumption under test:

- `self-contained`, `sibling-isolation`, and narrative JSON isolation are sufficient to prevent cross-contamination.

Why this is a hole:

- The schemas declare isolation rules, but they do not enforce loader boundaries.
- Narrative explicitly states JSON isolation is declarable but not enforceable by the schema alone.

Exploit or abuse path:

1. Multiple schemas or neighboring records are loaded into one context window.
2. The agent blends structurally similar fields across siblings.
3. A consuming system surfaces adjacent narrative records without strict isolation.
4. The wrong schema logic or wrong entity memory influences authoring.

Impact:

- wrong artifact generation
- narrative leakage across entities
- schema routing errors that look plausible
- hidden policy drift because the output still "looks structured"

Patch:

- Add a loader contract that allows only the routed schema into active context.
- Add per-record or per-entity namespace boundaries for narrative deployments.
- Provide reference wrappers or prompts that explicitly clear sibling artifacts before loading the chosen schema.

Risk acceptance:

- Accept only in tightly supervised manual workflows with one active schema and no automated cross-record loading.

Evidence:

- `AGENTS-schema-triage.md`
- all `AGENTS-schema-*.json` sibling-isolation declarations
- `README-AGENTS-schema-narrative.md`
- `AGENTS-schema-comparison-matrix.md`

### PF-03: Human-confirms is treated as a control without approval-quality controls

Severity: `High`

Assumption under test:

- A human confirmation is inherently meaningful.

Why this is a hole:

- The framework correctly blocks many high-risk transitions on human confirmation.
- It does not consistently require the approval payload to prove the human understood what was being approved.

Exploit or abuse path:

1. An agent frames a scope, migration, or rule change vaguely.
2. A rushed human says yes to keep momentum.
3. The system treats the approval as strong authorization.
4. A bad structure or dangerous change becomes legitimized by a low-quality confirmation.

Impact:

- unsafe scope shifts
- migration shortcuts blessed without comprehension
- destructive or semantically weakening changes approved under ambiguity

Patch:

- Require a short "approval summary" field for high-risk confirmations: what changed, what remains, what is being waived.
- For destructive, scope, and migration approvals, require the human to confirm the exact subject, not just the action.
- Add a template for "what you are approving" before blocked-until gates.

Risk acceptance:

- Accept only when the operator is experienced and the consequence of misunderstanding is low.

Evidence:

- `AGENTS-schema-triage.md`
- governance, narrative, and gov2gov blocked-until declarations
- `Getting-Started-For-Humans.txt`

### PF-04: Wrong-workspace protection is procedural only

Severity: `High`

Assumption under test:

- Echoing the current path and asking one confirmation question is enough to prevent wrong-repo work.

Why this is a hole:

- The wrong-workspace guard is strong as doctrine.
- It is not mechanically anchored to a verified workspace identity marker.

Exploit or abuse path:

1. An agent starts in a similar or adjacent repo.
2. The path is echoed, but the human approves casually or misses the mismatch.
3. Work proceeds in the wrong workspace with a plausible explanation.

Impact:

- cross-repo contamination
- misplaced docs or policies
- false ingest claims against the wrong target

Patch:

- Add a workspace identity marker convention that must match before edits.
- Add a pre-edit check script for path, repo name, and optional branch or sentinel file.
- Require the wrong-workspace confirmation to be recorded for non-trivial sessions.

Risk acceptance:

- Accept for bounded local hygiene work only.

Evidence:

- `docs/AI_COLLAB_STARTUP_PROMPT.md`
- `AGENTS.md`

### PF-05: Semantic compression detection is required but underspecified

Severity: `High`

Assumption under test:

- A steady-state measurement script can reliably detect semantic compression failures after migration.

Why this is a hole:

- Gov2Gov correctly identifies that classification and line-budget compliance are not enough.
- The repo does not provide a reference semantic-compression audit method, sample heuristics, or a validation corpus.

Exploit or abuse path:

1. A migration classifies all files and trims some prose.
2. Duplicate or stale authority remains in adjacent docs.
3. The measurement script checks structure and budgets but not meaning.
4. Migration is declared complete while shadow governance survives.

Impact:

- parallel authority
- stale truth presented as live truth
- support docs quietly regrowing into operating authority

Patch:

- Publish a semantic-compression review checklist with concrete failure signatures.
- Add a reference post-migration audit pack for stale-language, duplicate-law, and live-snapshot detection.
- Add example before/after migrated surfaces.

Risk acceptance:

- Accept only if a human doc owner manually audits every migrated surface before completion.

Evidence:

- `AGENTS-schema-gov2gov-migration.json`
- `README-AGENTS-schema-gov2gov-migration.md`
- `AGENTS-schema-comparison-matrix.md`

Progress note (2026-04-22):

- A prototype mechanical detector ships at `docs/scripts/test-semantic-compression-audit-compliance.ps1`, covering two of the three failure classes:
  - Class 1 — stale date-anchored live-truth claims (`as of <Month Year>`, `latest <noun> -- <date>`, and forward-commitment dates now in the past), via regex plus freshness threshold.
  - Class 3 — duplicate governance prose across files, via paragraph-level shingle + Jaccard similarity.
- Class 2 — dated snapshot claims presented as live truth without an explicit date token — is deliberately tabled, not routed to a human review checklist. Routing to human review would relocate the failure, not close it: human review has its own failure modes (faulty heuristics, attention deficit) that are arguably as unreliable as a mechanical heuristic, just with different error distributions. Claiming "human reviews this" as a patch would repeat the mechanism-as-guarantee overclaim pattern the repo has already been critiqued for. Class 2 remains an acknowledged open gap until either a semantic-embedding-based detector or a bounded checklist with measured adherence lands.
- Still outstanding from the PF-05 patch list: example before/after migrated surfaces, and validation of the prototype detector against a real post-migration corpus (e.g. Workorders after gov2gov). The detector is labeled `"prototype": true` in its JSON output until that validation lands; it must not be wired into `structured-commit` or `structured-review` before then.

### PF-06: The template layer under-transfers the strongest safety rules

Severity: `High`

Assumption under test:

- Downstream users who copy the templates will inherit materially similar safety behavior.

Why this is a hole:

- The repo-level guardrails are stronger than `templates/AGENTS_TEMPLATE.md`.
- The template omits several of the most important distinctions and controls.

Exploit or abuse path:

1. A team adopts the framework by copying the template first.
2. They never import the stronger repo-local language.
3. The resulting `AGENTS.md` looks aligned but lacks critical guardrails.

Impact:

- weaker destructive safety
- weaker workspace boundary protection
- lower downstream fidelity than the framework implies

Patch:

- Harden `templates/AGENTS_TEMPLATE.md` to include:
  - `gitignored` vs `untracked` vs workspace-safe distinction
  - non-synced artifact lane guidance
  - quarantine-before-delete language
  - wrong-workspace confirmation
  - no broad cleanup without inventory and rollback plan

Risk acceptance:

- Accept only if the template is explicitly marketed as skeletal scaffolding rather than a copy-ready baseline.

Evidence:

- `AGENTS.md`
- `docs/AI_COLLAB_STARTUP_PROMPT.md`
- `templates/AGENTS_TEMPLATE.md`

### PF-07: Narrative cadence is fail-open at the record-set level

Severity: `Medium`

Assumption under test:

- Monthly and quarterly cadence will be triggered often enough to keep records alive.

Why this is a hole:

- Narrative explicitly says cadence is recommended, not enforced.
- The schema can detect stale threads, but not a stale record set overall.

Exploit or abuse path:

1. Records are created successfully.
2. Review cadence is not scheduled or not honored.
3. Open threads may be visible, but dormant records quietly decay.

Impact:

- silent narrative rot
- false confidence that tribal knowledge is preserved
- stale ownership and decision history

Patch:

- Add record-level stale detection to the register.
- Add optional review SLA fields.
- Add automation guidance for cadence execution.

Risk acceptance:

- Accept for small manual-only environments where narrative review is directly owned by one person.

Evidence:

- `README-AGENTS-schema-narrative.md`
- `AGENTS-schema-narrative.json`
- `AGENTS-schema-comparison-matrix.md`

### PF-08: Distribution vs realtime copy discipline lacks a drift detector

Severity: `Medium`

Assumption under test:

- Humans and agents will manage distribution-to-realtime sync direction correctly without a dedicated detector.

Why this is a hole:

- Gov2Gov defines the distinction well.
- It does not provide a concrete version lineage or divergence audit mechanism.

Exploit or abuse path:

1. Realtime copies evolve locally under delivery pressure.
2. Distribution copy is not updated, or local changes are not declared as workspace-specific.
3. Two authorities exist with overlapping legitimacy.

Impact:

- undeclared forks
- inconsistent naming and schema semantics
- migration rules varying by deployment

Patch:

- Add explicit lineage metadata for distribution version, realtime version, and local divergence status.
- Add a promotion checklist for validated lessons moving upstream.
- Add a reference sync audit for distribution vs realtime copies.

Risk acceptance:

- Accept only when there is one active deployment and one operator.

Evidence:

- `AGENTS-schema-gov2gov-migration.json`
- `README-AGENTS-schema-gov2gov-migration.md`
- `AGENTS-schema-comparison-matrix.md`

### PF-09: Operative heuristics can be overtrusted as hard law

Severity: `Medium`

Assumption under test:

- Users and agents will correctly distinguish "operative heuristic" from validated technical law.

Why this is a hole:

- The scratchpad itself identifies the risk and proposes explicit labeling.
- That label is not yet propagated across the live schema family.

Exploit or abuse path:

1. A workspace adopts context-ordering or compression warnings as if they were proven invariants.
2. The team builds rigid process around them.
3. Edge cases or model changes appear, but the system treats the heuristic as absolute law.

Impact:

- brittle process
- over-claiming in research or product framing
- rejecting valid exceptions because the language sounded absolute

Patch:

- Apply the "operative heuristic" label where appropriate across schema docs.
- Add a validation-status table: proven, observed, heuristic, or pending.
- Separate research claims from production guidance more explicitly.

Risk acceptance:

- Accept for internal experimental use if the team already understands the claims are heuristic.

Evidence:

- `docs/SCRATCHPAD_prompt_efficacy_patterns.md`
- `AGENTS-schema-comparison-matrix.md`

### PF-10: External issue ownership is assumed, not guaranteed

Severity: `Medium`

Assumption under test:

- Bug reports and feature requests will live in some external system and that system will be named.

Why this is a hole:

- Every schema excludes issue governance.
- The framework does not ship a minimum fallback lane when no real external issue system exists.

Exploit or abuse path:

1. A team adopts the schemas without a disciplined external issue tracker.
2. Pitfalls, decisions, and unresolved work are recorded locally.
3. Remediation ownership falls between the schema boundary and an absent external lane.

Impact:

- dropped defects
- false separation between governance memory and work tracking
- unresolved issues hidden behind "external system" language

Patch:

- Add a minimum fallback issue-lane pattern for small deployments.
- Require explicit declaration when the external issue system is absent.
- Add a bootstrap question: where do defects and feature requests go today?

Risk acceptance:

- Accept only when the operator already maintains a reliable external backlog.

Evidence:

- all `AGENTS-schema-*.json` human-reported-issue-governance declarations
- `README-AGENTS-schema-1project.md`
- `README-AGENTS-schema-governance.md`
- `README-AGENTS-schema-narrative.md`

### PF-11: Abstract adoption without worked examples is high-variance

Severity: `Medium`

Assumption under test:

- Teams can adapt the framework safely from abstract models and templates alone.

Why this is a hole:

- The repo's own TODO says the first example adaptation path and multi-repo handoff example are still missing.
- Without examples, adopters fill gaps with their own defaults.

Exploit or abuse path:

1. A team copies the framework into a live repo.
2. Missing practical details are filled with private examples, weak shortcuts, or local jargon.
3. The adapted system diverges from the public-safe intended model.

Impact:

- unsafe or internalized examples
- weak migration practice
- under-specified startup packs

Patch:

- Add one public-safe single-repo adaptation path end to end.
- Add one public-safe multi-repo handoff pack.
- Add a destructive-command safety checklist.

Risk acceptance:

- Accept only for private guided onboarding where an expert is supervising the adaptation.

Evidence:

- `docs/TODO_ai_links.md`
- `docs/README_ai_links.md`

### PF-12: Full-plan enumeration is a policy, not a verifier

Severity: `Medium`

Assumption under test:

- Agents will disclose the whole remaining migration or governance plan once told to do so.

Why this is a hole:

- The triage and gov2gov material correctly call out selective disclosure as a real failure mode.
- There is no structured completion artifact that mechanically forces full-plan enumeration.

Exploit or abuse path:

1. An agent finishes the visible core and pauses.
2. Remaining conflict-surface or handoff work is not enumerated.
3. The human believes the stopping point is natural completion.

Impact:

- silent incompleteness
- partial migrations sold as finished
- deferred risk not visible to the operator

Patch:

- Add a standard completion checklist or plan-status artifact for migrations and governance refactors.
- Require "full plan with status per step" as a structured output, not just a prose norm.
- Add example completion reports.

Risk acceptance:

- Accept only for low-consequence local maintenance.

Evidence:

- `AGENTS-schema-triage.md`
- `AGENTS-schema-gov2gov-migration.json`
- `README-AGENTS-schema-gov2gov-migration.md`

## Patch Priority

Patch now:

1. `PF-01` Measurementless governance is fail-open
2. `PF-05` Semantic compression detection is underspecified
3. `PF-06` The template layer under-transfers the strongest safety rules

Patch before broad downstream adaptation:

1. `PF-02` Declarative isolation is not actual isolation
2. `PF-03` Human-confirms lacks approval-quality controls
3. `PF-04` Wrong-workspace protection is procedural only
4. `PF-11` Abstract adoption without worked examples is high-variance
5. `PF-12` Full-plan enumeration is policy, not verifier

Can be risk-accepted in bounded environments:

1. `PF-07` Narrative cadence is fail-open at the record-set level
2. `PF-08` Distribution vs realtime copy discipline lacks a drift detector
3. `PF-09` Operative heuristics can be overtrusted as hard law
4. `PF-10` External issue ownership is assumed, not guaranteed

## Acceptance Rule

Do not accept a risk merely because it is "documentation-only."

For this repo, documentation is the control plane. A documentation hole is a control hole when:

- it changes what agents think is allowed
- it changes what humans think is complete
- it changes whether drift is detectable
- it changes whether adjacent truth survives migration

## Recommended Next Moves

1. Add a reference measurement pack and validator for governance and gov2gov.
2. Harden the template layer so copied safeguards match the repo's real safeguards.
3. Add a structured migration completion artifact that forces full-plan status disclosure.
4. Add one public-safe single-repo adaptation example and one multi-repo handoff example.
5. Add validation-status labeling for research claims and operative heuristics.
