# Scratchpad: Prompt Efficacy Patterns and Research Foundations

Status: consolidating toward research paper viability

---

## Prompt Efficacy Observations

### Phrasing Efficacy (from real usage)

| Phrasing | Efficacy | Why |
|---|---|---|
| "Close all todos" | Extremely weak | Mechanical checklist mentality, tick boxes, declare done |
| "Complete the migration" | Weak | Names the outcome but gives no autonomy or judgment framing |
| "Complete the plan" | Medium | References a specific artifact, implies following it |
| "Handle the loop end-to-end and only ask me if you need direction" | Extremely strong | Grants ownership of outcome, permits judgment, sets human-confirms gate without making every step a gate |

### Sticky Labels

"North star" resists context compression because the term is distinctive and high-salience. "Primary goal" compresses away. "North star" stays loaded. Distinctive, slightly unusual terms survive compression better than generic ones.

---

## Empirical Observations from Schema Deployment

### Schema Family Lifecycle Progression (Fissure docker build project)

A real project moved through the full schema family lifecycle without the human needing to understand the system:

1. **Started as 1project-shaped** — "just build this docker thing"
2. **Grew into governance** — rules, ownership, and scope emerged
3. **Needed gov2gov** — existing workspace authority had to be reconciled
4. **Now in steady-state governance** — discovering architectural domains through governance measurement (monolith drift → missing constructor domain via object-class violations)

Key observations:
- The human's role was to push back when things felt wrong. The schemas routed and structured the work.
- The human reported "I don't feel like things are being hidden from me" after the full-plan enumeration rule was in place. Target metric: trust through visibility.

### Vocabulary as Discovery Mechanism (Fissure constructor extraction)

The governance schema's architecture.object-classes (truth, constructor, mutation, consumer, validator) gave the agent vocabulary that made a structural gap legible. Six runtimes carrying copied helpers looked like "messy duplication" without the vocabulary. With it: "unowned constructor logic between truth and consumers." The agent extracted the shared projection core autonomously — 24 minutes, 17 files, +410/-695 lines.

Key takeaway: the strongest schema value may be discoverability — giving agents words to name problems that would otherwise stay invisible.

### Narrative as Post-Session Standup Extraction (not yet validated)

Proposed: post-session extraction using the narrative schema as extraction template against .jsonl transcripts. Signal-cues tell the agent what to look for. Output is a structured narrative record filtered to standup-relevant content. Not live maintenance — extraction after work is done.

Status: needs a filter mechanism transient mode doesn't currently have.

### PreText (unformed intuition, parked)

Better text handling system, primarily graphics/canvas performance. Gut sense it belongs somewhere in this space. No articulated reason. Do not force a connection. Revisit when a concrete use case surfaces.

---

## Core Claims Under Development

### Claim 1: Past context cannot not be overridden by future context (operative heuristic)

- **As governance rule:** Sound. Prevents retroactive rationalization.
- **As technical claim:** Imprecise. Attention IS bidirectional within the context window. The model CAN reinterpret; the governance rule says it SHOULDN'T.
- **Refinement:** A constraint imposed against the model's natural behavior. The research question is whether the constraint produces better outcomes.
- **Measurable:** Provide contradictory instructions at position 200 and 4000. Measure adherence with and without the context-ordering-rule.
- **Supported by:** Leviathan et al. (2025) — causal masking means tokens only attend backward during generation, so earlier tokens are positionally disadvantaged. The context-ordering-rule compensates by declaring earlier context authoritative.

### Claim 2: Perfect non-lossy compression for this model in this session ALWAYS loses context in every other case (operative heuristic)

