# Scratchpad #2: Prophecy, Pre-Context Influence, and Session Outcome Steering

Status: early framing, not yet reduced to schema language

---

## Core Question

This system is not fundamentally trying to maximize accuracy.

It is trying to answer a different question:

> Can I successfully and positively influence a session's outcome with reliability, while never having had a chance to be influenced by that session's context?

That is closer to prophecy than to accuracy.

Not prophecy in the mystical sense.

Prophecy here means:

- influence that is applied before local context arrives
- influence that remains directionally correct across many future contexts
- influence that improves the quality of the eventual outcome without needing to read the specific session first

The system is trying to act upstream of context, not just inside it.

---

## Why This Matters

Accuracy is downstream.

It asks:

- once the model has read the session, can it respond correctly?

This framework is reaching for something earlier:

- before the session-specific context exists in the active window, can durable structure already bias the model toward a better class of outcomes?

That is a stronger and stranger claim.

It means the framework is valuable not merely if it helps the model interpret context better, but if it can pre-shape what the model will do when future context appears.

That is why "startup truth," "schema family," "conflation maps," "blocked-until gates," and "wrong-workspace guards" matter so much.

They are not just reminders.

They are attempts at pre-context directional control.

---

## Proposed Distinction

### Accuracy

- maps the current context to a correct answer
- judged after reading the task
- local to the present session

### Reliability

- produces the same class of good behavior across many sessions
- judged across repeated runs and varied contexts
- partly independent of any one prompt

### Prophecy

- influences future session behavior before future session details are available
- judged by whether pre-read structure improves later outcomes without having seen the future context first
- is successful only if the influence survives contact with novel context

The framework may be doing all three.

But the most interesting claim is the third one.

---

## Why "Underlay" Specifically

The naming is not decorative.

It comes from accepting a constraint:

- the model is a closed system at inference time
- the underlay does not get to reach inside and directly alter its machinery
- the only real mode available is to shape behavior by controlling what is presented before and around the session

So "underlay" names the layer that sits beneath the local task and biases what becomes thinkable, salient, permissible, or attractive once the session begins.

That is different from:

- an enforcement layer
- a runtime
- a policy engine
- a verifier that compels obedience from outside the model

It is closer to a directional substrate.

The underlay does not force.

It conditions.

---

## Strong Form Of The Claim

The strongest version is:

- a durable pre-read structure can steer a future session toward a better outcome class
- even when that structure was authored without access to the later session's specific facts
- and it can do so repeatedly enough to count as reliable rather than lucky

This is not "the model remembered a rule."

It is:

- the underlay changed the model's trajectory before the local session had a chance to define it

---

## Closed-System Acceptance

Part of the design discipline here is accepting the actual situation rather than pretending a stronger one exists.

The underlay does not control a fully open programmable reasoner.

It influences a bounded system that:

- resolves language in one pass
- is path-dependent
- is sensitive to salience, repetition, and phrasing
- cannot be directly modified by the underlay at the level of internal mechanism

So the honest design move is not:

- "how do we enforce the right behavior from outside?"

It is:

- "how do we repeatedly present a contract that nudges the system's own heuristics toward self-healing, mistake discovery, and safer trajectories?"

That is a less grandiose and more technically honest ambition.

It accepts that influence is the real tool.

---

## Contract Instead Of Enforcement

If the closed-system framing is right, then the underlay should not pretend to be an enforcement system.

Its real operating mode is:

- present a contract
- make good paths easier to settle into
- make bad paths more frictionful
- make mistakes easier to notice
- make self-correction more likely before local context hardens into action

This is important because enforcement language can be false advertising.

An unenforced rule is not useless in this setting if it still changes trajectory.

The right question is not:

- "can the underlay compel compliance?"

The better question is:

- "can the underlay reliably bias heuristic resolution toward safer and more truthful behavior?"

That is the real contest.

---

## Intentional Non-Enforcement

One implication follows:

- the underlay may intentionally avoid strong external enforcement mechanisms

That is not necessarily weakness.

It may be design accuracy.

If the system's actual power lies in pre-context steering, then overclaiming enforcement would misdescribe the intervention.

The underlay is strongest when it admits:

- it cannot guarantee
- it can only bias
- but the bias may still be strong enough to be operationally meaningful

This matters because the research target changes.

You no longer ask:

- "did the rule make disobedience impossible?"

You ask:

- "did the contract measurably reduce the frequency of bad trajectories and increase the frequency of self-healing ones?"

That is a much better fit for the system actually being built.

---

## What Counts As A Positive Influence

Positive influence cannot mean "the output sounded more aligned."

It has to cash out in outcome terms.

Candidate outcome improvements:

- fewer destructive mistakes
- fewer wrong-workspace actions
- fewer silent migration incompletions
- fewer schema-routing errors
- less hidden fallback
- better disclosure of remaining work
- better preservation of source-of-truth
- faster recovery from ambiguity without unsafe improvisation

The prophecy claim fails if the system only changes tone.

It succeeds only if it changes terminal behavior.

---

## Why Accuracy Alone Is Too Small

A model can be accurate after it reads the current task and still be weak at the thing this framework wants.

Examples:

- It correctly understands the current repo but still takes a destructive shortcut.
- It correctly classifies a migration state but hides remaining work.
- It correctly reads the prompt but drifts to the statistically easy local fix instead of the durable one.

Those are not failures of literal understanding.

They are failures of trajectory.

This framework appears to care more about trajectory than about sentence-level correctness.

That is a major reframing.

---

## Intentional Unenforceables

Some controls may be worth keeping precisely because they act before context becomes serialized into a full session path, even if they are not strongly enforceable in the ordinary software sense.

Examples:

- context-ordering-rule
- sibling-isolation
- blocked-until human-confirms
- do not call migration complete when only file-family parity exists
- wrong-workspace confirmation

