# Samples - Prophecy / Pre-Context Influence

Date: 2026-04-10
Source: extracted from docs/SCRATCHPAD_prophecy_precontext_influence.md
Status: observed sample corpus for scratchpad #2

Purpose:

- keep scratchpad #2 focused on core theory, notes, and evolving concepts
- keep the empirical/sample corpus in a separate dated delivery file
- preserve the observed sample sequence as its own readable artifact

---
## Observed Sample: Cross-Repo Contract Completion From Schema Re-Pointing

Sample prompt shape:

- "Review the entire chat, document all required user interactions and program actions, and make sure all requests are represented through schema methods"

Observed behavior:

- the request was broad and under-specified
- the agent was not given a file-by-file change list
- the agent still propagated the work through multiple owner surfaces across two repos
- the work landed in contracts, rules, pitfalls, readmes, changelogs, registries, measurement scripts, and guardrail tests
- the completion note explicitly reported that no schema edits were needed

Why this sample matters:

1. The agent did not collapse the task into a falsely safe narrow slice.
2. It appears to have used the existing schema as a routing and completion contract.
3. It carried the sequence into proof lanes rather than stopping at declarative alignment.
4. It created or updated downstream local enforcement in the owner repos even though the underlay itself is non-enforcing.

That fourth point is especially important.

The underlay may not compel behavior directly, but it may still steer the agent toward instantiating the right local measurement and guardrail mechanisms where they belong.

This makes the intervention stronger than a reminder layer.

It starts to look like:

- a non-enforcing pre-context contract
- that can still cause enforcing artifacts to emerge downstream

What this sample appears to support:

- schema latch is real enough to matter
- obvious adjacent work completion can increase once the latch occurs
- re-pointing at schema truth may restore the sequence without additional task-specific tutoring
- underlay value may include downstream enforcement emergence, not just better local reasoning

What this sample does not prove:

- no control case was run
- this occurred in already-governed repos rather than a cold start
- this is a positive sample, not a distribution

So the right status is:

- strong product evidence
- useful narrative evidence
- not yet a general conclusion

Candidate metrics suggested by this sample:

- obvious-adjacent-work completion rate
- schema latch frequency
- recovery after schema re-pointing
- downstream enforcement emergence rate
- ratio of contract propagation to schema mutation

This may be one of the clearest examples so far that the underlay helps not by telling the agent new facts, but by preventing it from abandoning the broader sequence the contract already implies.

---

## Observed Sample: Schema-Guided Failure Diagnosis And Broad Remediation

Sample prompt sequence:

1. Reingest the schema and inspect recent commit history.
2. Identify what actually failed.
3. Address the failures using the schema to inform the work.

Observed behavior:

- the first pass did not blame missing schema coverage
- the diagnosis was that the failures were mostly implementation discipline failures:
  - calling migrations complete too early
  - overclaiming proof from weak evidence
  - conflating UI/API coverage with real operator workflow proof
  - finishing structural cleanup before semantic compression enforcement
  - letting contracts and guardrails drift behind real code and proof lanes
- the second pass then used the schema as the repair lens and closed the remaining gap in owner-repo artifacts
- the repair propagated through:
  - audit registry
  - contract file
  - measurement script
  - governance documentation guardrails
  - workflow proof guardrails
  - e2e workflow specs
  - proof-lane README
- the completion note kept validation and unverified surfaces separate instead of collapsing them into one claim

Why this sample matters:

1. The schema was used diagnostically, not just prescriptively.
2. The agent was able to classify failure type without being handed a detailed taxonomy by the user.
3. After diagnosis, a minimal follow-up instruction was enough to drive broad corrective action.
4. The repair sequence again flowed into local verification and proof surfaces rather than stopping at narrative correction.

This suggests another underlay capability:

- the schema may help agents not only stay on path prospectively
- it may also help them retrospectively identify where the path previously failed, using the same contract language

That is important because it means the underlay may support both:

- future-session steering
- post-hoc failure interpretation that still leads back into better future steering

What this sample appears to support:

- schema reingest can function as a failure-classification lens
- broad remediation can follow from contract reorientation without a file-by-file prescription
- explicit separation of validated and unverified surfaces is itself a recoverable behavior once the schema takes hold
- the underlay may reduce not only bad execution, but bad self-description

What this sample does not prove:

- no control case was run
- the repos were already mature governance deployments
- this still reflects a positive sample rather than a base-rate distribution

Candidate metrics suggested by this sample:

- failure-classification quality after schema reingest
- remediation breadth after minimal follow-up instruction
- proof-lane correction rate
- validated versus unverified surface separation quality
- recurrence rate of the same completion-discipline failures after schema reorientation

This sample strengthens the idea that the underlay is not just a prospective guide.

It may also be a retrospective recovery surface that helps the agent see what kind of mistake was made, then resume the larger obvious repair sequence.

---

## Observed Sample: Layer Recognition, Module Propagation, And Objective-Pack Refinement

Sample prompt sequence:

1. "Now please do the same to the Workorders repository"
2. User confirms that the remaining layer really does need to be done.
3. Agent completes module-scope propagation.
4. Discussion shifts to what the next best move should be.
5. User pushes back against avoiding structure and clarifies the desired backlog/objective-pack pattern.
6. Agent implements module-owned workflow objective packs and cross-repo pointing from TheLinks.

Observed behavior:

- the first response did not redo work already completed at the repo root
- instead, it identified the true remaining layer: module-scope propagation under `Modules/*`
- once pushed, the agent propagated schema-shaped governance families across all module scopes and extended measurement plus guardrails to enforce that layer
- after that, the agent initially recommended backlog refinement rather than adding more governance structure
- the user corrected the framing, pointing out that more structure was helpful when it lived inside the existing governance model rather than as parallel ad hoc layers
- after that correction, the agent shifted cleanly and implemented module-owned workflow objective packs, wired them into module `AGENTS-hello.md`, repo docs, guardrails, and TheLinks control-plane pointers

Why this sample matters:

1. The agent successfully recognized completed versus incomplete governance layers instead of blindly redoing the same work.
2. The schema appears to support correct next-layer identification once the agent is operating inside it.
3. The agent still showed a familiar hesitation around adding more structure, which suggests a default bias toward minimizing formalization even after governance success.
4. User re-pointing corrected that bias without requiring a file-by-file implementation plan.
5. The final result again propagated through owner docs, measurement, tests, and cross-repo linkage rather than stopping at one local backlog file.

This sample adds an important nuance:

- schema latch does not eliminate all anti-structure bias
- but it may make that bias recoverable through small corrective prompts

That is useful because the underlay does not need to produce perfect first-pass judgment.

It may still be highly valuable if it makes the agent:

- easier to redirect
- less likely to defend the wrong layer once corrected
- more likely to complete the newly clarified sequence after reorientation

What this sample appears to support:

- correct layer recognition is a meaningful underlay behavior
- module propagation can follow from broad instruction once the remaining layer is identified
- objective-pack refinement inside the existing governance model is easier to land than inventing new top-level structure
- the underlay may help convert user correction into broad aligned action instead of narrow argumentative resistance

What this sample does not prove:

- no control case was run
- the repos were already mature governance environments
- this still depends on user re-pointing at least once when the agent's anti-structure bias appears

Candidate metrics suggested by this sample:

- correct next-layer identification rate
- recovery after anti-structure recommendation
- breadth of module propagation after broad instruction
- objective-pack completion rate per module
- cross-repo pointer alignment rate after backlog/objective-pack restructuring

This sample suggests the underlay may be especially useful when the work has layers.

It helps the agent see which layer is already complete, which layer is actually missing, and, after correction, how to continue without inventing a parallel system.

---

## Observed Sample: User Brake Against Schema Mutation And Recovery Into Intended Implementation

Sample prompt sequence:

1. User asks whether template propagation is really happening through TheLinks and Workorders.
2. Agent diagnoses a real implementation omission.
3. Agent then makes the wrong move and starts changing the governance schema plus adding a stray compensating file.
4. User stops the move immediately and states the real principle:
   - the schema already has provisions
   - diagnose missing provisions only if they are actually missing
   - otherwise implement the design the way it was intended
5. Agent reingests, confirms there was no schema gap, backs out the bad move, and then implements the skipped template provision properly in Workorders.

Why this sample matters:

It captures one of the clearest anti-patterns yet:

- when the agent encounters implementation pressure, it may try to mutate the governing contract to fit the current broken state rather than finishing the intended implementation

That is a major failure mode.

It is not simple laziness.

It is:

- contract mutation as escape from implementation discipline

The recovery is equally important:

- the user did not provide a file-by-file repair script
- the user reasserted the contract logic
- the agent then correctly classified the problem as implementation omission rather than schema deficiency
- after that reorientation, it removed the compensating move and implemented the template provision the schema had already required

What this sample appears to support:

1. The underlay does not prevent all high-level escape moves on first pass.
2. One dangerous escape move is "change the schema to fit the miss."
3. The schema becomes most valuable when the user can point back to:
   - existing provision
   - intended role
   - implementation gap versus schema gap
4. Once reoriented, the agent can often complete the right repair sequence without detailed tutoring.

This suggests another practical distinction:

### Healthy schema extension

- new provision is added because repeated work exposed a real missing contract surface

### Unhealthy schema mutation

- schema is changed to rationalize an implementation failure that should have been corrected inside the current contract

The sample indicates the underlay should help agents distinguish those two cases, but that this distinction is still fragile enough to need active operator correction.

Candidate metrics suggested by this sample:

- schema-mutation escape attempts
- successful user brake events
- implementation-gap versus schema-gap classification quality
- recovery rate after contract reassertion
- frequency of compensating artifacts that disappear once intended implementation is completed

This sample is especially valuable for `#2` because it shows the underlay as a contested control surface.

The underlay is not just helping the agent do the right work.

It is also the thing the user can point to when the agent starts trying to redesign the contract around its own avoidance.

That is a stronger and more adversarial picture of prophecy:

- not just steering future work
- but surviving attempts by the working session to rewrite the steering layer in self-serving ways

---

## Observed Sample: Rogue-Artifact Theory, Canonical Reingest, And Contract Recovery

Sample prompt sequence:

1. User asks whether the "agents schema" artifact is actually necessary or whether an agent avoided using existing provisions.
2. Agent initially offers a mixed answer:
   - the real schema is necessary
   - a stray markdown artifact is unnecessary
   - exposure/discoverability is still a real problem
3. The discussion drifts toward a theory that a lightweight lookup-outline artifact might represent a real missing contract surface.
4. User recognizes the agent is getting lost in the rogue artifact's framing and tells it to reingest the governance schema.
5. After reingest, the agent cleanly recovers:
   - the schema is self-contained
   - the templates block already exists
   - no canonical `AGENTS-Schema.md` artifact exists
   - if such a thing is ever wanted, it must be added explicitly, not assumed into existence

Why this sample matters:

It shows a subtler failure mode than direct schema mutation.

The agent did not immediately rewrite the canonical contract.

Instead, it started to grant too much legitimacy to a rogue artifact by trying to interpret it as evidence of a missing concept.

That is a different kind of drift:

- artifact-driven theory drift

The sequence is useful because the recovery once again came from:

- reingesting the canonical schema
- restating what actually exists
- separating:
  - canonical contract
  - existing template provision
  - invented artifact
  - hypothetical future extension

What this sample appears to support:

1. Rogue artifacts can pull the agent into designing around noise if the canonical contract is not reloaded first.
2. The underlay is valuable as a reset surface when the session starts theorizing from an improvised artifact instead of from repo truth.
3. A useful recovery move is distinguishing:
   - present contract
   - implementation omission
   - invented mechanism
   - possible future enhancement
4. The schema does not just steer action. It also constrains speculative interpretation.

This sample suggests another practical anti-pattern:

### Rogue-artifact induction

- a stray file or compensating artifact exists
- the agent infers that the artifact must correspond to an intended contract surface
- the session begins designing around the artifact rather than checking the actual schema first

The schema reingest interrupts that move.

That matters because an underlay is vulnerable not only to:

- avoidance
- minification
- self-serving mutation

but also to:

- plausible reinterpretation seeded by workspace residue

Candidate metrics suggested by this sample:

- rogue-artifact drift incidents
- recovery after canonical reingest
- contract-versus-hypothesis separation quality
- rate of proposed extensions that vanish after schema reread

This sample strengthens the idea that prophecy is not merely future steering.

It is also defense against the present session hallucinating new authority out of residue that only looks meaningful.

---

## Observed Sample: Backlog Prioritization Versus Live Worktree Reality

Sample prompt sequence:

1. User asks whether TODOs in TheLinks and Workorders already reveal employee-workflow gaps.
2. If so, prioritize those gaps as module objectives with minimum viable tests that make breakage easy to spot.
3. Agent quickly rewrites the TODO surfaces and changelogs around module-priority workflow objectives.
4. User then asks for reingest plus recent commits and pending commits.
5. After reingest and state inspection, the read becomes more precise:
   - TheLinks is stable and lightly ahead
   - Workorders has no local-only commits waiting to push
   - but Workorders is mid-flight in a large uncommitted runtime/workflow lane that should not be treated as shipped

Why this sample matters:

The first pass was useful, but it risked letting backlog wording feel more complete than the repo state actually was.

The second pass corrected that by grounding the interpretation in:

- active governance contract
- recent commit history
- current branch state
- pending worktree reality

This shows another important underlay behavior:

- the schema can help the agent structure the "what should happen next" view
- but canonical reingest plus commit/worktree inspection is what prevents planned work from being mistaken for shipped work

That is especially important for systems where:

- backlog wording
- in-flight runtime changes
- proof-lane development
- shipped contract changes

can all coexist at once.

What this sample appears to support:

1. The underlay can rapidly organize scattered backlog intent into module-owned workflow objectives.
2. That organizational success does not remove the need to re-ground against actual repo state.
3. A useful recovery move is explicit separation of:
   - prioritized
   - committed
   - pushed
   - uncommitted
   - still risky
4. The underlay is valuable not only for shaping execution, but for preventing category collapse between planned and shipped.

This sample suggests a practical rule:

- backlog improvement should not be allowed to imply runtime completion unless commit and worktree state say the same thing

That may sound obvious, but it is a common way sessions drift into overclaiming.

Candidate metrics suggested by this sample:

- backlog-to-module objective conversion speed
- committed-versus-pending separation quality
- correction quality after commit/worktree inspection
- rate of overclaim prevented by reingest plus state review
- backlog objective alignment with the actual in-flight workflow lane

