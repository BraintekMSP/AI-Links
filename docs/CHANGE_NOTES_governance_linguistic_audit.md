# Change Notes: Governance Schema Linguistic Audit

Status: in progress
Date: 2026-04-11
Purpose: Track linguistic changes with reasoning so reversions are informed, not blind

---

## Principle

Human linguistic intuition about how ambiguous or directionally loaded a term feels is a strong proxy for how a model will statistically resolve that term. If a word carries exit energy, completion energy, or debug-territory energy to a human reader, it will carry similar statistical weight to the model.

---

## Change 1: blocked-until → requires-human-approval-before-proceeding (or similar)

### Current
`blocked-until: human-confirms`

### Problem
"blocked" and "until" both carry strong exit/stop energy.
- "blocked" → wall, obstacle, cannot proceed. Passive state. Satisfying to be at.
- "until" → waiting. Implies time-based resolution. Does not imply active engagement.
- Together: "stop and wait for someone else to act." That is a satisfying abort state, not a continuation gate.

### Human's framing
"Abort-with-explanation seems more inline with the statistical area we want to end up in."

### Intended replacement concept
The gate should feel like: "you must actively obtain approval to continue" not "you are stopped."
The energy should be continuation-with-checkpoint, not blockage-with-rescue.

### Selected replacement
- `report-to-human` — completely reframes the gate from "you are stopped" to "you have a reporting obligation." The agent's action is to report. The human's role is to receive. No wall, no gate, no exit energy. Continuation-with-accountability.

### Scope of change
This term appears: meta.blocked-until-values, every generates block, every edit-mode, operational-modes, auto-triggers, optional-artifacts, soft-metrics. Major sweep.

### Reversion notes
If this change weakens the gate (agents proceed without approval more often), revert. The old term was wrong in energy but right in strength. Monitor whether the new term maintains the same gate enforcement.

---

## Change 2: greenfield → first-time-governance-authoring

### Current
`"greenfield": "no existing AGENTS files -- generate from schema and templates"`

### Problem
"Greenfield" to a non-technical human connotes: meadow, open field, all-things-go, possibly military operations. The denotative definition is not accessible without software engineering context. Models trained on mixed corpora will pick up the open/permissive/freedom connotation alongside the technical meaning.

### Human's framing
"I could not tell you a denotative definition, but feel it will strongly tie into military ops, which I also feel it feels less likely to encourage progress in all cases than R&D."

### Intended replacement concept
The mode should read as: "this is the first time governance is being authored here." No connotation of wide-open freedom. No military ops. No meadow. Just: first authoring.

### Candidate replacements
- `first-time-governance-authoring` — precise, no baggage
- `initial-authoring` — shorter, still clear
- `new-workspace` — but "new" is weak

### Scope of change
migration.mode values, authoring-sequence keys, migration definitions. Moderate sweep.

### Reversion notes
If agents stop recognizing the mode distinction (because "greenfield" was a very strong, well-known term in training data), revert. The old term carried wrong energy but was highly recognizable.

---

## Change 3: if-unresolved → when-recovery-fails-after-attempt

### Current
```
"if-unresolved": "record new pitfall entry -- do not raise tier -- escalate to intra-session tracking"
```

### Problem
"if" (logic gate) + "unresolved" (negation/absence) = runtime error pattern. This reads like an exception handler that puts responsibility on the human without providing a path forward. Lands in debug territory.

### Human's framing
"If (logic) unresolved (connotes negation, not a logical operation) would likely land in an error state - not programmatically here, but rather as a runtime error similarity which expects human intervention without a direct feedback from the program or other assistance. Lands in debug territory putting responsibility solely on the human."

### Intended replacement concept
The branch should feel like: "recovery was attempted and did not succeed — here is what happens next." Active, not passive. The agent still has work to do (record, escalate). Not a dead end.

### Selected replacement
- `else` — universally recognized from training data as "the other execution path." Not an error state, not a dead end, not debug territory. Just: the other thing that happens. Pairs naturally with `if-resolved` / `else`. No negation, no passivity.

### Scope of change
auto-triggers block only (3 occurrences: intra-attempt, intra-session, cross-session). Small sweep.

### Reversion notes
If the new term is too long and compresses away, or if agents treat the recovery-attempt framing as license to try more aggressively before recording, revert.

---

## Change 4: Remove incomplete-session field

### Current
```
"incomplete-session": {
  "type": "file-path",
  "required": false,
  "role": "path to HANDOFF.md left at the present working directory..."
}
```