- **Precision:** Compression optimized for one evaluation context is inherently exclusive. What was noise to THIS session but signal to the next is systematically gone.
- **Theoretical basis:** Rate-distortion theory. The "ALWAYS" holds for any session of meaningful complexity.
- **The word "perfect":** Even in the best case where nothing relevant to THIS session was lost, the compression still lost things relevant to other contexts.
- **Measurable:** Compress a session. In a new session, ask questions about content present in the original but not relevant to the compression context. Measure systematic information loss correlated with relevance-to-original-session.
- **Epistemological status: operative heuristic (defeasible).** Not strictly axiomatic — edge cases exist (trivial sessions). But the behavior it produces (distrust compression, write durable state to files) is always correct regardless of edge cases. The cost of treating it as true when occasionally false is near zero. The cost of treating it as false when true is catastrophic. If hedging toward strict accuracy weakens the instruction enough that agents don't internalize it, the hedged version is also wrong — accurate but ineffective.

### Claim 3: Tokenization, statistical projection, and evaluation are functionally one process

- **Technical reality:** Separable (tokenization is deterministic, inference is subsequent). But by the time the model resolves ambiguity, the resolution has happened and cannot be reconsidered. One forward pass, one resolution, no dwelling.
- **Refinement:** The PROCESS is fixed (one pass, no reconsideration). The INPUT is controllable. You can't change how the model resolves. You can change what the model reads before resolving.
- **Supported by:** Dziri et al. (2023) — transformers reduce multi-step reasoning to linearized subgraph matching in one forward pass. Leviathan et al. (2025) — causal masking confirms the one-directional limitation; prompt repetition compensates by giving tokens multiple exposure points.

### Corollary: Human intuition about colloquial ambiguity predicts model error

- "My dog" / "that is a dog" / "that is my dog" — different statistical projections.
- "My ass" / "that is my ass" / "that is an ass" — even more divergent projections.
- If humans find a word highly ambiguous, the model likely has multiple strong statistical attractors and the wrong one may win.
- **Potentially the strongest novel research contribution.** Testable, requires no model internals access, practical implications for system design.
- **Measurable:** Create a corpus of terms rated by humans for ambiguity. Test whether model error rates correlate with those ratings.

---

## Research Foundation: Two Papers, One Argument

### Paper 1: Faith and Fate: Limits of Transformers on Compositionality