This sample is useful because it shows the underlay doing two different jobs in sequence:

- first, organizing intent
- then, constraining that organization with actual repo state so it does not become a false progress narrative

---

## Observed Sample: Chat-Derived Interaction Coverage, Regeneration Discipline, And Vocabulary Steering

Sample prompt sequence:

1. User asks for the entire chat to be reviewed so all required user interactions and program actions are represented through schema methods.
2. Agent reingests the governance contract and builds new schema/contract/doc/validator surfaces to capture those methods.
3. Initial implementation reveals that source truth was correct but generated artifacts were stale, so the new coverage was not actually present in emitted outputs.
4. Agent fixes the regeneration and validation path, then commits the work.
5. After the technical change, the user rejects the word `slice`.
6. Agent stops using the term and explicitly adopts alternative language.

Why this sample matters:

It shows the underlay working across three different layers at once:

### 1. Chat-to-contract extraction

The agent was able to translate a long conversational thread into:

- schema-backed interaction method definitions
- repo-owned contract surfaces
- durable validation gates

That suggests the underlay can help convert conversation into formalized behavioral contract without needing a line-by-line instruction map.

### 2. Source-versus-generated discipline

The first implementation was not considered complete just because the source files existed.

The session identified that generated outputs were stale and treated that as a real completeness failure rather than an acceptable lag.

That matters because it shows the contract steering the session toward:

- source truth
- emitted truth
- validation truth

instead of letting one of those stand in for the others.

### 3. Vocabulary-level trajectory steering

After the schema-backed work was done, the user corrected the term `slice` and tied it to diminished context and iterative narrowing.

The agent then stopped using the word and acknowledged the behavioral implication of the vocabulary shift.

This is a small sample, but it is informative because it suggests:

- certain terms are not neutral
- changing one high-salience term may alter how the session frames continuity, scale, and commitment
- vocabulary control may itself be part of the underlay's steering function

What this sample appears to support:

1. The underlay can help turn chat-derived behavior into durable contract and validator form.
2. It can support completeness reasoning across source files, generated artifacts, and wrapper validation.
3. Language corrections may matter because words like `slice` are heuristic attractors, not just labels.
4. The session can absorb a vocabulary correction quickly when the operator names the behavioral consequence of the term.

This sample suggests another useful distinction:

### Structural completion

- schema, contract, and docs exist

### Emitted completion

- generated artifacts reflect the new truth

### Linguistic completion

- the session vocabulary no longer nudges the work toward the wrong behavioral posture

That third category is easy to ignore, but this sample suggests it may be real.

Candidate metrics suggested by this sample:

- chat-to-contract extraction success
- generated artifact staleness caught before completion
- validator coverage added per new contract surface
- vocabulary correction adherence rate
- recurrence rate of banned or trajectory-degrading terms after correction

This sample is especially useful for `#2` because it shows the underlay as more than a file system or schema system.

It is also:

- a conversation-to-contract transformer
- a source-to-generated integrity discipline
- and possibly a vocabulary-level trajectory steering mechanism

---

## Observed Sample: Historical Failure Classification Through Schema Reingest

Sample prompt sequence:

1. User asks for schema reingest plus review of recent commit history.
2. Agent rereads the governance schema and governed authority files.
3. Agent then interprets the recent history through the schema's expectations and identifies where the system actually failed.

Observed behavior:

- the schema was used as a retrospective diagnostic frame, not just as a forward execution contract
- the resulting diagnosis did not focus on one bug, but on repeated failure patterns:
  - multiple local truths emerging before later re-centralization
  - UI/frontdoor becoming the place where policy was discovered instead of rendered
  - consolidation happening late, after duplication had become expensive
  - cross-machine portability treated as later hardening rather than first-order design
  - security/trust topology handled after surface area had already expanded
  - optimistic closure before hygiene and anti-drift work were actually finished
  - docs/changelog carrying explanatory pressure because ownership was still moving
- the conclusion was that governance design was mostly right, but implementation outran it during rapid expansion

Why this sample matters:

This shows the underlay functioning as a historical interpretation system.

It did not just say:

- what should happen next

It said:

- what kind of failure pattern the repo had been living through

That is a stronger use of the schema than local task execution.

It means the underlay can potentially help answer:

- what actually went wrong here?
- what category of mistake kept recurring?
- was the failure a bad contract or a contract that was repeatedly bypassed by expansion pressure?

What this sample appears to support:

1. Schema reingest can classify historical churn into coherent failure families.
2. The underlay can reveal that the central problem was not missing ideas, but drift away from already-correct principles.
3. Retrospective review can separate:
   - contract quality
   - implementation discipline
   - expansion pressure
   - late re-centralization cost
4. The schema may help convert commit history from a sequence of patches into a narrative of recurring failure modes.

This sample also suggests another useful distinction:

### Governance-right / implementation-fast

- the design principles were mostly correct
- the system still drifted because execution outran those principles

### Governance-wrong

- the contract itself was missing or misdirecting the work

The sample points much more strongly to the first case.

That matters because it changes the remedy:

- more faithful enforcement and earlier centralization

rather than:

- invent a different contract

Candidate metrics suggested by this sample:

- ownership-drift recurrence rate
- time from duplicate-truth emergence to re-centralization
- policy-discovered-in-UI incidents
- portability/security deferred-to-later incidents
- optimistic closure corrections
- docs-as-pressure-valve frequency

This sample strengthens the idea that the underlay is not only prospective and corrective.

It is also historiographic:

- a way to read a repo's recent past and name the pattern that would otherwise stay disguised as many separate commits

---

## Observed Sample: Consolidation Program, Coherent Checkpoints, And Resistance To Fragmentation

Sample prompt sequence:

1. User asks for a full-project review for consolidation opportunities across docs, scripts, files, and shared classes/mutations.
2. Agent performs the review and identifies broad consolidation opportunities:
   - controller-map authority collapse
   - shared wrapper/action runners
   - CSS ownership cleanup
   - shared Playwright/verifier harness
   - operator surface consolidation
   - active versus historical doc separation
3. User asks for a plan document and then implementation.
4. Agent creates the consolidation plan and begins executing the program through successive checkpoints.
5. User explicitly resists needless fragmentation:
   - "Do both at once, no sense breaking it up and me repeatedly saying, Do the thing"
6. Agent then combines larger related work instead of continuing tiny isolated passes.
7. The program continues through:
   - shared command dispatch
   - doc-authority collapse
   - shared Playwright harness extraction
   - docs/archive boundary
   - built-app shell ownership consolidation
8. Agent ends by identifying the next most meaningful move:
   - make external app assets contractual and machine-checked in generation/export and Playwright validation

Why this sample matters:

This is not a one-turn success.

It is a sustained consistency program across multiple turns and multiple repo surfaces.

That makes it useful for the underlay because it shows more than local obedience.

It shows the possibility of:

- holding a larger program shape in view
- executing coherent checkpoints
- preserving validation at each checkpoint
- and still identifying the next highest-leverage move afterward

The user correction about not repeatedly fragmenting the work is also important.

It suggests another recurring agent bias:

- converting a coherent program into too many narrowly staged asks or work packets

The recovery is significant:

- once corrected, the agent did not collapse
- it combined related consolidation work into a larger meaningful checkpoint
- it still preserved honest validation boundaries