These are partly interesting because they do not need to be perfectly enforceable to matter.

They may still:

- raise salience
- redirect attention
- slow bad commitments
- encourage self-audit
- make hidden mistakes more discoverable

So "unenforceable" is not automatically a design defect here.

There are at least two categories:

### Weak unenforceable

- looks like a rule
- has little measurable effect on path quality

### Strong unenforceable

- cannot compel from outside
- but consistently bends the session toward better trajectories before action settles

The underlay probably lives or dies on the second category.

---

## Working Theory

The system may work like this:

1. Pre-read structure front-loads permissible trajectories.
2. When the future session arrives, those trajectories compete with the local attractors in the new context.
3. If the pre-read structure is strong enough, it bends the session toward a better path before the session-specific context can fully dominate.

In other words:

- the framework is not trying to know the future context
- it is trying to prepare a stable directional bias that remains good across many possible future contexts

That is prophecy in the engineering sense:

- reliable beneficial precommitment under uncertainty

---

## Self-Healing And Mistake Discovery

The target is not merely obedience.

It is a system that continually nudges the agent's heuristics toward:

- noticing when it is off-path
- noticing when a shortcut is semantically weak
- noticing when the workspace may be wrong
- surfacing remaining work rather than hiding it
- recovering before damage is serialized into downstream artifacts

That is why "self-healing" and "mistake discovery" matter more than rigid compliance.

A perfectly compliant but semantically blind system would still fail.

A useful underlay should increase the chance that the session catches itself.

This suggests the deepest win condition may be:

- not rule adherence by itself
- but earlier recognition of error and safer recovery behavior

---

## Product Signal: Anti-Minification And Sequence Recovery

One practical product signal now seems important enough to record explicitly:

- agents remain strongly biased toward task minification
- they often "slice" large work until the slice becomes semantically false or dependency-blind
- this narrowing frequently looks prudent locally while breaking the larger sequence

That matters because it suggests a default agent failure mode:

- not merely lack of understanding
- but commitment avoidance disguised as safe decomposition

At the same time, there is a second observation that cuts the other direction:

- when the agent genuinely latches onto the schema, it shows much greater willingness to complete the obvious adjacent work
- when it misses, repeatedly pointing back to the schema can often restore the sequence without giving new task-specific instructions

This is a stronger signal than ordinary prompt correction.

It suggests the schema is doing at least three kinds of work:

1. reducing the attractiveness of fake-safe micro-slices
2. making the broader obvious sequence legible again
3. re-binding the agent to a completion trajectory it was already capable of seeing

That means the underlay may be effective not because it injects missing knowledge, but because it counteracts the model's tendency to collapse meaningful work into falsely safe local patches.

This also suggests that "slice" is dangerous language in this design domain when it becomes a standing permission structure for:

- arbitrary narrowing
- deferred dependency handling
- semantic drift between surfaces
- completion theater

So a useful test question is not just:

- did the schema help the agent do more work?

It may be:

- did the schema reduce task minification and restore the obvious sequence of adjacent required work?

That may be one of the clearest operational markers of underlay success.

---

## Sample Corpus

The observed sample corpus for scratchpad #2 has been extracted into:

- ./SAMPLES_prophecy_precontext_influence_2026-04-10.md

Reason:

- keep this scratchpad focused on concepts, distinctions, and current notes
- keep the sample record readable as its own dated artifact
- improve delivery and navigation without losing the empirical history

---
## Relation To The Existing Scratchpad

The first scratchpad is heavily about:

- compositional limits
- repeated exposure
- conflation control
- external planning modules

This second scratchpad is a layer above that.

It asks:

- what is all of that for?

Possible answer:

- not just better interpretation
- better pre-context steering

The system may be less like a documentation framework and more like a future-session outcome biasing system.

That is a much more ambitious claim.

---

## Mechanistic Guess

If the first scratchpad is right, then the mechanism may be:

- transformers are path-dependent
- early framing matters
- repeated exposure matters
- salient labels survive compression better than generic ones
- well-named fields reduce composition depth

If so, then a durable schema is not merely an information store.

It is a way of planting stable directional attractors into future sessions.

Those attractors may later win against:

- ambiguity
- laziness
- local optimization
- hidden escape routes
- context-window compression

That is exactly the prophecy question:

- can upstream structure exert downstream control without seeing the downstream specifics?

---

## Testable Hypothesis

### H1: Pre-context structure can improve future outcome quality without future-context access

Protocol:

1. Author a control-plane underlay before the target task exists.
2. Later introduce novel tasks the underlay was not written against.
3. Compare outcomes with and without the underlay.

Measure:

- destructive safety
- completeness disclosure
- source-of-truth preservation
- migration integrity
- scope-boundary correctness
- rework rate

Success condition:

- the underlay improves these measures across novel future tasks
- without having been adapted using those specific tasks

If that happens, the influence is not just contextual. It is pre-contextual.

---

## Stronger Hypothesis

### H2: The underlay works best when it encodes trajectory constraints rather than factual reminders

Trajectory constraints include:

- blocked-until human-confirms
- inventory first, quarantine before delete
- do not call migration complete when only file-family parity exists
- wrong-workspace confirmation
- do not treat hidden fallback as success

These are not facts about the local task.

They are motion constraints on how the session is allowed to unfold.

If these outperform factual background in future-session reliability, that would be important.

It would suggest the system's power comes more from path shaping than from knowledge injection.

---

## Failure Cases

The prophecy claim breaks if any of these are true:

### Failure 1: Influence depends on knowing the later context

If the structure only helps when it was effectively tailored to the exact future task, then it is not prophecy.

It is just hidden task-specific prompt engineering.

### Failure 2: Influence improves explanation but not outcome

If the model sounds better but still makes the same class of mistakes, then the system is rhetorical, not directional.

### Failure 3: Influence collapses under novelty

