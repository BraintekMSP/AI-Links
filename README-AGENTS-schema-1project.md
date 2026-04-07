# AGENTS-schema-1project.json

## The Problem

Most projects start simple: one goal, one person or agent doing the work, one expected output. No one creates governance for something this small. And they shouldn't — governance overhead on a focused task is friction that slows down work without adding value.

But focused projects still fail in predictable ways:

- The agent that starts the work is not the agent that finishes it. The second agent has no context.
- A failure occurs, gets fixed, and is forgotten. Three sessions later it happens again.
- The goal drifts — slowly, one "small addition" at a time — until the project is doing something nobody originally intended.
- Terms get used inconsistently across sessions because no one wrote down what they meant.

These are not governance problems. They are continuity problems. The project does not need rules, ownership declarations, or scope hierarchies. It needs a memory that survives across sessions and an honest record of what the goal is.

## What This Schema Does

AGENTS-schema-1project.json provides session continuity for single-goal projects without governance overhead. It defines:

- **Goal declaration**: One sentence, set once at authoring time, changed only with human confirmation. If the goal shifts, that is a signal to exit to the governance schema.
- **project-state-when-work-starts-and-stops**: Open and close state recorded per work period. Any agent picking up the project reads the last close state and knows where things stand. String-based, not command-based — deliberately distinct from governance's agent-session-open-close-measurement.
- **Terms** (optional): Only created when a term is being used inconsistently or has a project-specific meaning.
- **Pitfalls** (optional): Failure memory. When something goes wrong, it gets recorded so the next session does not rediscover it. If the same pitfall hits session-count 3, that is an exit signal.
- **freeform-patterns**: Lightweight notes on what is working and what is not. No signal mapping, no formal structure — just string lists. If the same not-working note appears across 3 sessions, that is an additional exit-to-governance signal.
- **External tracking pointer**: Where bug reports and feature requests live for this project, if anywhere. No tool assumption — just a reference.
- **narrative-ref**: Optional pointer to a narrative record if this project is being tracked by a narrative schema deployment.
- **Incomplete session handling**: A file-path pointer to a HANDOFF.md when work ended before completion.

That is the entire schema. Four files maximum, two of them optional.

## When to Use It

- A project has one clear goal and one expected output
- No ownership rules, scope boundaries, or cross-team coordination needed
- The work is bounded enough that drift is unlikely but not impossible
- You want session continuity without governance overhead
- A personal project, a focused sprint, a proof of concept, a single deliverable

## When NOT to Use It

- Rules or ownership decisions need to be recorded — use governance
- Multiple agents or teams need to coordinate — use governance
- The subject is communication, relationships, or tribal knowledge — use narrative
- The same pitfall keeps recurring (session-count >= 3) — exit to governance

## How the Solution Manifests

An agent receives this schema and authors two required files (AGENTS.md, AGENTS-hello.md) with human confirmation. Optionally, it adds AGENTS-Terms.md and AGENTS-Pitfalls.md when the project needs them.

The result is a workspace where:

1. Any new agent reads AGENTS.md, then AGENTS-hello.md, and immediately knows: what is the goal, what happened last work period, and what is known to go wrong
2. Work handoffs are explicit — the project-state-when-work-starts-and-stops open/close eliminates "where did we leave off?"
3. Failures are captured before they become recurring mysteries
4. Goal drift is detectable — the goal field is a fixed reference point that changes only with deliberate human approval
5. The schema gets out of the way — no rules to write, no vision to declare, no architecture to map

The files are flat labeled key-value markdown — same encoding as the governance schema, same human-readability.

## Relationship to Siblings

- **Exits to governance** when: rules emerge, ownership matters, multiple scopes appear, or pitfall session-count hits 3. A 12-step migration-protocol maps 1project fields to governance equivalents, identifies what carries over, what needs human input, and what cannot be derived automatically.
- **Referenced by narrative** when a narrative decision spawns a concrete project (via known-decisions.project-ref). 1project can reference the narrative back via narrative-ref on AGENTS-hello.md.
- **Sibling isolation**: If another AGENTS-schema-*.json is present in the context window, do not merge, inherit, or cross-reference fields unless an explicit pointer field (narrative-ref, project-ref) declares the relationship.
- 1project is the entry point of the family — the smallest useful tool.

## Exit Conditions and Migration

This schema has a natural ceiling. Replace it with AGENTS-schema-governance.json when any of these emerge:

1. Rules or ownership decisions need to be recorded and enforced
2. Multiple agents or teams need to coordinate without re-explaining context
3. Scope grows to include distinct deliverables with different recipients
4. The same pitfall is rediscovered 3 or more times — that is a rule waiting to be written

The exit is a graduation, not a failure. It means the project grew enough to need real governance.

The schema includes a migration-protocol that maps the transition:
- The goal becomes the governance scope-statement in AGENTS-Vision.md
- freeform-patterns.not-working entries that triggered the exit become candidate rules
- freeform-patterns.working entries become candidate observed-good-patterns
- Existing Terms and Pitfalls carry over; new governance-required files (Vision, Rules) must be authored with human confirmation
- project-state-when-work-starts-and-stops does not map to agent-session-open-close-measurement — the human must configure measurement commands for the governance schema

## Key Design Decisions

- **No rules, no vision**: These are governance tools. A single-goal project has a goal, not a vision. Adding rules to a focused project is premature abstraction.
- **Optional files only when needed**: Terms and Pitfalls are not authored unless the project encounters a reason to create them. No empty boilerplate.
- **project-state-when-work-starts-and-stops**: String-based open/close, no command execution, no baseline/diff. Named distinctly from governance's agent-session-open-close-measurement to prevent conflation.
- **freeform-patterns are lightweight by design**: No signal mapping, no formal structure. The 3-session repeat threshold on not-working entries is an exit signal, not a governance mechanism.
- **Goal immutability**: The goal field is set once. Changing it requires human-confirms. This is the primary drift detection mechanism.
- **Context compression is not trustworthy**: Acceptable compression in the current context window does not guarantee fidelity in future context. Durable state must be written to files, not held in context.
- **Bug reports and feature requests are not governed here**: Same boundary as governance. The schema points to where they live, does not own them.