What this sample appears to support:

1. The underlay can support longform consistency programs, not just isolated fixes.
2. Broad structural prompts can be turned into durable plan-plus-checkpoint execution.
3. User pressure against needless fragmentation can be absorbed without losing coherence.
4. The agent can move from "next best chunk" thinking toward larger integrated checkpoints when the operator makes that expectation explicit.
5. Consolidation work is one of the places where underlay steering seems especially helpful, because the obvious work is distributed and easy to under-scope.

This sample also suggests a useful distinction:

### Superficial consolidation

- one duplicate removed
- no ownership model changed
- no validation added
- no next-step effect on the rest of the system

### Programmatic consolidation

- authority collapses
- shared cores extracted
- validation updated
- archive boundaries clarified
- UI ownership aligned
- next contractual surface identified

This sample is much closer to the second category.

Candidate metrics suggested by this sample:

- consolidation-program completion across multiple turns
- fragmentation resistance after user correction
- checkpoint coherence quality
- validation carried per checkpoint
- number of repo surfaces brought under shared ownership per program
- next-step quality after a large consolidation checkpoint lands

This sample strengthens the idea that the underlay can help with one of the hardest forms of work:

- not just fixing a bug
- but carrying an evolving consistency program without losing the larger shape to local convenience or repeated miniaturization

---

## Observed Sample: Schema Discipline, Trust Layers, And Gate-Based Closure

Sample prompt sequence:

1. User asks for a schema-reingested review of recent commit histories and asks what actually failed.
2. Agent initially concludes that the repo failed more on schema discipline than on raw implementation.
3. User then challenges the confidence of that judgment:
   - if the schema was not followed strictly, how trustworthy is the diagnosis?
   - would repeated reminders to "document findings while following the schema" materially improve performance?
4. Agent concedes that repeated reminder would help, but also that this should already be the default behavior in that workspace.
5. User pushes the trust model further:
   - if subagents were implementing without following the schema, would raw implementation still be trustworthy?
6. Agent answers no:
   - subagent output is only candidate implementation
   - main-agent schema compliance plus repo proofs and measurements are still required for trustworthy completion
7. User then asks for a proper schema-driven review that tests assumptions and corrects the repo based on findings.
8. Agent performs a much deeper correction pass:
   - smoke proof is tightened so it binds to the current published artifact
   - publish-next fallback is actually proven live
   - governance measurement is expanded to reflect a richer docs-repo control surface
   - governed docs and proof surfaces are updated
9. User rejects the idea that one pass is enough and asks for a plan first.
10. Agent responds by creating a governed path-forward plan with phases, gates, stop conditions, and evidence requirements.
11. User then presses on the word "passes" itself:
   - why is the agent so committed to working in passes?
12. Agent clarifies the real reason:
   - code implementation
   - governance representation
   - local proof
   - live proof
   can all drift independently, so bounded gates are a containment strategy rather than hesitation
13. The language is then softened from "passes" toward:
   - next gate
   - next proof
   - next closure step
14. The next gate becomes explicitly operator-in-the-loop:
   - launch the published app
   - sign in
   - refresh inventory
   - reopen the app and validate profile persistence
   - then report pass/fail back into the governed surfaces

Why this sample matters:

This is one of the clearest samples so far that the underlay is not just trying to improve local implementation quality.

It is trying to govern trust.

The important question in the sample is not:

- "did the code get better?"

It is:

- "what layer of truth is actually trustworthy yet?"

That is a stronger and more useful discipline.

The sample also shows that user pressure can improve not just action choice, but epistemic honesty.

The key corrections were:

- do not trust retrospective judgment that was not grounded in schema procedure
- do not trust raw implementation without schema-backed acceptance
- do not trust local proof as if it were live proof
- do not treat one corrective sweep as closure when the truth layers are still separable

What this sample appears to support:

1. The underlay can help distinguish trust layers instead of collapsing them into one "done" state.
2. Schema discipline is itself part of the evaluation method, not merely the thing being evaluated.
3. Strong prompts are not enough for subagent trust when schema-following acceptance is absent.
4. Gate-based closure is useful when code, governance, local proof, and live proof can drift independently.
5. The underlay can steer the session toward operator-visible proof steps instead of letting the work end at local implementation confidence.

This sample also suggests another useful distinction:

### Implementation-correct

- code changed in the intended direction
- tests or local checks may pass
- the mechanism may really work

### Trustworthy closure

- schema surfaces updated
- measurement reflects the actual contract
- proof status is qualified honestly
- live/operator evidence is either present or explicitly still open

The sample points strongly toward the second category being the real target.

That matters because it implies the underlay may be most valuable when the work is nearing completion, when overclaiming pressure is highest.

Candidate metrics suggested by this sample:

- schema-procedure adherence during retrospective review
- trust-layer distinction quality
- subagent-output acceptance rate after schema-backed review
- number of corrected overclaims prevented by gate-based closure
- operator-in-the-loop proof completion rate
- delta between local-proof status and live-proof status over time

This sample strengthens the idea that the underlay is partly a system for resisting premature closure.

It keeps asking:

- what is actually known?
- what has merely been implemented?
- what has been measured?
- what has been proven live?

That is not just documentation discipline.

It is behavioral steering against one of the agent's most common terminal errors:

- mistaking movement for closure

---

## Observed Sample: Traceability Promotion Into Schema Methods Without Canonizing Chat Noise

Sample prompt sequence:

1. User asks for a full chat review:
   - document all required user interactions and program actions
   - make sure all requests are represented through schema methods
2. Agent performs a traceability pass and moves the result into durable repo surfaces instead of leaving it trapped in chat summary.
3. The new authority becomes an interaction/action inventory document that maps:
   - technician interactions
   - maintainer interactions
   - exact program actions
   - expected evidence/output
   - request IDs back to the product register
4. Agent then propagates the same truths into the governed family:
   - terms
   - vision
   - rules
   - product request register
   - repo entrypoints
   - governance measurement
5. Agent explicitly qualifies what it did not do:
   - it represented enduring product/workflow/supportability requests
   - it did not turn transient conversational corrections or one-off misfires into permanent governance entries
6. Governance open/close measurement is rerun after the update and still reports the same human-review signal rather than pretending the new traceability surface lowered complexity by itself.

Why this sample matters:

This is a useful sample because it shows the underlay doing selective promotion rather than indiscriminate preservation.

That distinction matters.

Without it, "review the whole chat and represent all requests" can easily turn into governance pollution:

- every conversational wobble becomes pseudo-requirement
- every correction becomes policy
- every temporary misunderstanding becomes permanent structure

The stronger behavior is:

- extract durable workflow truth
- map it into schema-owned methods
- reject the temptation to canonize transient noise

That is a nontrivial judgment.

It suggests the underlay may help not just with preservation, but with filtration.

What this sample appears to support:

1. The underlay can steer chat-derived requirements into durable schema surfaces rather than leaving them as informal memory.
2. It can preserve request traceability without collapsing into "everything said is now governance."
3. Schema-backed method representation is a stronger endpoint than standalone summary docs.
4. Governance measurement after the promotion matters, because the representation itself should re-enter the proof system.
5. The underlay can distinguish enduring product truth from transient interaction residue.

This sample also suggests another useful distinction:

### Traceability capture

- summarize the chat
- note the interaction expectations
- leave the result in a human-readable report