**Citation:** Dziri, N., Lu, X., Sclar, M., Li, X.L., Jiang, L., Lin, B.Y., Welleck, S., West, P., Bhatt, C., Bras, R.L., Hwang, J.D., Sanber, S., Le Bras, R., Chandu, K., Dziri, S., Sachan, M., Hajishirzi, H., Choi, Y. (2023). Faith and Fate: Limits of Transformers on Compositionality. *Advances in Neural Information Processing Systems (NeurIPS 2023, Spotlight).* [arxiv.org/abs/2305.18654](https://arxiv.org/abs/2305.18654)

**Core finding:** Transformers solve compositional tasks by reducing multi-step reasoning into linearized subgraph matching — pattern matching against computation fragments seen in training — without developing systematic problem-solving skills.

**Key metrics:**
- GPT-4: 59% accuracy on 3-digit multiplication, 4% on 4-digit
- Fine-tuning improves in-distribution, fails catastrophically out-of-distribution
- Theoretical proof: probability of incorrect predictions converges exponentially to ~1 as composition depth increases
- Error types: local (single-step), propagation (cascading), restoration (incorrect self-correction)

**Recommendations:** Accept approximate solutions, augment with planning modules, implement refinement methods.

### Paper 2: Prompt Repetition Improves Non-Reasoning LLMs

**Citation:** Leviathan, Y., Kalman, M., Matias, Y. (2025). Prompt Repetition Improves Non-Reasoning LLMs. *Google Research.* [arxiv.org/abs/2512.14982](https://arxiv.org/html/2512.14982v1)

**Core finding:** Repeating input prompts improves LLM performance on non-reasoning tasks (47 wins, 0 losses across 70 benchmark-model tests). The mechanism: causal masking means tokens can only attend backward, so tokens at the beginning of a prompt are positionally disadvantaged. Repetition gives every token a chance to attend to every other token.

**Critical observation:** Reasoning models trained with RL independently learn to repeat parts of the user's prompt as part of their reasoning process. The paper does not explain WHY this emerges but notes that prompt repetition becomes "neutral to slightly positive" when reasoning is already enabled — suggesting reasoning models already compensate for the same limitation.

**Key metrics:**
- 47/70 benchmark improvements with zero losses (non-reasoning)
- NameIndex task: 21.33% → 97.33% accuracy with repetition (Gemini 2.0 Flash-Lite)
- Repetition occurs in the parallelizable prefill stage — no output length or latency increase

### How these papers connect (not cited by either paper)

These papers were published independently (2023 and 2025) and do not reference each other. But they describe two sides of the same limitation:

**Dziri et al. (2023)** proves that transformers can't compose multi-step reasoning — they pattern-match against linearized subgraphs. Performance decays exponentially with composition depth.

**Leviathan et al. (2025)** proves that the one-pass causal masking limitation is real AND that repeated exposure compensates for it. The critical bridge: **reasoning models independently learn to repeat prompts through RL training.** This means the model's own optimization process discovers that re-exposure to input improves resolution quality. The model is learning to compensate for the architectural limitation that Dziri et al. identified.

**The implication for our methodology:** If RL-trained reasoning models independently learn that repeated structured exposure to the input improves output quality, then providing structured pre-read context (schemas, conflations, well-named fields) is doing externally what the model's own training pushes it to do internally. We are not fighting the architecture — we are pre-supplying the compensation mechanism the model would try to build for itself.

**Multiple exposure points support the schema methodology specifically:**
- The context-ordering-rule appears verbatim in every AGENTS file — repeated structural exposure
- The self-contained declaration appears in every schema — repeated isolation signal
- Well-named fields repeat their meaning in the name itself — no lookup composition needed
- The triage document re-states key concepts from the schemas in routing language — repeated framing
- Conflations provide the resolution pattern explicitly — the model gets the "repeated prompt" for ambiguous terms without needing to encounter them multiple times naturally

---

## How Faith and Fate Validates the Schema Family

**The schema family IS a planning module.** Dziri et al. recommend augmenting transformers with external planning structure. The schemas provide that — external structure read before acting, so the agent pattern-matches against the schema (good at) instead of composing governance from scratch (proven to fail at depth).

**Conflations are subgraph corrections.** Dziri et al. show transformers succeed when they've seen the computation pattern. Conflations inject the correct resolution pattern. We provide the right subgraph for pattern-matching to hit.

**Well-named fields reduce composition depth.** `parallel-authority-surface-class` is one pattern-match. `class` requires multi-step composition (what kind? → what context? → what meaning?). Dziri et al. prove transformers fail at exactly that depth. Longer specific names are EASIER for the model — they flatten composition to one step.

**Exit-condition-as-escape is linearized subgraph matching.** When an agent at step g jumps to an exit, it's matching a familiar pattern rather than composing unfamiliar remaining steps. The full-plan enumeration rule forces the complete subgraph to be visible before matching to an exit.

**"Approaches 0" leaves room for extreme misinterpretation.** Dziri et al. show correct prediction probability converges toward 0 but never reaches it. The gap is where the model produces something structurally coherent but semantically inverted. This is why blocked-until human-confirms gates exist.

**Repeated exposure through schema structure mirrors reasoning model behavior.** Leviathan et al. show reasoning models learn to repeat prompts. Our schemas provide repeated exposure to critical context (verbatim rules, self-contained declarations, well-named fields) — the same compensation mechanism, applied structurally rather than learned through RL.

---

## Where Our Findings May Diverge

Both papers study clean compositional tasks (multiplication, logic puzzles, benchmarks). Our domain is messier — governance, narrative, and migration are not clean computation graphs.

If agents compose novel governance solutions that work (fuzzy compositional success), that would be a meaningful counter-finding extending beyond both papers. If agents default to familiar patterns and need explicit structure (consistent with both papers), that extends the findings into a new applied domain.

The stress test: run the same governance task with and without the schema family. Measure whether the schema-aided agent produces structurally different (better-composed) solutions, or whether it produces the same pattern-matched solutions but with correct patterns to match against.

---

## Product Identity

**Working name:** AI Operating System Underlay

**Elevator pitch:** "We engineer what the model reads before it starts thinking, so its one-pass decisions land on the right meaning instead of the statistically easiest one."

**What it is:** A pre-tokenization context layer. Not a runtime. Not rules. The substrate that makes rules survivable across sessions, agents, and compression events.

**What it is not:** A better approach to rules. Rules are content. The underlay is the ground rules are written on. Rules fail when vocabulary resolves wrong (conflation), context compresses away (persistence), agents don't know rules exist (discoverability), or agents find an easier path (escape). The schema family addresses all four.

**Research framing:** An external planning module (per Dziri et al. 2023 recommendations) that exploits pattern-matching behavior rather than fighting it, combined with structured repeated exposure (per Leviathan et al. 2025 findings) to compensate for causal masking limitations.

---

## Operative Heuristic as a Schema-Wide Label

"Operative heuristic" should be applied throughout the schemas where statements function as behavioral directives rather than provable axioms. The label signals: "this is not a proof, it is a directive with a sound empirical basis that produces correct behavior regardless of edge cases."

Current candidates for the label:
- context-compression-warning (Claim 2)
- context-ordering-rule (Claim 1 — governance rule imposed against natural model behavior)
- sibling-isolation (empirically motivated, not provably necessary in all cases)
- The "ALWAYS" in Claim 2 specifically

The label prevents two failure modes:
1. An agent treating the statement as literally axiomatic and breaking when an edge case appears
2. A researcher dismissing the statement as "not technically proven" when the behavior it produces is demonstrably correct

---

## Why the Schema Still Serves a Purpose When Models Compensate Internally

Reasoning models learn through RL to repeat parts of the prompt during inference (Leviathan et al. 2025). This raises the question: if the model compensates internally, why provide external structure?

The schema does what the model tries to do for itself, but:
- **More precisely** — domain-specific context vs. statistically learned repetition
- **More cheaply** — input tokens are parallelizable prefill, not sequential reasoning tokens
- **More durably** — on disk, survives compression, re-readable across sessions
- **More specifically** — conflation maps, well-named fields, and verbatim rules are purpose-built resolution patterns, not training-data averages

The model's internal compensation is a general-purpose approximation. The schema is the purpose-built version of the same mechanism.

### Measurable token efficiency claim

Compare reasoning token usage on the same task with and without the schema. If the schema reduces reasoning token expenditure while maintaining or improving output quality:
1. Efficiency claim: fewer tokens to same result
2. Reliability claim: same or better output quality
3. Mechanistic explanation: the schema front-loads what the model would otherwise spend reasoning tokens building internally

This is clean, publishable, and requires no model internals access.

### Potential schema corrections informed by the research

The Leviathan paper shows input-side repetition is parallelizable and essentially free. The Dziri paper shows composition depth is the enemy. Combined implication: schemas should be less afraid of redundancy and more afraid of requiring agents to compose back to distant definitions.

Candidates for inlining (concepts referenced far from their definition):
- `blocked-until` semantics: defined once in meta, referenced by name throughout — agent at line 400 must compose back to line 47
- Auto-trigger conditions: multi-step composition in one block (condition + recovery + branching + recording)
- Edit-mode meanings: defined in one block, referenced by name in authoring-sequence without repeating what each mode means

These are optimizations, not structural fixes. The schemas work now. Inlining would reduce composition depth per the Dziri findings.

## Open Questions

- Does the ambiguity-predicts-error corollary generalize across models or is it model/tokenizer specific?
- Can the prompt repetition finding (Leviathan et al.) be deliberately exploited through schema structure, or is the benefit already captured by well-named fields and verbatim repeated rules?
- Is "AI Operating System Underlay" the right product name, or does "underlay" imply infrastructure dependency that doesn't exist?
- Could this scratchpad itself become a draft research paper outline without becoming a schema? (The impulse to formalize fires immediately. Both human and AI exhibited this. Same attachment pattern as gov2gov human-attachment-note, applied to the schema system itself.)
- What is the minimum sample size for the ambiguity-predicts-error study to be publishable?
- Does the schema family's effectiveness correlate with model capability (works better on stronger models) or inversely (helps weaker models more)?