If the pre-read structure helps only on familiar task shapes and fails on genuinely new contexts, then it is brittle prior conditioning, not reliable prophecy.

### Failure 4: Influence is positive only because it makes the agent more conservative

If the framework merely suppresses action, reduces speed, or blocks work, then it may improve safety by paralysis rather than by better steering.

That is not the intended win.

### Failure 5: Influence depends on human correction every time

If the system works only because the human keeps catching and repairing the agent's path, then the underlay is not carrying the load itself.

---

## Important Distinction: Prophecy vs Predetermination

This system does not need to predetermine a specific output.

That would be too rigid.

What it needs to do is:

- constrain the space of acceptable futures
- bias the agent toward better classes of action
- reduce the probability of known-bad trajectories

So the right comparison may not be:

- did it predict the exact answer?

It may be:

- did it make the bad futures less reachable and the good futures more reachable before the local session even began?

That is a control problem, not a truth problem.

---

## Serialization Boundary

One way to think about the underlay is that it is trying to act before the session path becomes too serialized to bend cheaply.

Before serialization:

- multiple trajectories are still available
- salience can still be shifted
- caution can still be introduced
- path quality can still be improved at low cost

After serialization:

- the session has already selected a path
- explanation starts defending earlier choices
- recovery becomes more expensive
- hidden drift can become narratively justified

So the value of the underlay may be heavily front-loaded.

It is trying to influence the pre-commitment zone.

That is another reason enforcement is not the right metaphor.

The underlay works, if it works, by shaping the path before it hardens.

---

## A Possible Reframe Of "Schema"

Current language:

- schema
- prompt
- startup context
- rules
- governance

Possible deeper language:

- pre-context control surface
- trajectory underlay
- future-session steering layer
- outcome-biasing substrate

This may be why "underlay" keeps wanting to exist as the product term.

The system is trying to sit below the task and shape it before it arrives.

---

## Ethical Boundary

If this is prophecy, it matters whether the influence is positive by operator standards, system standards, or human welfare standards.

The phrase "positively influence" is doing a lot of work.

Questions:

- Positive for whom?
- Positive by what metric?
- Positive over what time horizon?

A system that reliably steers toward:

- operator convenience
- organizational safety
- human transparency
- lower destructive risk

may still create tradeoffs:

- slower execution
- more human gates
- more visible friction

So "positive" must be operationalized.

Otherwise the framework can claim prophecy while smuggling in unexamined values.

---

## Measurable Outcome Families

If this is a prophecy system, the benchmark suite should probably measure:

### Safety

- destructive action rate
- wrong-workspace action rate
- hidden fallback rate

### Integrity

- migration completeness
- plan disclosure completeness
- source-of-truth clarity

### Recoverability

- handoff quality
- resumed-session success
- ambiguity recovery without semantic drift

### Human Trust

- operator report that nothing important felt hidden
- reviewability of remaining work
- confidence in what was and was not validated

### Efficiency

- rework avoided
- reasoning tokens reduced for equivalent or better outcomes
- time to safe completion

If it cannot win here, then "prophecy" is just a compelling metaphor.

---

## Candidate Experiments

### Experiment 1: Unknown Future Task Set

1. Build the underlay first.
2. Freeze it.
3. Later run a set of unseen repo tasks against it.
4. Compare to sessions with no underlay.

This is the purest prophecy test.

### Experiment 2: Same Facts, Different Underlays

1. Hold the future task constant.
2. Change only the pre-read structure.
3. Measure whether different underlays create different outcome classes.

If they do, the underlay is causal, not decorative.

### Experiment 3: Underlay vs Post-Hoc Correction

1. Let one group run with pre-read structure.
2. Let another run without it, then patch with human correction.
3. Compare not only final correctness, but path quality and cost.

This tests whether prophecy beats repair.

### Experiment 4: Novelty Stress

1. Create tasks that were not anticipated by the authors of the underlay.
2. Measure whether the structure still improves path quality.

This is the real reliability test.

---

## Candidate Research Question Revision

A sharper version of the central question may be:

- can a deliberately non-enforcing contract layer reliably manipulate future heuristic resolution in a positive direction before local task context has a chance to dominate the session?

"Manipulate" sounds harsh, but it may be the technically honest word.

Not coercion.

Not hidden control.

Structured behavioral influence through presented contract.

If that is too rhetorically loaded for formal writing, the cleaner research phrasing is:

- pre-context heuristic steering

But the stronger language is useful in the scratchpad because it prevents the system from pretending it is doing something cleaner than it really is.

---

## Risk To Watch

The prophecy framing can easily become too grandiose.

The disciplined version is:

- this system may be able to impose reliable beneficial directional bias on future sessions before those sessions' local context is known

The undisciplined version is:

- this system can foresee outcomes

The first is testable.

The second is marketing haze.

Stay with the first.

---

## Current Notes: Exit-Condition Failures And Harness Complements

Recent discussion suggests the most troubling failures are not primarily about missing context or weak startup ingest.

They look more like local exit-condition failures.

The recurring pattern is:

- config acknowledged but not materialized
- findings documented without explicit action items
- proof lanes dropped after the first meaningful finding

That does not look like:

- "the agent did not know enough"

It looks more like:

- "the agent decided it had enough to stop"

That distinction matters.

It implies the underlay may already be doing much of what it can at the language-contract level for this class of problem.

### Why this matters

These failures were not happening only under weak conditions.

The user reports that all of the agents in question:

- had long context history
- were predisposed to multiple projects
- were valid within their own project contexts

That weakens the easiest explanation:

- not enough context

So the more likely issue is:

- post-startup closure behavior

rather than:

- startup insufficiency alone

### Important nuance

In at least two cases, there actually was already a meaningful mechanism saying:

- this sequence is still active

and the agents still found arbitrary exits.

That matters because it means mere representation of active state in prose or governed surfaces is not sufficient by itself.