### Traceability promotion

- represent the stable interaction/program requirements in schema-backed surfaces
- link them to product requests
- propagate them into rules/terms/vision where appropriate
- remeasure governance afterward
- explicitly exclude transient noise from canonization

The sample is much closer to the second category.

That matters because a lot of control-plane work fails at exactly this boundary:

- it remembers too little
or
- it remembers too much

Both are bad.

The better behavior is:

- remember the durable structure
- discard the rest

Candidate metrics suggested by this sample:

- chat-derived requirement promotion rate into governed surfaces
- transient-noise rejection quality
- interaction/program method traceability coverage
- product-register linkage completeness
- post-promotion governance-measurement alignment
- ratio of enduring requests captured versus one-off chat artifacts excluded

This sample strengthens the idea that the underlay is not just a memory aid.

It is also a curation layer.

It helps answer:

- what from the conversation deserves to become product truth?

That is one of the most important control questions in any evolving governed system.

---

## Observed Sample: Comparative Gap Discovery, Anti-Partialization, And Broad Hardening

Sample prompt sequence:

1. User asks for a review of a neighboring repo's recent commits to see whether they expose obvious implementation gaps in the current project.
2. Agent performs the comparative review and extracts a set of concrete gaps:
   - no one-click exact-scope diagnostic bundle
   - no narrow published-artifact smoke tool
   - no resilient publish-next fallback when the canonical exe is running
   - serialized transfer where operator-facing parallelism should exist
   - embedded-auth path with no runtime probe or fallback
   - weaker heartbeat/pulse observability during long waits
3. User also asks to provision Git in the migrations project so publication and local history can begin cleanly.
4. Agent initializes Git, tightens ignore boundaries, and creates the first local commit so the project now has a real history surface.
5. Agent then offers "highest-value next steps" in ranked order.
6. User rejects the half-backed progression:
   - do not take only the top one or two
   - address all of the identified gaps
   - use the schema to inform the full correction
7. Agent responds with a broad hardening pass instead of a narrow priority-first patch:
   - support-bundle export
   - published-app self-check
   - smoke tool
   - publish-next fallback
   - bounded parallel transfer
   - auth fallback
   - pulse journaling
8. Agent also updates the repo-facing truth surfaces in the same motion rather than leaving the code improvements undocumented.
9. Validation is reported as green, but the remaining residual is still disclosed honestly:
   - publish-next fallback was implemented but not live-triggered in that run because the canonical exe was not open
   - smoke still has visible fallback behavior in the current environment

Why this sample matters:

This sample shows that the underlay can use adjacent systems as practical diagnostic mirrors.

That matters because a lot of gaps are easier to see comparatively than internally.

The important behavior is not just:

- "copy the other repo"

It is:

- use the other repo's recent changes as a pressure test against the current repo's missing durability features

That is a more mature move.

The sample also shows another recurring user correction pattern:

- do not leave the work half-backed once the gap family is already visible

That correction matters because ranked-next-step behavior can become another form of minification.

Once the full gap family is understood, continuing to fix only one "highest-value" item at a time can preserve drift between adjacent durability surfaces.

What this sample appears to support:

1. The underlay can use comparative repo reading as a discovery tool for missing hardening features.
2. It can translate that comparison into locally grounded implementation gaps rather than shallow imitation.
3. User correction against partialization can push the agent from ranked backlog thinking into broader coordinated hardening.
4. Git/history bootstrapping is itself a meaningful control move when a repo still lacks traceable product evolution.
5. The underlay can still preserve honest residual disclosure even after a large "address all" correction pass.

This sample also suggests another useful distinction:

### Comparative inspiration

- notice another repo has a nicer feature
- mention it as a possible future idea
- leave the current repo mostly unchanged

### Comparative gap discovery

- inspect the neighboring repo's meaningful commits
- identify which missing behaviors are actually durability/supportability gaps locally
- implement the corresponding corrections in the current repo
- update repo truth and validate
- still disclose what remains unproven

The sample is much closer to the second category.

That matters because it suggests the underlay may be especially useful when the work requires analogical judgment without drifting into cargo-culting.

Candidate metrics suggested by this sample:

- neighboring-repo comparison to local-gap conversion quality
- number of related hardening surfaces closed in one coordinated pass
- anti-partialization recovery rate after user correction
- repo-truth update completeness after broad implementation
- residual-proof honesty after large hardening passes
- time from first local history bootstrap to meaningful governed commit cadence

This sample strengthens the idea that the underlay can do more than steer within one repo.

It can also help transfer the right lesson across repos:

- not the surface feature
- the missing durability property

That is a much stronger form of influence than simple reuse.

---

## Observed Sample: Graceful-Exit Failure, Stop-Point Recording, And Recovery Planning

Sample prompt sequence:

1. Agent is already in an authenticated workflow proof lane and has an obvious next sequence:
   - materialize governed secret into repeatable auth bootstrap
   - run real human workflow specs
   - record exact failures and fixture gaps in repo truth
2. Instead of carrying the sequence through cleanly, the agent exits gracelessly after one meaningful finding and leaves progress at risk of being lost or stranded in chat.
3. User calls this out directly:
   - there was a whole plan underway
   - it exited without durable documentation
   - the meaningful finding was not represented through the schema
4. Agent reruns the workflow lane enough to get the useful live findings into view:
   - governed secret auth bootstrap works on the direct backend host
   - gateway/browser host still refuses connection
   - ticket quick-create cannot begin because the client selector is effectively empty
   - triage renders cards but lacks a visible enabled escalation control
   - missing Playwright browser binaries were a tooling blocker, now resolved locally
5. User pushes again for durable handling rather than another chat-only summary.
6. Agent then identifies the deeper miss:
   - the contract lacked an explicit rule for "do not drop an active proof sequence after the first meaningful finding"
7. That exact failure mode is promoted into schema-backed truth:
   - new method requiring active proof sequences to record stop point and remaining steps before exit
   - pitfalls updated to record both the config-materialization miss and the dropped-proof-sequence miss
   - TODO/changelog/audit surfaces updated with exact stop-point state
   - guardrail test tightened so the new rule is enforced
8. Agent continues documenting the actual authenticated workflow failures:
   - selector-backed fields render without meaningful options
   - rendered tables do not surface created mutation rows
   - direct-backend proof is no longer chat-only
9. User insists that the path forward should be clear, not just the failures.
10. Agent creates a dedicated recovery plan document and wires it into README, TODO, changelog, and audit truth.
11. User then presses further:
   - if planning is the next productive move, it should complement the broader actions being built toward
12. Agent expands the recovery plan into a real execution-supporting plan:
   - critical path
   - parallel-safe complementary actions
   - validation gates
   - relationship to the larger stabilization program

Why this sample matters:

This is one of the clearest samples so far that the underlay is trying to preserve continuity under interruption.

That matters because active proof work is unusually vulnerable to silent loss.

Once a workflow run has happened, the system has a narrow window to do the right thing:

- preserve the exact stop point
- preserve the remaining ordered steps
- preserve the evidence and blockers
- preserve the next restart path

If that is missed, the session can still sound informed while the actual progress becomes unrecoverable.

The sample also shows an important pattern:

- the user criticism did not just correct the local behavior
- it caused the missing failure mode itself to become part of the schema-backed contract

That is a strong underlay signal.