### Problem
- The agent-session-open-close-measurement already captures whether a session completed cleanly.
- The incomplete-session field adds clerical overhead without adding value.
- The concept of writing a HANDOFF.md feels similar to the "if-unresolved" debug territory — satisfying clerical exit that substitutes for continuing work.
- The plan-completion-directive and plan-requirement-directive now cover the "session must complete" intent.

### Human's framing
"Also feels unneeded. The check would be agent-start-stop-whatever. That one informs if the session was likely abandoned. And an agent prompt acting on incomplete-session probably is not benefitting from the additional clerical work."

### Action
Remove the field from AGENTS-hello.md fields and template. The measurement system and plan directives cover the intent.

### Reversion notes
If sessions end without any record of what was incomplete and the measurement diff doesn't capture enough detail, the field may need to return in a different form.

---

## Change 5: Remove required: false declarations

### Current
Multiple fields have `"required": false` explicitly declared.

### Problem
If false is the default, declaring it explicitly is misdirecting. It draws attention to the field's optionality rather than its purpose. This is a linguistic artifact — the field is not semantically valuable in any case where false is the expected default.

### Human's framing
"I really don't see the need to add 'required:false'. If that's the default, it seems needlessly misdirecting. this is a linguistic artifact, and that field is not semantically valuable to any case I think of."

### Action
Remove `"required": false` from fields where it appears, leaving only `"required": true` declarations. Absence of `required` means optional.

### Reversion notes
If agents start treating unlabeled fields as required (because the absence of the flag is ambiguous), add a meta-level declaration: "fields without a required declaration are optional."

---

## Change 6: All bare property name renames

### Principle
"Well named variables make code not need documentation." Consistency with naming methodology applied throughout the rest of the schema family.

| Current | New | Reasoning |
|---|---|---|
| scope | scope-identity-statement | Bare. Scope of what? |
| parent | parent-scope-agents-hello-path | Bare. Parent what? |
| inherits (Terms) | inherits-terms-from-parent-scope | Bare. Inherits from what? |
| inherits (Pitfalls) | inherits-pitfalls-from-parent-scope | Same |
| success (Vision) | what-done-looks-like-at-this-scope | Bare. Success of what? |
| locked (Vision) | commitments-that-must-survive-scope-evolution | Bare. Locked what? |
| rules (Rules) | operating-rules-for-this-scope | Bare. What kind? |
| refs (Rules) | required-files-loaded-before-rules-are-valid | Bare. Refs to what? |
| date (tier-history) | date-tier-changed | Bare date |
| trigger (pitfalls) | failure-trigger | Compromise between brevity and clarity |
| date (DECISION-Record) | date-decision-was-made | Bare date |
| decision (DECISION-Record) | approval-or-rejection | Bare |
| context (DECISION-Record) | why-this-discrepancy-exists | Very bare |
| changed (DECISION-Record) | what-specifically-changed | Bare |
| object (ownership) | durable-business-object-name | Bare in multiple-class context |
| class (ownership) | object-class | Already fixed in gov2gov, not here |

### Reversion notes
If any renamed field is too long and causes formatting issues in flat markdown encoding, shorten. The two-space indentation plus long field names may create readability problems in actual AGENTS files. Test with real output before committing.

---

## Change 7: Parked — exit signal audit of recovery language

Not being done in this pass. Noted for future: auto-trigger recovery language, incomplete-session language, and optional-file language may function as satisfying abort grammar. Separate focused audit needed.

---

## Research Finding: Negation Mitigation, Not Inversion

### Human neuroscience