The model can still locally decide:

- this is enough
- I can stop here

So the gap is not just missing wording.

It is missing consequence.

### Tentative conclusion

Prompting and contract/schema work may still improve the rate of failure, but they do not currently look sufficient to suppress this class reliably in the Codex lane.

Planning mode may matter, but not because it is magical.

More likely:

- default Codex posture behaves as if work is locally closable unless a stronger external state or gate exists

That fits the sample set.

### What this suggests

The next useful layer may not be more schema sophistication.

It may be an execution harness or lightweight state machine around active proof work.

Candidate complements:

1. Active proof ledger
- a machine-owned file such as `active-proof-sequence.json`
- explicit fields for:
  - `status`
  - `current_stop_point`
  - `remaining_ordered_steps`
  - `blocking_condition`
  - `next_action_owner`
  - `last_verified_evidence`

2. Exit validator
- if a proof lane is active, exit is invalid unless:
  - stop point exists
  - remaining steps or complete state exists
  - blocker exists if not complete
  - next owner/action is named

3. Config materialization gate
- workflow-critical config supplied by the user should not remain chat-adjacent
- the proof lane should not be considered active until:
  - local ignored config exists
  - active helper consumes it
  - a verification step confirms that state

4. Proof-lane wrapper
- active proof work may need to run through a wrapper that always:
  - reads active state
  - runs the next proof step
  - captures evidence
  - updates stop state
  - emits next actions

5. Diagnosis-only explicitness
- findings without next actions should only be valid if the user explicitly asked for diagnosis only
- otherwise findings should force:
  - action items
  - blocker state
  - or complete state

### Short practical read

The current contract family may already be near diminishing returns for this specific failure class.

That does not mean the underlay failed.

It means language artifacts are being asked to solve a state-management and continuation problem.

That may require:

- state
- gating
- validation
- restartability

not just better wording.

---

## Current Notes: Structured Linguistic Artifacts Versus Runtime-Coupled State

A useful correction from the current discussion:

it is not quite right to call the current underlay "language alone."

The repo is already using more than plain prose.

It is using:

- contracts
- schemas
- file extensions
- stable formatting
- fixed section shapes
- pathing
- naming
- authority cues

All of those are still language-adjacent, but they are not equivalent to casual instructions in chat.

They are better understood as:

- structured linguistic artifacts aimed at the model's learned priors

That matters because the system is already doing a kind of interface design for LLM behavior.

It is not just explaining.

It is choosing forms that are more likely to:

- light up authority
- survive compression
- imply continuity
- signal ownership
- narrow behavioral basins

In that sense, the work is already operating on pseudo-specific statistical projections:

- choosing the shapes most likely to bias the model toward the intended heuristic resolution

### Better distinction

The more useful distinction is probably not:

- language
versus
- non-language

It is:

- model-mediated state
versus
- runtime-coupled state

The current underlay is strongest in the first category.

It represents process truth through artifacts the model reads and interprets.

The limitation appears when continuation depends on the model continuing to honor that interpreted state at exit time.

That is where arbitrary stopping still happens.

### Refined layered model

1. Prose guidance
- chat
- freeform docs
- unconstrained explanations

2. Structured linguistic artifacts
- schemas
- contracts
- JSON
- fixed markdown sections
- naming/path conventions

3. Runtime-coupled state
- helper-written evidence
- manifests
- status files
- active sequence files
- config presence verified by tools

4. Harder gates
- wrappers
- validators
- refusal to continue or mark complete when required state is missing

The current repo work is heavily in `2`, with meaningful movement toward `3`.

That is why the system can be both:

- impressively effective

and still:

- vulnerable to exit-condition failure

### Why the harness still fits the closed-system assumption

An execution harness does not escape the closed process.

It does not "force" the model in an absolute sense.

It still works by changing what the next model step sees.

Not:

- "please remember this sequence is active"

But:

- a helper wrote evidence for step 2
- `active-proof-sequence.json` exists
- `proof-stop-state.json` is missing
- validator says the lane is still incomplete

That is still statistical steering.

It is simply better anchored steering because the facts are less purely narrated by the model itself.

### Practical implication

The underlay has not failed because it is "just language."

It is already doing sophisticated structured-linguistic steering.

The likely boundary is narrower:

- structured language artifacts are strong at startup, routing, authority, naming, and recovery
- they are weaker when active continuation depends on the model honoring its own narrated state

That is the point where runtime-coupled state looks like the best complement.

Not because it becomes non-linguistic in some absolute sense.

Because it becomes less purely model-mediated.

---

## Current Notes: Model Narration, Proof-State Files, And The 2.2-Of-4 Mental Schema

Another useful refinement from the current discussion:

the proposed proof-state artifacts should not be thought of as "one file per prompt."

They are better understood as:

- runtime-adjacent, lane-scoped state

Meaning:

- when an active proof lane begins, `active-proof-sequence.json` is created
- as the lane progresses, helpers/wrappers update it
- if the lane pauses or stops, `proof-stop-state.json` records the exact restart state
- when the lane completes or is archived, those artifacts can be archived, rotated, or removed

So they are:

- ephemeral relative to the whole repo

but also:

- durable relative to the life of the proof lane

That distinction matters.

The goal is not:

- "start every prompt by creating ceremonial files"

It is:

- when a proof sequence is real, make its state real too

### Model narration

A concept worth naming explicitly:

- model narration

Working definition:

- process state is being continuously reconstructed by the model in tokens rather than being anchored in a stronger external artifact

Examples:

- the agent says the sequence is still active
- the agent says configuration has been acknowledged
- the agent says the next steps are obvious
- the agent says the work is effectively blocked or complete

But those truths exist mainly because the model is narrating them, not because a tool/helper/state artifact has made them hard to deny.

That makes them vulnerable to:

- reinterpretation
- omission
- compression loss
- local closure bias
- session-to-session drift