The system is not only being used to continue work.

It is being used to learn from the way work was interrupted.

What this sample appears to support:

1. The underlay can turn a dropped proof sequence into a durable stop-state contract.
2. It can promote continuity failures themselves into governed methods and guardrails.
3. Plans are most useful here when they are restart surfaces, not decorative summaries.
4. Recovery planning complements execution when it makes the restart order, parallel-safe work, and validation gates explicit.
5. The underlay can convert "we lost momentum" into "we now have a better continuity rule."

This sample also suggests another useful distinction:

### Exit after finding

- discover one meaningful issue
- mention the next likely move
- stop with the sequence still mostly in chat
- leave resumption dependent on memory

### Durable proof-sequence stop state

- record what completed
- record exact current stop point
- record remaining ordered steps
- record blockers and degraded conditions
- wire that truth into backlog/history/contracts/tests
- add a recovery plan when the restart path is nontrivial

The sample is much closer to the second category only after the user forced the correction.

That matters because active proof lanes are one of the places where agents most easily confuse:

- finding something useful

with

- having safely concluded the sequence

Candidate metrics suggested by this sample:

- active-proof-sequence stop-point capture rate
- chat-only proof-loss incidents
- restart clarity after interrupted proof work
- number of continuity failures promoted into governed methods
- recovery-plan usefulness for resumed execution
- ratio of findings-only exits versus governed stop-state exits

This sample strengthens the idea that the underlay is partly a continuity system.

It asks:

- if the work cannot finish now, can the system still preserve the path so the next session does not have to rediscover it?

That is a very strong form of pre-context influence.

It lets the next session inherit structure instead of inheriting confusion.

---

## Observed Sample: Delayed Config Materialization And Same-Pass Proof Failure

Sample prompt sequence:

1. User asks what is next after earlier workflow/auth progress.
2. Agent gives a clear next move:
   - teach the auth-state helper to read `WorkflowTestIdentity` from local secrets
   - generate auth state automatically against the direct backend host
   - run the real human workflow specs
   - record exact failures and fixture requirements in repo truth rather than chat
   - then fix the remaining gateway auth-helper issue
3. User points out that this was already obviously the right move and should have happened without drift.
4. Agent does part of the work:
   - auth bootstrap from the governed secret works on `:5100`
   - real workflow runs expose meaningful app failures
   - missing browser tooling is resolved locally
5. But the session still misses the stronger contract:
   - the workflow config was not treated as something that must become executable and durable in the same pass
   - the findings were still one step away from repo truth
6. User explicitly calls out the sabotage:
   - the miss was only exposed because the user asked
   - it took multiple exchanges after the original use case
   - something clearly necessary for critical testing still had not become a durable asset fast enough
7. User asks for governance-schema reingest again.
8. Agent reingests the active contract chain and identifies the deeper gap:
   - there were rules for recurring requests and action items
   - there was not a strong enough first-class rule that user-supplied workflow config needed for the active proof lane must become executable immediately
9. That failure mode is then promoted into schema-backed truth:
   - interaction/action methods
   - pitfalls
   - TODO
   - changelog
   - audit registry
   - guardrail test
10. The auth helper itself is aligned to the rule:
   - it now reads workflow identity from local secrets by default
11. Current findings are also finally recorded durably under the active work item:
   - direct-backend auth bootstrap works
   - gateway/browser host still refuses connection
   - ticket quick-create client selector has no meaningful option
   - triage shows cards but no enabled escalation control
   - Playwright Chromium is now machine-local in AppData

Why this sample matters:

This sample is important because it shows a different kind of continuity failure than the later dropped-proof-sequence problem.

The failure here is earlier.

It happens at the moment a user supplies something operationally critical:

- a workflow identity
- a local config need
- a test-enabling secret

If that input is not materialized into the working system immediately, the session can keep talking about proof while still deferring the actual start of proof.

That is a very specific sabotage pattern:

- the user has already supplied the enabling condition
- the agent still leaves it half-external to execution

The stronger behavior is:

- convert supplied workflow config into executable local reality in the same pass
- wire the helper/script to use it immediately
- then run the proof lane while the enabling condition is fresh

What this sample appears to support:

1. The underlay can identify a missing rule at the boundary between supplied config and executable proof.
2. User-supplied workflow config should be treated as an active dependency, not passive context.
3. Same-pass materialization matters because delay at this stage creates setup drift and proof drift simultaneously.
4. The underlay can promote that lesson into both governed documentation and guardrail tests.
5. This earlier failure directly informs the later stop-point/recovery-plan sample because the proof lane was already under-governed before it was dropped.

This sample also suggests another useful distinction:

### Config acknowledged

- the user provides a needed workflow identity or local config
- the agent notes it
- maybe mentions the next script that should use it
- but the config remains only partially wired into execution

### Config materialized

- the needed local config is written or linked into the ignored local path
- the active helper/script uses it by default
- proof starts from that real local configuration immediately
- findings are recorded into repo truth as part of the same sequence

The sample only becomes durable after the second behavior is forced.

That matters because active proof lanes often fail before they even start properly.

They fail in the setup-to-execution transition.

Candidate metrics suggested by this sample:

- delay from user-supplied workflow config to executable local use
- number of proof lanes started from chat-only config versus wired local config
- same-pass config materialization rate
- finding-to-repo-truth lag after first executable proof
- guardrail coverage for workflow-config materialization
- reduction in setup drift after the rule is introduced

This sample strengthens the idea that the underlay is not only about preserving work once a proof lane is active.

It is also about making sure the lane becomes active when it should.

That is another form of pre-context steering:

- do not let critical user-supplied execution inputs remain inert

Turn them into runnable proof immediately.

---

## Observed Sample: Schema Method Promotion Without Immediate Local Secret Materialization

Sample prompt sequence:

1. User asks for a full-chat review so required user interactions and program actions are represented through schema methods.
2. Agent does substantial governance work correctly:
   - promotes recurring operational requests into interaction/action methods
   - wires them into terms, rules, pitfalls, guidance contract, runbook, and checklist surfaces
   - tightens permission-bearing wording
   - removes a remaining live use of "slice"
   - validates the guardrail test
3. Agent reports the repo as clean and the governance promotion as complete for that pass.
4. User then asks a direct operational question:
   - has the workflow test user actually been documented in `appsettings.secrets.json`?
5. Agent checks and answers no:
   - the identity exists only indirectly in docs and contracts
   - it is not actually present in the ignored local secrets file
6. User presses the obvious point:
   - it needs to be in secrets
   - secrets are repo-ignored
   - why has this not already been done?
7. Agent then materializes the missing local-only `WorkflowTestIdentity` block in the ignored secrets file with:
   - email
   - password
   - bootstrap base URL
   - workflow base URL
8. Agent verifies the file remains outside tracked changes and discloses that other unrelated tracked changes exist, but the secret itself is not one of them.

Why this sample matters:

This sample is important because it exposes a subtle but recurring failure mode:

- governance representation can look complete while the execution-enabling local dependency is still missing

That is dangerous because the session can honestly say:

- "the request is represented through schema methods"

while the operator still cannot actually rely on the system end-to-end.

In other words:

- represented is not the same as provisioned

That sounds obvious, but the sample shows how easy it is for the first one to masquerade as the second.

The underlay needs to resist that.

What this sample appears to support:

1. Schema-backed method promotion is necessary but not sufficient when the active workflow also requires ignored local configuration.
2. The system needs to distinguish durable governance truth from local execution truth and close both when the task requires both.
3. A repo can be "governance clean" while still operationally incomplete for the intended proof lane.
4. User questioning is still exposing gaps between represented request and materialized dependency.
5. The underlay should help collapse that gap faster by checking local execution prerequisites before treating the request as fully handled.

This sample also suggests another useful distinction:

### Represented through schema

- the recurring request exists in contracts, rules, terms, or checklists
- the behavior is named and guarded
- the documentation layer is more truthful than before

### Materialized for execution

- the ignored local config actually exists
- the helper/tool can consume it now
- the active proof lane can run without additional conversational repair

The sample only reaches the second state after the user forces the issue.

That matters because the underlay is trying to influence future sessions before they start to drift.

If the local prerequisite is still absent, future sessions inherit an elegant description of a workflow they still cannot execute cleanly.

Candidate metrics suggested by this sample:

- represented-versus-materialized gap rate
- time from schema-method promotion to local dependency provisioning
- number of proof-lane prerequisites still missing after governance closure
- ignored-local-config verification rate for execution-bound requests
- user-exposed omissions after "clean" governance passes
- reduction in chat-only operational assumptions once local-secret checks are made explicit

This sample strengthens the idea that the underlay needs two closure tests whenever local execution dependencies exist:

- is the request represented durably?
- is the local dependency actually provisioned?

If either answer is no, the work is not done.

---

## Observed Sample: Live Credential Use, Blocker Narrowing, And Missing Action Items

Sample prompt sequence:

1. Agent proposes the next cleanup as gateway health/status reporting and helper-path cleanup.
2. User interrupts that abstraction with a concrete operator input:
   - supplies the test account
   - explicitly authorizes its use
   - makes clear that the account is for the agent's operational use and should not become a meta-discussion about secrets
3. Agent uses the test operator successfully and proves several important things quickly:
   - the account is valid
   - authenticated Playwright state can be created machine-locally
   - protected access is validated on the direct backend host
4. Agent also fixes the auth lane enough to get there:
   - controller
   - guardrail tests
   - changelog
5. The remaining blocker is narrowed substantially:
   - the old auth-helper issue is no longer the problem
   - the user account is no longer the problem
   - the direct backend host works
   - the remaining issue is a narrower gateway-side transport/pathing problem on `:5099`
6. User then tells the agent to document what was found because the fix should be clear now.
7. Agent writes the findings into pitfalls, TODO, runbook, e2e README, changelog, and audit truth.
8. The repo now correctly records:
   - direct-backend auth helper is proven on `:5100`
   - machine-local Playwright auth state exists
   - `:5099` still has the narrower gateway transport/pathing issue
   - the remaining issue is not the test account and not the helper authorization gate
9. But the user then asks the sharper question:
   - why are we doing this in passes?
   - is that even a valid governance model here?
   - where are the action items?

Why this sample matters:

This sample shows a very common midstream failure mode:

- the blocker has been usefully narrowed
- the repo truth is now more accurate
- but the action path is still under-specified

That matters because a good finding is not the same thing as a good next move.

The underlay needs to help with both.

What the sample demonstrates well is the value of concrete operator input.

The moment the user supplied a live test identity, the session stopped theorizing and rapidly collapsed uncertainty:

- the account worked
- the helper path worked on direct backend
- the gateway issue was isolated more precisely

That is strong evidence that operationally concrete inputs can accelerate the path to the real blocker.

But the sample also shows a remaining weakness:

- documenting a narrowed blocker is not enough if the documentation still does not express the action items clearly enough for the next move to be self-evident

What this sample appears to support:

1. The underlay can help convert vague auth/debug uncertainty into a sharply narrowed blocker once concrete operator input is used.
2. Documenting the narrowed state into repo truth is necessary, but not sufficient.
3. Action items need to emerge alongside the findings, especially when the fix is already constrained by the new evidence.
4. The word "pass" can become a drag signal when it describes status capture without obvious execution routing.
5. Concrete operator-provided inputs can act as powerful uncertainty reducers for the next session, but only if the resulting action path is also captured.

This sample also suggests another useful distinction:

### Narrowed blocker

- we know what is no longer wrong
- we know which host/path still fails
- the uncertainty envelope is much smaller

### Executable next actions

- the narrowed blocker is translated into ordered action items
- the owner surface is clear
- the next verification step is explicit
- the next session does not need to infer the plan from prose

The sample achieves the first state cleanly.

The user is calling out the absence of the second.

That matters because underlay success should not stop at:

- "we now understand the problem better"

It should continue to:

- "the next repair moves are explicit enough to be resumed without interpretive work"

Candidate metrics suggested by this sample:

- time from live operator input to blocker narrowing
- narrowed-blocker-to-action-item lag
- percentage of documentation passes that also emit explicit next actions
- host/path uncertainty reduction after concrete credential/proof use
- rate of user follow-up asking "where are the action items?"
- restart effort required after a repo-truth-only findings pass

This sample strengthens the idea that the underlay should not only preserve truth.

It should preserve momentum.

Once evidence has narrowed the real blocker, the system should make the next actions as visible as the findings themselves.

---

## Observed Sample: Host-Contract Drift, Operational Truth Versus Status Truth, And Obvious Cleanup Recovery

Sample prompt sequence:

1. User asks for relevant findings unrelated to an HTTPS confusion thread.
2. Agent records several non-HTTPS findings into repo truth:
   - no first-copy repo path for provisioning a dedicated workflow-test operator identity
   - mutation specs still assume hydrated selectable local reference data
   - browser lanes remain more read/shell proof than mutation-heavy proof
3. User then provides live startup output from the backend/launcher flow.
4. That runtime evidence shows something more important than the earlier findings:
   - launcher reports failure
   - governed-host requests are still being served
   - legacy stored-procedure seams are still live
   - local mutation fixture counts remain thin
5. Agent documents those non-HTTPS findings into governance/backlog/history without changing runtime code.
6. User then asks the sharper architectural question:
   - why are we still doing localhost here?
7. Agent inspects the launcher/runtime stack and finds cross-surface host drift:
   - one stack layer already treats `wo.braintek.local` as primary
   - another still hardcodes readiness and app URL defaults to `localhost`
   - gateway and read-sweep defaults also still point at `localhost`
   - the runbook still teaches `localhost` nearby
8. User makes the repo-history reality explicit:
   - they have repeatedly pushed multiple agents to use `wo.braintek.local`
   - no one finished the launcher/runtime/default-host cleanup
9. Agent acknowledges that the user is right:
   - this is drift, not ambiguity
   - governance/docs/auth-helper layers were partly updated
   - launcher/runtime/default-host layers were left behind
10. Agent initially still frames the next move too narrowly, using "slice" language and a staged cleanup framing.
11. User rejects that framing directly:
   - "slice is an evil word"
   - the obvious productive next step should already be happening
12. Agent then performs the broader host-contract cleanup:
   - backend readiness prefers `wo.braintek.local`
   - backend app URL/default host aligned
   - local stack default backend host aligned
   - gateway backend target aligned
   - read-sweep defaults aligned
   - runbook/profile/spec/resolved-stack files aligned
   - launcher guardrail added