Zuanazzi et al. (NYU, PLOS Biology, May 2024): "Negation mitigates rather than inverts the neural representations of adjectives."
Source: [NYU news](https://www.nyu.edu/about/news-publications/news/2024/may/how-does--not--affect-what-we-understand--scientists-find-negati.html) | [PLOS Biology](https://journals.plos.org/plosbiology/article?id=10.1371/journal.pbio.3002622)

Key finding: "not hot" is first interpreted as closer to "hot" than to "cold." The brain activates the affirmative FIRST, then slowly mitigates (never inverts) toward a middle state. Negation slows processing measurably. The final interpretation is "less hot," never "cold."

Implication for instructions: "do not modify existing entries" first activates "modify existing entries" in the brain, then weakly suppresses toward "less modify" — not toward "leave entries alone."

### LLM behavior

"Negation: A Pink Elephant in the Large Language Models' Room" (2025): LLMs show the same pattern but worse.
Source: [arxiv.org/html/2503.22395v2](https://arxiv.org/html/2503.22395v2)

Key finding: negation tokens have "limited effect on the representations learned distributionally." The model sees "do not delete" and the strongest activation is "delete." Named after the pink elephant paradox — telling someone not to think of something makes it more salient.

Tested across Llama 3, Qwen 2.5, Mistral at multiple sizes. Larger models handle negation better (Spearman 0.867) but never eliminate the problem. Vision-language models asked for "no elephant" produce elephants.

### The critical difference

Humans get a second processing step where the brain partially corrects (mitigates). LLMs don't — one forward pass means whatever activated strongest wins. "Do not delete" → "delete" is the strongest activation and the negation token doesn't get a second chance to override it.

### Schema implication

Every "do not" construction in the schemas is fighting the model's strongest activation. The fix is to state what TO do, not what NOT to do:

- "do not modify" → "preserve as-is" (not "preserve unchanged" — "unchanged" carries "change" with negation prefix, model activates "change" first)
- "do not discard" → "retain and map to new structure"
- "do not proceed" → report-to-human (already selected)
- "do not leave empty" → "populate before file is valid"
- "do not call the migration complete" → "the migration is complete only when [conditions]"
- "must not restate" → "state only what is new to this scope"
- "may not store a local copy" → "read from the source"

Guiding principle: every replacement must be a pure affirmative directive with zero negation morphology. "Un-", "not", "no", "never", "must not", "may not" all activate the thing they're trying to prevent. "As-is", "retain", "report", "only when" are pure state descriptors or affirmative actions.

### Word energy taxonomy (emerging from review)

- **"avoid"** — acceptable under certain circumstances. Directional guidance, not a hard stop.
- **"disqualify"** — acceptable in most circumstances. Active judgment, not negation.
- **"mismatch"** — problematic. Carries "something is wrong" energy before the gate purpose lands.
- **"requires"** — soft enough to read as optional. Use "report-to-human" or "gate" instead.
- **"signoff"** — bureaucratic. Connotes paperwork, not active engagement.
- **"gate"** — good. Checkpoint energy, not wall energy. `measurement-gate-on-discrepancy` was selected over `measurement-mismatch-requires-human-signoff` for this reason.

### Revised field name
`blocked-on-discrepancy` → `measurement-gate-on-discrepancy` (not `measurement-mismatch-requires-human-signoff` — that carried three problems: mismatch=error, requires=soft, signoff=bureaucratic)

### Deeper observation: taxonomy informs word choice but does not inform on the structural problem

The word energy taxonomy tells you which words to use and which to avoid. It does not explain WHY certain words create problems at the model inference level.

The structural problem: ambiguous qualifiers force composition depth. "Mismatch" requires the model to reconstruct: mismatch between WHAT and WHAT? What does "matching" mean here? Then evaluate the negation of that match. That is multi-step composition — the same failure mode Dziri et al. identified.

Attempting to fix an ambiguous qualifier by adding more qualifiers compounds the problem. Each qualifier adds another composition step. The spiral is not about language getting more negative — it is about language getting deeper.

Two distinct failure modes that compound:
1. **Negation activation** (Zuanazzi 2024, Pink Elephant 2025): "not X" activates X first, then weakly mitigates
2. **Composition depth from ambiguous qualifiers** (Dziri 2023): words that require multi-step qualification force the model into exactly the composition pattern that fails at depth

Both are real. They stack. An ambiguous negated qualifier ("measurement-mismatch") hits both failure modes simultaneously.

The fix for both: use words that resolve in one step without qualification. "Gate" resolves immediately (checkpoint). "On discrepancy" resolves immediately in measurement context (numbers differ). Neither requires the model to compose a multi-step qualification chain.

This should be applied across all four schemas as a systematic rewrite pass. It is not cosmetic — it directly addresses a documented failure mode in both human cognition and LLM inference.

### Change 8: Systematic negation rewrite (scope TBD)

Every "do not" / "must not" / "may not" / "never" construction in the schema family should be audited and rewritten as affirmative directives where possible. This is a large sweep and should be done with the same change-notes discipline as the other changes.

Estimated scope:
- Governance schema: ~20+ negation constructions
- Gov2gov: ~15+
- 1project: ~5
- Narrative: ~10
- Triage: ~5

This is a significant rewrite. Park or proceed based on human decision.