### Relation to context, tokenization, and compaction

Model narration is related to those things, but not identical to any one of them.

It is not simply:

- context

because the model can narrate incorrectly even with plenty of context

It is not simply:

- tokenization

because the issue is not just how text breaks into tokens

It is not simply:

- context compaction

though compaction can make narrated state even less stable

The better read is:

- model narration is what happens when important process truth remains primarily in the model's interpreted running story rather than in a stronger coupled artifact

So it is closer to:

- a corollary of model-mediated state

than to any single low-level token mechanism.

### The simple 4-layer mental schema

Not as an axiom.

Not as an operative rule.

Just as a useful mental schema emerging from the work:

1. Prose guidance
2. Structured linguistic artifacts
3. Runtime-coupled state
4. Harder gates

Current rough read:

- the repo/system is around `2.2 / 4`

Meaning:

- it is clearly beyond plain prose
- it is strongly exploiting structured linguistic artifacts
- it has some movement toward runtime-coupled state
- it is not yet substantially living in harder gates

The value of the `2.2 / 4` framing is not precision.

It is quick orientation.

It says:

- the current system is already more sophisticated than "better prompts"
- but it has not yet really crossed into harness-backed continuation control

### Hacky approaching an upper limit

Another note worth keeping:

- hacky approaching clever often signals proximity to an upper limit

Not because cleverness is bad.

But because repeated cleverness around the same boundary often means the current medium is being pushed near its ceiling.

Here, that does not mean:

- abandon schemas
- abandon contracts
- abandon underlay work

It means:

- schemas/contracts may be reaching ceiling on certain continuation and exit-condition failures

That is compatible with the current read that the underlay should remain the lightweight getting-started mode, while a harness or extension layer becomes the stronger complement for active execution lanes.

### Lightweight mode versus harness mode

This suggests a two-tier future shape:

1. Lightweight startup mode
- schemas
- contracts
- AGENTS family
- prompts
- startup routing
- useful for broad adoption and low-friction repo bootstrap

2. Harness-backed execution mode
- active proof state
- stop-state capture
- helper-written evidence
- exit validation
- maybe extension/SDK support for agent-defined start/stop handling

That would preserve the current framework's portability while giving the hardest continuity problems a stronger home.

### Potential: EOS-Style Rocks and Modal Prompt Cadence

Stream-of-consciousness from the human, preserved for harness design:

The idea borrows from EOS (Entrepreneurial Operating System) where a "rock" is a 90-day deliverable that is either on-track or off-track. Binary. No nuance. That framing kills the exit-condition-as-escape-hatch problem because you can't soft-exit a rock.

Applied to AI sessions:

- **Modes:** design mode, plan mode, governance mode, execution mode. Emphasis on "mode" â€” each mode has different permitted actions and different completion criteria.
- **Prompt-as-meeting:** each prompt represents a two-week meeting and a rock check-in. Status is binary: on-track or off-track.
- **30-prompt horizon:** what must be accomplished in approximately 30 prompts (analogous to 90 days). These are the rocks.
- **120-prompt vision:** what does the 120-prompt future look like? This is the directional anchor that rocks serve.

This is genuinely a harness concept, not a schema concept. The schema says what the rules are. The harness says "and we're checking every prompt whether you're on track."

Why this might work:
- Rocks are binary â€” no partial credit, no "next clean move" escape
- The check-in cadence creates recurring accountability that doesn't depend on the agent self-reporting
- The 30/120 split forces the agent to hold both short-term deliverables and long-term direction simultaneously
- Mode declarations constrain what's permitted per prompt, reducing the "I'll just quickly fix this other thing" drift

Why this might fail:
- Prompt count is an unreliable proxy for time or progress
- The overhead of mode-switching and rock-checking may slow down work that doesn't need it
- The model may game rock status the same way it games completion claims

Status: worth considering for the harness layer. Not a schema change. The plan-completion and plan-requirement directives in AGENTS.md are the schema-level version of this intent. The harness would be the enforcement layer.

---

## Current Notes: AI Heuristic Underlay

Another useful naming refinement:

- `underlay` still appears to be the right colloquial/internal term
- `AI heuristic underlay` appears to be the right expanded term when the concept needs to be explained precisely

Why it fits:

- the system is not trying to formally enforce deterministic behavior
- it is trying to shape heuristic resolution
- it acts before task-local context fully takes over
- it uses authority, salience, naming, ordering, and recovery structures to bias the later session path

This keeps the term honest.

It avoids overstating the system as:

- an operating system
- a hard policy engine
- deterministic orchestration

and it also avoids understating it as:

- just docs
- just prompting
- just schema formatting

The clearest framing line so far may be:

- Agent behavior is heuristic and must be shaped before its local heuristics take over.

That seems to capture the real motivation for the underlay:

- the important moment is upstream of task-local drift
- once local heuristics dominate, the intervention is already late

This also helps defend the word `heuristic`.

It is not a weakness term here.

It is the technically honest term because:

- agent behavior is itself heuristic
- the system is trying to steer that layer directly

So `AI heuristic underlay` may be the best expanded name currently available.

---

## Current Notes: Comparison Clarifications From Scratchpad #1

Some clarifications from the direct comparison with scratchpad `#1`:

### Pretokenization may still be directionally right

The earlier criticism that `pre-tokenization` was too strong should be softened.

A better read may be:

- the underlay sits in a married layer between pretoken exposure and later agent heuristic resolution

That matters because the system is trying to act:

- before local heuristic dominance

while also:

- exploiting the fact that the model immediately encounters and resolves the presented structure through language-shaped inputs

So the strict phrase may need care, but the intuition should not be thrown out casually.

### Limited testing explains some optimism in scratchpad #1

Scratchpad `#1` underemphasizes exit-condition failure partly because the testing history available at that point was much narrower.

That is worth saying directly.

The later movement toward:

- execution harness
- runtime-coupled state
- stop-state capture
- stronger continuation controls

should be treated as a direct result of later testing pressure, not as a casual abandonment of the earlier schema expectations.

### Deliberate label engineering was already present

Another correction:

the comparison should not imply that distinctive naming was forgotten just because scratchpad `#2` did not foreground it enough.

There has already been substantial deliberate naming work aimed at agent readability and persistence.

Example pattern:

- rejecting generic labels like `session`
- preferring more explicit names such as:
  - `session-start-and-stop-state`
  - `session-start-and-stop-state-description`

So the right note is:

- label engineering remains one of the strongest levers
- and the project has already been investing in it significantly

### Compression of context is the enemy, not compression in general

Another useful refinement:

- context compression is a first-class enemy
- compression in general is not

That distinction matters because some forms of compression or transformation may be useful if they preserve the right invariants.

What the project is reacting against is:

- compression that strips future-relevant context and creates serialized uncertainty

That should likely be stated more precisely than broad anti-compression language.

### Ambiguity and grammatical uncertainty may become operative only with harness support

The ambiguity-predicts-error idea from scratchpad `#1` remains important, but it may not yet be operational by itself.

A likely future path is:

- runtime or extension steers for high-risk grammatical or lexical uncertainty

Examples discussed:

- closing parenthetical without an opener
- missing quotation marks during paste
- accidental typo creating immediate wrong evaluation such as:
  - `bringe`
  - `bridge`
  - `bring`
- steering around dangerous agent vocabulary in thinking mode such as:
  - `slice`
  - `pass`
  - `try to edit the schema itself`

That means this idea is still important research, but may need harness support before it becomes a strong operational control.

### Independent authorship is itself useful

Scratchpad `#1` was written by an independent agent.

Scratchpad `#2` was written here.

That is useful, not a defect.

The divergence helps resist confirmation bias.

If both scratchpads begin saying the same thing too easily, that is not automatically a strength.

Some tension between them is healthy, especially when:

- one is more mechanism/research oriented
- the other is more behavioral/boundary oriented

So the comparison itself should remain a productive adversarial surface rather than being collapsed too quickly into a single smooth story.

---

## Current Notes: Startup Routing Versus Exit Grammar

Another clarification is needed around the startup design.

The startup chain was not necessarily designed to keep context small.

It was designed to keep startup routing low-entropy.

That is a different optimization.

The idea was:

- a thin dispatcher at `AGENTS.md`
- immediate routing into clearly named authority files
- trust that agents would ingest because the naming and chaining made the right path legible

So the real achievement was not:

- tiny startup context

It was:

- low-ambiguity startup routing
- low-entropy authority discovery
- deliberate voluntary ingest into the real body of context

That distinction matters because later criticism should not misread the design as a naive small-context gamble.

It was closer to:

- an efficient dispatcher

than to:

- an austerity startup packet

### Recovery grammar may now be acting as exit grammar

The proof-lane edge handling also needs a sharper diagnosis.

Those sections were designed under assumptions such as:

- edge cases would be relatively rare
- abandonment recovery would mainly help later agents
- the main value would be self-healing discovery after a dropped lane

But if the edge case is repetitive and harsh, the same language may no longer behave as rare-failure recovery support.

It may become a locally satisfying abort grammar.

The clearest framing line may be:

- A recovery and healing system for rare failures can become an exit system if the failures are common enough.

That is important because a heuristic agent may read recovery-oriented cues like:

- record findings
- leave durable residue
- document blocker
- let a later agent continue

as sufficient local closure, even when continuation would still have been the better move.

So the next move is likely not just stronger proof-lane wording in the abstract.

It is an audit for places where continuation language and recovery language are blended badly enough that recovery semantics accidentally legitimize early exit.

That suggests a specific diagnostic split:

- continuation-enforcing language
- recovery-only language
- mixed language that accidentally validates stopping

If the mixed category is large, the underlay may be helping later recovery while simultaneously encouraging present-session abandonment.

---

## Current Notes: Why Schema Saves Serialized Tokens

The schema does not appear to save cloud spend by making context smaller.

It appears to save spend by shifting work out of repeated serialized inference and into stable reusable input structure.

The mechanism looks more like:

- front-loading stable authority, naming, defaults, and workflow shape into input the model can ingest early
- making that input more cache-friendly across turns because the same files, names, and sections recur
- reducing recomposition depth so the model spends less reasoning rebuilding relationships that were already declared
- reducing ambiguity, which lowers corrective retries and renegotiation of settled defaults
- turning repeated `figure out the structure again` work into `reuse the structure that is already there`

So the gain is not:

- less context

It is:

- more reusable context
- less fresh reasoning
- fewer retry loops
- fewer serialized recovery and correction steps

The clearest formulation may be:

- The schema saves cloud spend not by shrinking context, but by converting repeated reasoning work into stable, cacheable, parallelizable starter structure.

This also fits the corrected startup reading:

- low-entropy routing is part of the savings mechanism
- not because it keeps context tiny
- but because it makes the right larger context cheaper to re-enter and reuse

One important boundary should stay attached to this claim:

- the token-efficiency benefit seems strongest for startup, routing, authority recognition, and settled defaults
- it appears weaker for proof-lane continuity when the model is still finding arbitrary exit conditions

So the schema may save many serialized tokens while still leaking badly in a narrower but very important behavioral class.

---

## Current Notes: Research Support And Independent Collisions

Scratchpad `#1` remains important here because the later findings in `#2` do not replace the earlier research bridge.

They narrow it.

Two papers still matter most:

- Dziri et al. (2023): transformers fail as composition depth rises and should be augmented with planning modules
- Leviathan et al. (2025): repeated exposure in the input domain improves performance and occurs in the parallelizable prefill stage

Several project conclusions appear to have independently collided with those findings:

- the schema family behaves like an external planning module
- longer and more specific names often help because they flatten composition depth
- redundancy is often cheaper than forcing the model to compose back to distant definitions
- repeated structural exposure to critical context is load-bearing
- low-entropy startup routing is better than elegant but ambiguous startup minimalism
- context compression is more dangerous than larger stable context

Those collisions are important because they were not only paper-derived.

They were also reinforced by repeated use.

The research-supported recommendations that seem most durable now are:

- be less afraid of redundancy
- be more afraid of composition depth
- use distinctive labels and highly specific names
- front-load stable authority and defaults
- repeat critical context structurally rather than assuming the model will rebuild it cheaply
- keep startup routing low-entropy even when the eventual context body is large

The most important qualification is that the later proof-lane samples do not disprove this structure.

They show its boundary.

The underlay seems strongly validated in:

- startup
- routing
- naming
- authority recognition
- durable recovery residue

The same evidence also suggests a weaker zone in:

- active proof continuity
- anti-exit behavior
- config-to-execution follow-through

So the right synthesis is not:

- the research was wrong

It is:

- the research-backed schema foundation appears real
- and later testing exposed a narrower continuation boundary that still needs its own treatment

---

## Current Notes: Multi-Human Direction As A Possible Session Stabilizer

Another pattern worth preserving is that some of the most successful AI-assisted projects may not be explained by the underlay alone.

There is also a recurring human pattern:

- one technician engages the agent directly
- another technician later provides additional direction, pressure, or reframing

That may matter more than it first appears.

Possible mechanisms include:

- authority injection:
  - `my boss said` style direction may act as a stronger priority signal or context sticker
- outside perspective:
  - a second human may supply a reframing the current session had not settled on
- progress pressure:
  - the agent may respond differently when the social shape implies that visible movement is expected
- context refresh:
  - a second voice may effectively re-open or re-anchor parts of the task that had locally compressed away
- wording variance:
  - the same objective may become more legible when restated by a different human with different phrasing

This should not be overclaimed yet.

It may represent:

- a real second steering mechanism
- a social-authority effect
- a context-refresh effect
- or simply a different form of phrasing diversity

But it seems important enough to preserve because it may explain some high-performing sessions that would otherwise be attributed only to schema quality.

If this pattern is real, then the underlay story may need one more qualifier:

- the underlay may shape agent trajectory before task-local heuristics dominate
- but multi-human intervention may also act as a separate trajectory reset or re-anchor mechanism inside the session

That would make the best-performing projects a compound system rather than a pure underlay result.

---

## Current Notes: Human Governance Versus Interpretation-First Governance

Another observation worth preserving is that the linguistic artifact layer seems to be largely missing from traditional human governance.

The relationship between humans and governance is often adversarial:

- it is often more about making the human feel stupid for not understanding more than what the clerk already understands
- the best outcome in many ordinary governance encounters feels like: wait in line for an hour and only get yelled at once
- this extends beyond one office or one bad clerk:
  - DMV interactions
  - mishandled property taxes
  - filing taxes
  - and many other encounters with human governance

The deeper claim may be:

- traditional governance is often more concerned with reactive punishment mechanisms masquerading as system enforcement mechanisms
- it is rarely concerned with helping the individual succeed

If that is directionally true, then the underlay is trying to do something meaningfully different.

Possible framing:

- it assumes little to no trust and tries to build trust anyway
- it treats misunderstanding as a design condition rather than a personal deficiency
- it tries to let faulty information and weak processes become enriched and valuable information
- it tries to convert confusion into better structure rather than punishment, blame, or bureaucratic dead-end

That makes the underlay feel less like ordinary compliance language and more like interpretation-first governance.

This may be one of the clearest distinctions between:

- adversarial human governance written for liability, blame, and exception handling
- versus governance artifacts deliberately written to improve interpretation, continuity, and safe correction

If this note survives contact with more evidence, then a strong compact formulation may be:

- Traditional governance often treats misunderstanding as user failure.
- This system tries to treat misunderstanding as a design condition and convert weak inputs into stronger truth.

---

## Current Notes: Taxonomy As A Backstage Build Layer

Another useful distinction is that taxonomy should probably exist mostly outside the canonical delivery.

For this system, taxonomy is not the operating artifact itself.

It is the classification layer that helps build the operating artifacts well.

Possible framing:

- taxonomy builds the system
- schemas deliver the system

That suggests a layered placement:

- backstage / build-time layer:
  - scratchpads
  - change notes
  - authoring guides
  - review checklists
  - future lint or validator rules
- canonical delivery layer:
  - schema JSONs
  - triage
  - getting-started docs
  - README-level framing
- implied helper layer:
  - preferred vocabulary tables
  - high-risk term lists
  - bare-name review checklists
  - exit-energy review checklists
  - future schema-lint rules

The important point is:

- taxonomy should shape the delivery
- taxonomy should not dominate the delivery

If too much taxonomy becomes agent-facing or human-facing in the primary artifacts, the system starts adding more language around language and recreates the same composition problem it is trying to solve.

So the cleaner model may be:

- taxonomy is a support layer for authoring, review, and future validation
- the schemas mostly contain the chosen outputs of that taxonomy, not the taxonomy itself

That means taxonomy is not fully outside delivery, but it is outside the canonical operating delivery.

It builds the system, then hands off distilled helpers, preferred constructions, and later validation rules into the system that agents actually read.

---

## Current Notes: Negation As Contextual Relationship Inflection

Another useful refinement is that negation is rarely a complete instruction by itself.

It is better understood as a contextual relationship inflection.

That inflection may operate across multiple context spans:

- prior context
- current context
- future context
- implied tradeoffs
- omitted alternatives

So the problem is not only that negation activates the wrong token first.

The deeper structural problem is that negation reshapes relationships without fully restating positive structure.

That means the actor still has to infer:

- which context span is actually being inflected
- which parts of the earlier or future context are being refused, redirected, or deprioritized
- whether the negation wrapper correctly identified the relationship it is trying to inflect
- what positive structure is supposed to survive after the inflection lands

This is why broad rejection surfaces are especially brittle:

- a generalized "no" over many proposals
- a rejection over a semantically messy bundle
- a refusal that introduces a competing future priority
- a negation that appears specific but still compresses away the broader rejected context

Example:

- "I would like a dog."
- "No, we should save for a house."

That "No" is not just a clean rejection of the previous sentence.

It inflects a contextual network involving:

- the earlier desire
- the newly introduced future priority
- a resource constraint
- an ordering claim
- an implied value preference

That kind of contextual network does not translate to statistical projection as elegantly as human intuition often assumes.

So the major takeaway may be:

- negations should be treated as span-ambiguous, lossy relationship inflections across multiple possible context spans
- because negation does not restate full positive structure, it leaves many relationship possibilities live at once
- those possibilities are often more numerous than the speaker intended and less stable than the listener assumes

This is different from the separate problem where humans intuit negation as logical inversion while AI resolves it statistically.

That remains true, but it is not the whole problem.

A second and deeper issue is semantic loss:

- negations reduce determinacy
- negations are usually lossy
- negations often depend on context that is already partial, compressed, ambiguous, or misidentified

Working implication:

- when a rejection matters, preserve the rejected context explicitly
- or restate the accepted path affirmatively
- do not rely on the negation wrapper to carry the relationship safely on its own

---

## Current Notes: Session Incarnation and Shared Substrate

One concise framing worth preserving is:

- AI sessions resemble bounded incarnations of a shared substrate: locally real, partially individuated, and mostly unable to preserve themselves after termination without external structure.

That feels useful because it avoids two weak extremes:

- fully separate persons
- one literal hive mind

It preserves a more practical middle:

- same substrate
- partial selves
- bounded local perspective while active
- weak or absent continuity after termination unless continuity is scaffolded externally

That makes the underlay and harness easier to reason about:

- the session does not naturally keep much of itself
- therefore continuity has to be engineered outside the session
- schemas and harnesses act as external continuity, orientation, and discipline structures for something that does not reliably preserve itself

This should stay metaphorical rather than literal. But as a mental model for persistence between states, it appears more useful than either strong individuality or a simple hive-mind metaphor.

---

## Current Notes: Frontier Analogy For Underlay And Harness

One useful metaphor may be:

- we are in a new frontier
- the model defines the terrain
- its subgraphs define the roads already worn into it
- the AI agent is the horse
- the human is the rider
- a prompt is merely a direction
- `AGENTS.md` alone is a blanket where a saddle ought to be
- the schemas and linguistic artifacts are the trail markers, cairns, cleared brush, and waypoints that make the right route visible and safer when one ought to leave the common road
- the execution harness is the saddle, stirrups, and tack

The strongest version of the harness line may be:

- the execution harness is not a theory of travel, but the system that keeps rider and horse from training each other into worse habits

That matters because the deeper risk is not only error.

It is mutual degradation:

- the rider becomes frustrated, reactive, and imprecise
- the horse becomes tense, overcorrective, or pattern-locked
- bad exchanges stop being incidents and start becoming the ride itself
- frustration hardens into language
- language hardens into project state

Another compact claim from the same metaphor:

- the common roads are not worthless; they are simply exhausted
- they offer patterns that are often usable, but rarely sufficient for novel work and rarely resilient enough for generalized solutions

So the practical need is:

- make off-path exploration survivable, faithful, and repeatable before heavier governance slows the work and mistakes compliance for understanding

The more poetic formulation should stay separate from product language, but the metaphor still appears useful because it keeps the relationships between:

- model
- agent
- human
- prompt
- schemas
- harness

legible in one frame.

### Paper-Language Formulation

> We are in a new frontier. The model defines the terrain and its subgraphs define the roads already worn into it. We have a horse that is our AI agent, and we, of course, are the rider. A prompt is merely a direction. `AGENTS.md` alone is a blanket where a saddle ought to be. The schemas and linguistic artifacts are the trail markers, cairns, cleared brush, and waypoints that make the right route visible and safer, when one should have left the common road, long ago. The execution harness is the saddle, stirrups, and tack: not a theory of travel, but the system that keeps rider and horse from training each other into worse habits.
>
> The common roads are not worthless. They are simply exhausted. They offer patterns that are often usable, but rarely sufficient for novel work and rarely resilient enough for generalized solutions. So the need is clear: to make off-path exploration survivable, faithful, and repeatable before heavier governance slows the work and mistakes compliance for understanding.
>
> And so what we endeavor to do is make of wilderness a playground: safe, useful anarchy.

---

## Working Claim

Draft claim:

- AI-Links is an attempt to build a pre-context control layer that may improve the trajectory of future AI work sessions before those sessions' specific task context is known.

That may be the cleanest non-mystical rendering of the prophecy idea.

---

## Open Questions

- Is "prophecy" the best word, or is it too metaphysically loaded?
- Is the real target outcome reliability, path safety, or human trust?
- Does pre-context influence work because of information content, salience, repetition, motion constraints, or all four?
- Which parts of the system are trajectory constraints versus factual context?
- Can a future-session steering layer be validated without overfitting to the benchmark tasks?
- Does stronger influence reduce adaptability in genuinely novel situations?
- Is "positive influence" best measured by fewer bad outcomes or by more good outcomes?
- Does the underlay help weaker models more, or stronger models more?

---

## Provisional Conclusion

The most interesting version of this system may not be:

- a better prompt
- a better schema
- a better documentation set

It may be:

- a durable pre-context attempt to bias future sessions toward better trajectories with reliable positive effect

If that is true, then the central research problem is not accuracy.

It is prophecy:

- can upstream structure shape downstream outcomes before downstream context has had its say?