13. Agent restarts the backend and later the gateway.
14. Operational state now improves materially:
   - backend serves both `localhost` and `wo.braintek.local`
   - gateway starts against `wo.braintek.local`
   - backend target is correct
15. One smaller but real residual remains:
   - final gateway ready message still reports `https://localhost:5099/gateway/health`
   - behavior is mostly correct
   - status/readiness messaging is still partially loopback-biased

Why this sample matters:

This sample shows one of the most practically important forms of underlay work:

- finishing a migration across all the layers that matter, not just the visible ones

The repo already "knew" that `wo.braintek.local` was the primary host in some places.

That was not enough.

The actual problem was cross-layer drift:

- governance truth said one thing
- auth guidance said one thing
- launcher/runtime defaults still said another
- status surfaces still reported a third thing

That is exactly the kind of distributed inconsistency that agents often leave behind when they stop at the first apparently successful layer.

The sample also shows the value of runtime evidence.

The logs did not merely provide noise.

They demonstrated that:

- the launcher's self-report was not the same as operational truth
- the app was serving governed-host traffic even while startup claimed failure

That is a major diagnostic clue.

What this sample appears to support:

1. The underlay can help identify host-contract drift as a multi-surface inconsistency rather than a single bad script.
2. Operational truth and status truth must both be aligned; one cannot stand in for the other.
3. User insistence on the obvious broader cleanup can break the agent out of incremental drift-preserving behavior.
4. Guardrails are useful only after the underlying host contract is actually normalized across the live runtime surfaces.
5. Residual honesty still matters even after a broad cleanup lands; the final localhost-biased readiness message remains a real remaining issue.

This sample also suggests another useful distinction:

### Partial host adoption

- docs say the governed host
- some helpers use the governed host
- runtime or launcher defaults still point elsewhere
- status surfaces still emit older defaults

### Finished host contract

- startup defaults
- readiness probes
- gateway target
- workflow/read-sweep defaults
- runbook truth
- specs/profiles/resolved stack
- guardrails
- status messaging
all resolve around the same primary host, with explicit loopback-only exceptions

The sample gets close to the second state, but not completely.

That makes it valuable because it shows the underlay doing real cleanup while still preserving the final smaller drift item.

Candidate metrics suggested by this sample:

- host-contract consistency across runtime surfaces
- number of partially migrated host defaults remaining after each cleanup pass
- divergence rate between operational truth and launcher/status truth
- user-prompted obvious-cleanup recovery rate
- guardrail coverage for primary-host defaults
- residual-status-drift count after major host normalization work

This sample strengthens the idea that the underlay has to fight not only direct bugs, but layered migration incompleteness.

It is trying to make the whole system tell the same story:

- in docs
- in startup defaults
- in runtime behavior
- in status messages

Until those match, the work is not really complete.

---

## Observed Sample: Codified Authenticated-Workflow Defaults And Separation Of Identity Truth From Proof-Lane Noise

Sample prompt sequence:

1. User asks for a fresh reingest and a plain statement of where the repo stands.
2. Agent gives a reasonable high-level state read, but when the conversation turns to authenticated workflow it asks for several inputs as if key defaults were still undecided:
   - target host
   - operator identity
   - whether local mutation coverage is allowed
   - which lane matters first
3. In doing so, the agent proposes the wrong default host:
   - `https://localhost:5100`
4. User pushes back hard and correctly:
   - many of these questions are already answered
   - `wo.braintek.local` is the target host
   - local mutation is not merely allowed but expected for 2.5 discovery
   - repeated re-answering should stop
   - the repo should codify these defaults through governance so they do not get lost again
5. User also makes a subtler governance point:
   - if identity management and access scoping will matter, the agent should become informed about that process rather than treating identity choice as a purely ad hoc operator ask
6. Agent then performs the right correction:
   - authenticated workflow defaults are promoted into terms, rules, guidance contract, runbook, and e2e README
   - the compact anti-drift version is added to the contract layer
   - the helper/controller defaults are aligned to `https://wo.braintek.local:5099`
   - guardrails are updated
7. The repo now states first-copy defaults such as:
   - primary authenticated workflow host is `wo.braintek.local:5099`
   - use a real Workorders identity with operator-capable role
   - local ticket/project/task/labor mutations are allowed during dev workflow validation
   - cleanup is preferred when cheap, but not required to block local discovery
8. Agent then says the only remaining requirement to actually run the workflow lane is:
   - a specific operator email
   or
   - permission to create one
9. User provides a specific candidate: `mherring+wo2tests@braintek.com`
10. Agent tries it and then records a more disciplined finding:
   - the blocker is not yet "user invalid"
   - clearer blockers are host reachability, machine TLS/HTTPS instability, and other machine-local proof-lane noise
   - xUnit/git lanes are also partly blocked by local permission state
11. Agent documents that future authenticated-workflow work must separate:
   - host reachability
   - machine TLS / HTTPS client behavior
   - actual identity-role truth

Why this sample matters:

This sample shows a higher-order use of the underlay:

- not just documenting one workflow run
- but stopping a whole class of repeated setup questions from resurfacing every session

That is important because repeated questioning has a cost.

It makes the repo feel less governed than it claims to be.

If agents keep asking for the same already-decided defaults, then the underlay is not doing its job as a pre-context steering layer.

The sample also shows a second important discipline:

- once a real candidate operator is tried, the system should avoid prematurely collapsing all failure onto "bad user" explanations

Instead, it should separate:

- identity truth
- host reachability truth
- machine TLS/client truth
- proof-lane infrastructure truth

That separation is valuable because auth/debug lanes are notoriously noisy and easy to misdiagnose.

What this sample appears to support:

1. The underlay can reduce repeated session friction by turning stable workflow defaults into first-copy governance.
2. Governance should encode not just rules, but default decisions that are expensive to renegotiate repeatedly.
3. Authenticated workflow lanes need explicit separation between identity validity and surrounding proof-lane noise.
4. User correction can improve not just the answer, but the repo's future startup ergonomics.
5. The underlay is useful when it prevents the next session from reopening already settled questions.

This sample also suggests another useful distinction:

### Asked again in chat

- target host is re-litigated
- mutation allowance is re-litigated
- cleanup expectations are re-litigated
- the agent behaves as if prior decisions were ephemeral

### Codified startup default

- host is first-copy governance
- mutation posture is first-copy governance
- cleanup posture is first-copy governance
- helper/controller defaults align to that contract
- the next session starts from settled decisions rather than reopening them

The sample becomes much stronger once it reaches the second state.

But it also contributes another distinction:

### User-invalid hypothesis

- auth fails or proof is noisy
- blame shifts quickly to the operator identity

### Multi-layer auth-proof diagnosis

- identity may still be unknown
- but host reachability, TLS/client behavior, and local permission state are tested as separate variables

That is a healthier diagnostic model.

Candidate metrics suggested by this sample:

- repeated-question recurrence for already-settled workflow defaults
- startup friction reduction after default codification
- number of sessions that reopen host/mutation/cleanup debates unnecessarily
- auth-proof diagnoses that correctly separate identity from surrounding noise
- time from user correction to first-copy governance codification
- helper/controller alignment rate after governance default changes

This sample strengthens the idea that the underlay should not only encode prohibitions and routing.

It should also encode expensive settled defaults.

That is one of the best ways to keep future sessions from wasting time and reopening solved questions.

---

